using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NetBoard.Controllers.Helpers;
using NetBoard.Model.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using static NetBoard.Controllers.Helpers.ImageManipulation;
using static NetBoard.Model.Data.Post;

namespace NetBoard.Controllers.Generic
{
	[Route("api/{board}")]
	[ApiController]
	public class BoardController : ControllerBase {
		#region Board rules

		public int MaxResponses = 500;
		private readonly int ThreadsPerPage = 10;
		public int MaxPages = 10;
		public int MaxImages = 200;
		/// <summary>
		/// How long should archived threads be kept?
		/// </summary>
		public TimeSpan ArchivedLifetime = TimeSpan.FromHours(24);
		protected readonly int MaxThreads; // filled in constructor

		#endregion Board rules

		#region Constructor
		private readonly ApplicationDbContext _context;
		private readonly IConfiguration _configuration;
		private readonly ILogger<Post> _logger;
		private readonly BoardProvider boardProvider;
		private readonly PostCreator postCreator;

		public BoardController(ApplicationDbContext context, IConfiguration configuration, ILogger<Post> logger) {
			_context = context;
			_configuration = configuration;
			_logger = logger;
			MaxThreads = ThreadsPerPage * MaxPages;
			boardProvider = new BoardProvider(context, configuration);
			postCreator = new PostCreator(_context, boardProvider, logger, configuration);
		}

		#endregion Constructor

		#region GET

		// GET: Entity/2
		/// <summary>
		/// Gets all threads from provided board.
		/// </summary>
		/// <param name="mode">Either 'list', 'archive' or page number.</param>
		[HttpGet("{mode}")]
		public virtual async Task<ActionResult<Dictionary<string, object>>> GetThreads(string mode) {
			var userIp = HttpContext.Connection.RemoteIpAddress;
			var board = RouteData.Values["board"].ToString();

			// determine how to display available threads
			// list - only OPs
			// archive - only archived OPs + up to 3 responses
			// pageNumber - convert to int and display OPs + up to 3 responses in this page
			var list = false;
			var archive = false;
			var pageNumber = 0;
			var totalThreadCount = boardProvider.GetTotalThreadCount(board);
			if (totalThreadCount < 1) return Ok("No threads on this board.");
			var pageCount = (int)Math.Ceiling((double)totalThreadCount / ThreadsPerPage);
			if (mode == "list") {
				list = true;
			} else if (mode == "archive") {
				archive = true;
			} else if (int.TryParse(mode, out int n)) {
				pageNumber = Math.Clamp(n, 1, pageCount);
			} else {
				return BadRequest($"Expected page number, 'archive' or 'list', but received '{mode}'.");
			}
			
			// get all threads
			List<Post> threads;
			if (list) {
				threads = await boardProvider.GetThreadList(board);
			} else {
				threads = await boardProvider.GetThreadPage(pageNumber, totalThreadCount, ThreadsPerPage, archive, board);
			}

			// filter shadowbanned posts
			ShadowBans.FilterShadowbanned(ref threads, userIp);

			int previewLength = int.Parse(_configuration["PreviewLength"]);
			string cutoffText = _configuration["CutoffText"];

			foreach (var thread in threads) {
				// prepare the thread for data transfer
				// if in list mode, add extra thread info
				if (list) {
					var threadInfo = boardProvider.GetThreadInfo(threads, MaxResponses, MaxImages);

					if (threadInfo.TryGetValue("responseCount", out int responseCount)) thread.ResponseCount = responseCount;
					if (threadInfo.TryGetValue("imageCount", out int imageCount)) thread.ImageCount = imageCount;
					if (threadInfo.TryGetValue("pastLimits", out int pastLimits)) thread.PastLimits = pastLimits == 1;

				// otherwise add responses to this thread ("classic" mode)
				} else {
					var lastResponses = await boardProvider.GetLastResponses(3, thread.Id, board);

					// DTOify responses
					if (lastResponses.Count > 0) {
						var tempList = new List<Post>();
						foreach (var r in lastResponses) {
							r.AsDTO(previewLength, cutoffText, userIp, thread.Id);
							tempList.Add(r); 
						}
						thread.Responses = tempList;
					}
				}

				thread.AsDTO(previewLength, cutoffText, userIp, null, true);
			}

			Dictionary<string, object> pageData;
			if (list) {
				pageData = new Dictionary<string, object> {
					{ "board", board },
					{ "boardLong", GetBoardName(board) },
					{ "totalThreads", totalThreadCount },
				};
			} else {
				pageData = new Dictionary<string, object> {
					{ "board", board },
					{ "boardLong", GetBoardName(board) },
					{ "currentPage", pageNumber },
					{ "pageCount", pageCount },
					{ "totalThreads", totalThreadCount },
					{ "threadsPerPage", ThreadsPerPage }
				};
			}

			var result = new Dictionary<string, object> {
				{ "pageData", pageData },
				{ "threads", threads }
			};

			return result;
		}

		// GET: Entity/thread/42
		/// <summary>
		/// Gets the thread and all responses.
		/// </summary>
		[HttpGet("thread/{id}")]
		public virtual async Task<ActionResult<Dictionary<string, object>>> GetThread(int id) {
			var userIp = HttpContext.Connection.RemoteIpAddress;
			var board = RouteData.Values["board"].ToString();
			var posts = await boardProvider.GetLastResponses(-1, id, board);
			var op = posts.FirstOrDefault();

			if (posts.Count == 0 || !op.ShouldDisplayShadowbanned(userIp)) {
				return BadRequest("This thread does not exist.");
			}

			// add thread data
			var threadInfo = boardProvider.GetThreadInfo(posts, MaxResponses, MaxImages);

			// convert to DTOs
			boardProvider.MakeThreadDTO(posts, userIp);

			var result = new Dictionary<string, object> {
				{ "board", board },
				{ "data", threadInfo },
				{ "posts", posts }
			};

			return result;
		}

		// GET: Entity/thread/42/149921324
		/// <summary>
		/// Gets all posts that are after certain ID.
		/// </summary>
		[HttpGet("thread/{id}/{lastId}")]
		public virtual async Task<ActionResult<Dictionary<string, object>>> GetNewPosts(int id, int lastId) {
			var board = RouteData.Values["board"].ToString();
			// check if this thread exists
			var userIp = HttpContext.Connection.RemoteIpAddress;
			if (!await ThreadExists(id, userIp, board)) return BadRequest($"There's no thread with ID {id}.");

			// get new responses
			var replies = await boardProvider.GetResponsesPastId(id, lastId, board);

			if (replies.Count == 0) {
				return BadRequest("No new replies.");
			}

			var threadInfo = boardProvider.GetThreadInfo(replies, MaxResponses, MaxImages);

			// turn into DTOs
			boardProvider.MakeThreadDTO(replies, userIp);

			var result = new Dictionary<string, object>() {
				{ "board", board },
				{ "data", threadInfo },
				{ "posts", replies }
			};

			return result;
		}

		// GET: Entity/post/42
		/// <summary>
		/// Gets a single post.
		/// </summary>
		[HttpGet("post/{id}")]
		public virtual async Task<ActionResult<Post>> GetPost(int id) {
			var userIp = HttpContext.Connection.RemoteIpAddress;
			var board = RouteData.Values["board"].ToString();

			if (!await PostExists(id, userIp, board)) {
				return BadRequest($"There's no post with ID {id}.");
			}

			var post = await boardProvider.GetPostById(id, board);
			var isThread = post.Thread.HasValue;
			post.AsDTO(userIp, post.Thread, isThread);

			return post;
		}

		// GET: Entity/post/42/thread
		/// <summary>
		/// Gets a thread of provided post <paramref name="id"/>.
		/// </summary>
		/// <returns>Thread ID.</returns>
		[HttpGet("post/{id}/thread")]
		public virtual async Task<ActionResult> GetPostThread(int id) {
			var userIp = HttpContext.Connection.RemoteIpAddress;
			var board = RouteData.Values["board"].ToString();

			if (!await PostExists(id, userIp, board)) {
				return BadRequest($"There's no post with ID {id}.");
			}

			var post = await boardProvider.GetPostById(id, board);
			var thread = post.Thread;

			var response = new Dictionary<string, int>();
			if (!thread.HasValue) {
				response.Add("threadId", id);
				return Ok(response);
			}

			response.Add("threadId", thread.Value);
			return Ok(response);
		}

		#endregion GET

		#region POST

		// POST: Entity
		[HttpPost]
		public virtual async Task<ActionResult<Post>> CreateThread([Bind("Image, Content, Name, Password, Subject, Options, CaptchaCode")] Post entity) {
			if (string.IsNullOrEmpty(entity.CaptchaCode) || await Captcha.IsCaptchaValid(entity.CaptchaCode, _configuration)) {
				return await Post(entity, RouteData.Values["board"].ToString());
			} else {
				return BadRequest("Invalid captcha");
			}
		}		

		// POST: Entity/thread/42
		[HttpPost("thread/{threadId}")]
		public virtual async Task<ActionResult<Post>> CreateResponse([Bind("Content, Name, Password, Options, CaptchaCode")] Post entity, int threadId) {
			if (string.IsNullOrEmpty(entity.CaptchaCode) || await Captcha.IsCaptchaValid(entity.CaptchaCode, _configuration)) {
				return await Post(entity, RouteData.Values["board"].ToString(), threadId);
			} else {
				return BadRequest("Invalid captcha");
			}
		}

		private async Task<ActionResult<Post>> Post(Post post, string board, int threadId = 0, bool admin = false) {	
			var userIp = HttpContext.Connection.RemoteIpAddress;
			var threadPosts = await boardProvider.GetThreadAndResponses(threadId, -1, board);

			if (threadPosts is null || !threadPosts.First().ShouldDisplayShadowbanned(userIp)) {
				return BadRequest($"There is no thread with ID {threadId}.");
			}

			// only test when in response mode
			if (threadId != 0) {
				if (threadPosts.First().Archived && !admin) {
					return BadRequest("You can't respond to an archived thread.");
				}
			}

			await postCreator.Create(post, userIp, _configuration, threadPosts, MaxResponses, MaxImages, MaxThreads, ArchivedLifetime, board);

			return Created("post", new Dictionary<string, int> { { "id", post.Id } });
		}		

		// POST: Entity/report
		[HttpPost("report")]
		public virtual async Task<ActionResult> ReportPost([Bind("PostID, Reason, CaptchaCode")] Report report) {
			var userIp = HttpContext.Connection.RemoteIpAddress;
			if (string.IsNullOrEmpty(report.CaptchaCode) || !await Captcha.IsCaptchaValid(report.CaptchaCode, _configuration)) {
				return BadRequest("Invalid captcha");
			}

			var board = RouteData.Values["board"].ToString();
			if (!await PostExists(report.PostId, userIp, board)) {
				return BadRequest($"There is no post with ID {report.PostId}.");
			}

			report.Date = DateTime.UtcNow;
			report.PostBoard = board;
			report.ReportingIP = HttpContext.Connection.RemoteIpAddress.ToString();

			_context.Reports.Add(report);
			await _context.SaveChangesAsync();

			return Ok($"Post {report.PostId} was reported with the following reason: {report.Reason}");
		}

		#endregion POST

		#region DELETE

		// DELETE: Entity/post/42
		[HttpDelete("post/{id}")]
		public virtual async Task<ActionResult<Post>> DeletePost([FromBody] Delete delete, int id, [FromQuery] bool onlyImage = false){
			var board = RouteData.Values["board"].ToString();
			var entity = await boardProvider.GetPostById(id, board);
			if (entity == null) {
				return BadRequest("Post with this ID was not found.");
			}
			if (entity.Password == null || !entity.TestPassword(delete.Password)) {
				return BadRequest("Wrong password.");
			}
			if (!onlyImage) {
				if (entity.Thread == null) {
					await boardProvider.RemoveThread(id, board);
				} else {
					await boardProvider.RemovePost(id, board);
				}
				DeleteImage(board, id);
			} else {
				entity.Image = null;
				DeleteImage(board, id);
			}
			await _context.SaveChangesAsync();
			_logger.LogInformation($"A post with ID {id} was deleted.");
			return Ok();
		}
		#endregion DELETE

		#region Tools

		// POST: Entity/del
		/// <summary>
		/// Deletes archived threads in this board. Localhost only.
		/// </summary>
		[HttpPost("del")]
		[LoopbackOnly]
		public virtual async Task RemoveArchived() {
			var board = RouteData.Values["board"].ToString();
			var archivedThreads = await boardProvider.GetArchivedThreads();
			var deleteList = new List<Post>();

			// add thread responses to deletion
			foreach (var thread in archivedThreads) {
				var archivedResponses = await boardProvider.GetLastResponses(0, thread.Id, board);
				deleteList.AddRange(archivedResponses);

				// also delete images
				foreach (var response in archivedResponses) {
					if (!string.IsNullOrEmpty(response.Image)) {
						DeleteImage(board, response.Id);
					}
				}
				DeleteImage(board, thread.Id);
			}
			deleteList.AddRange(archivedThreads);

			await boardProvider.RemoveThreads(deleteList.Select(p => p.Id).ToArray(), board);
		}

		/// <summary>
		/// Checks if post exists and if it is shadowbanned, determines if it should be returned.
		/// </summary>
		/// <param name="id">Post ID.</param>
		/// <param name="userIp">Connecting IP used to check if shadowbanned post should be displayed.</param>
		private async Task<bool> PostExists(int id, IPAddress userIp, string board) {
			var post = await boardProvider.GetPostById(id, board);
			if (post != null) {
				return post.ShouldDisplayShadowbanned(userIp);
			}
			return false;
		}

		/// <summary>
		/// Checks if thread exists and if it is shadowbanned, determines if it should be returned.
		/// </summary>
		/// <param name="id">Thread ID.</param>
		/// <param name="userIp">Connecting IP used to check if shadowbanned thread should be displayed.</param>
		private async Task<bool> ThreadExists(int id, IPAddress userIp, string board) {
			var post = await boardProvider.GetThread(id, board);
			if (post != null) {
				return post.ShouldDisplayShadowbanned(userIp);
			}
			return false;
		}

		/// <summary>
		/// Default action on accessing the board. Returns the full name of the board.
		/// </summary>
		public virtual string GetBoardName(string board) {
			return _configuration[$"Boards:{board}"] ?? "No such board!";
		}

		#endregion Tools

		#region localhost

		#region DELETE
		// DELETE: Entity/post/admin/42
		[HttpDelete("post/admin/{id}")]
		[LoopbackOnly]
		public virtual async Task<ActionResult<Post>> AuthorizedDeleteEntity(int id) {
			var board = RouteData.Values["board"].ToString();
			var entity = await boardProvider.GetPostById(id, board);
			if (entity == null) {
				return BadRequest("Post with this ID was not found.");
			}

			if (entity.Thread == null) {
				await boardProvider.RemoveThread(id, board);
			} else {
				await boardProvider.RemovePost(id, board);
			}

			await _context.SaveChangesAsync();
			_logger.LogWarning($"A post with ID {id} was deleted using administrative access (via {HttpContext.Connection.RemoteIpAddress}).");
			return Ok();
		}
		#endregion DELETE

		#region POST
		// POST: Entity/admin/thread
		[HttpPost("admin/thread")]
		[LoopbackOnly]
		[Authorize]
		public virtual async Task<ActionResult<Post>> AdministrativeCreateThread([Bind("Archived,Content,Name,Options,Sticky,Thread")] Post entity) {
			var adminLevel = PosterExtensions.GetPosterLevel(HttpContext.User);
			return await AdministrativePost(entity, adminLevel, RouteData.Values["board"].ToString());
		}

		// POST: Entity/admin/response/42
		[HttpPost("admin/response/{threadId}")]
		[LoopbackOnly]
		[Authorize]
		public virtual async Task<ActionResult<Post>> AdministrativeCreateResponse([Bind("Archived,Content,Name,Options,Sticky,Thread")] Post entity, int threadId) {
			var adminLevel = PosterExtensions.GetPosterLevel(HttpContext.User);
			return await AdministrativePost(entity, adminLevel, RouteData.Values["board"].ToString(), threadId);
		}

		private async Task<ActionResult<Post>> AdministrativePost(Post post, AdministrativeLevel posterLevel, string board, int threadId = 0) {
			post.Password = null;
			post.PosterIP = "localhost";
			post.PosterLevel = posterLevel;
			post.PostedOn = DateTime.UtcNow;

			return await Post(post, board, threadId, true);
		}
		#endregion POST

		#endregion localhost
	}
}
