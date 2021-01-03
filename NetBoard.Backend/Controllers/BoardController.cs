using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
using static NetBoard.Model.Data.PostStructure;

namespace NetBoard.Controllers.Generic
{
	[ApiController]
	public class BoardController<Board> : ControllerBase where Board : PostStructure {
		#region Board rules

		public string BoardName = "Unnamed Board";
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
		private readonly ILogger<Board> _logger;
		private readonly BoardProvider<Board> boardProvider;
		private readonly PostCreator<Board> postCreator;

		public BoardController(ApplicationDbContext context, IConfiguration configuration, ILogger<Board> logger) {
			_context = context;
			_configuration = configuration;
			_logger = logger;
			MaxThreads = ThreadsPerPage * MaxPages;
			boardProvider = new BoardProvider<Board>(_context);
			postCreator = new PostCreator<Board>(_context, boardProvider, logger);
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

			// determine how to display available threads
			// list - only OPs
			// archive - only archived OPs + up to 3 responses
			// pageNumber - convert to int and display OPs + up to 3 responses in this page
			var list = false;
			var archive = false;
			var pageNumber = 0;
			var totalThreadCount = boardProvider.GetTotalThreadCount();
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
			List<Board> threads;
			if (list) {
				threads = await boardProvider.GetThreadList();
			} else {
				threads = await boardProvider.GetThreadPage(pageNumber, totalThreadCount, ThreadsPerPage, archive);
			}

			// filter shadowbanned posts
			ShadowBans<Board>.FilterShadowbanned(ref threads, userIp);

			// prepare threads for response (ie. cut undesired data)
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
					var lastResponses = await boardProvider.GetLastResponses(3, thread.Id);

					// DTOify responses
					if (lastResponses.Count > 0) {
						var tempList = new List<PostStructure>();
						foreach (var r in lastResponses) {
							r.AsDTO(previewLength, cutoffText, userIp, thread.Id);
							tempList.Insert(0, r); // .Add() would reverse the ordering
						}
						thread.Responses = tempList;
					}
				}

				thread.AsDTO(previewLength, cutoffText, userIp, null, true);
			}

			Dictionary<string, object> pageData;
			if (list) {
				pageData = new Dictionary<string, object> {
					{ "board", typeof(Board).Name.ToLower() },
					{ "totalThreads", totalThreadCount },
				};
			} else {
				pageData = new Dictionary<string, object> {
					{ "board", typeof(Board).Name.ToLower() },
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
			var posts = await boardProvider.GetThreadAndResponses(id, -1);
			var op = posts.FirstOrDefault();

			if (posts.Count == 0 || !op.ShouldDisplayShadowbanned(userIp)) {
				return BadRequest("This thread does not exist.");
			}

			// add thread data
			var threadInfo = boardProvider.GetThreadInfo(posts, MaxResponses, MaxImages);

			// convert to DTOs
			boardProvider.MakeThreadDTO(posts, userIp);

			var result = new Dictionary<string, object> {
				{ "board", typeof(Board).Name.ToLower() },
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
			// check if this thread exists
			var userIp = HttpContext.Connection.RemoteIpAddress;
			if (!await ThreadExists(id, userIp)) return BadRequest($"There's no thread with ID {id}.");

			// get new responses
			var replies = await boardProvider.GetResponsesPastId(id, lastId);

			if (replies.Count == 0) {
				return BadRequest("No new replies.");
			}

			var threadInfo = boardProvider.GetThreadInfo(replies, MaxResponses, MaxImages);

			// turn into DTOs
			boardProvider.MakeThreadDTO(replies, userIp);

			var result = new Dictionary<string, object>() {
				{ "board", typeof(Board).Name.ToLower() },
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
		public virtual async Task<ActionResult<PostStructure>> GetPost(int id) {
			var userIp = HttpContext.Connection.RemoteIpAddress;

			if (!await PostExists(id, userIp)) {
				return BadRequest($"There's no post with ID {id}.");
			}

			var post = await boardProvider.GetPostById(id);
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

			if (!await PostExists(id, userIp)) {
				return BadRequest($"There's no post with ID {id}.");
			}

			var post = await boardProvider.GetPostById(id);
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
		public virtual async Task<ActionResult<Board>> CreateThread([Bind("Image, Content, Name, Password, Subject, Options, CaptchaCode")] Board entity) {
			if (string.IsNullOrEmpty(entity.CaptchaCode) || await Captcha.IsCaptchaValid(entity.CaptchaCode, _configuration)) {
				return await Post(entity);
			} else {
				return BadRequest("Invalid captcha");
			}
		}		

		// POST: Entity/thread/42
		[HttpPost("thread/{threadId}")]
		public virtual async Task<ActionResult<Board>> CreateResponse([Bind("Content, Name, Password, Options, CaptchaCode")] Board entity, int threadId) {
			if (string.IsNullOrEmpty(entity.CaptchaCode) || await Captcha.IsCaptchaValid(entity.CaptchaCode, _configuration)) {
				return await Post(entity, threadId);
			} else {
				return BadRequest("Invalid captcha");
			}
		}

		private async Task<ActionResult<Board>> Post(Board post, int threadId = 0) {
			var userIp = HttpContext.Connection.RemoteIpAddress;
			var threadPosts = await boardProvider.GetThreadAndResponses(threadId, -1);

			if (threadPosts is null || !threadPosts.First().ShouldDisplayShadowbanned(userIp)) {
				return BadRequest($"There is no thread with ID {threadId}.");
			}

			// only test when in response mode
			if (threadId != 0) {
				if (threadPosts.First().Archived) {
					return BadRequest("You can't respond to an archived thread.");
				}
			}

			await postCreator.Create(post, userIp, _configuration, threadPosts, MaxResponses, MaxImages, MaxThreads, ArchivedLifetime);

			return Created("post", new Dictionary<string, int> { { "id", post.Id } });
		}		

		// POST: Entity/report
		[HttpPost("report")]
		public virtual async Task<ActionResult> ReportPost([Bind("PostID, Reason, CaptchaCode")] Report report) {
			var userIp = HttpContext.Connection.RemoteIpAddress;
			if (string.IsNullOrEmpty(report.CaptchaCode) || !await Captcha.IsCaptchaValid(report.CaptchaCode, _configuration)) {
				return BadRequest("Invalid captcha");
			}

			if (!await PostExists(report.PostId, userIp)) {
				return BadRequest($"There is no post with ID {report.PostId}.");
			}

			report.Date = DateTime.UtcNow;
			report.PostBoard = typeof(Board).Name;
			report.ReportingIP = HttpContext.Connection.RemoteIpAddress.ToString();

			_context.Reports.Add(report);
			await _context.SaveChangesAsync();

			return Ok($"Post {report.PostId} was reported with the following reason: {report.Reason}");
		}

		#endregion POST

		#region DELETE

		// DELETE: Entity/post/42
		[HttpDelete("post/{id}")]
		public virtual async Task<ActionResult<Board>> DeleteEntity([FromBody] Delete delete, int id, [FromQuery] bool onlyImage = false){
			var entity = await _context.Set<Board>().FindAsync(id);
			if (entity == null) {
				return BadRequest("Post with this ID was not found.");
			}
			if (entity.Password == null || !entity.TestPassword(delete.Password)) {
				return BadRequest("Wrong password.");
			}
			if (!onlyImage) {
				_context.Set<Board>().Remove(entity);
				if (entity.Thread == null) {
					var responses = await _context.Set<Board>().Where(x => x.Thread == id).ToArrayAsync();
					_context.Set<Board>().RemoveRange(responses);
				}
				DeleteImage(typeof(Board).Name, id);
			} else {
				entity.Image = null;
				DeleteImage(typeof(Board).Name, id);
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
			// I regret deciding on this approach to generic controller. I regret it so much.
			var deleteList = new List<Board>();

			var archived = await _context.Set<Board>().Where(x => x.Archived && !x.Sticky).ToArrayAsync();

			// add thread responses to deletion
			foreach (var thread in archived) {
				var archivedResponses = await _context.Set<Board>().Where(x => x.Thread == thread.Id).ToArrayAsync();
				deleteList.AddRange(archivedResponses);

				// also delete images
				foreach (var response in archivedResponses) {
					if (!string.IsNullOrEmpty(response.Image)) {
						DeleteImage(typeof(Board).Name, response.Id);
					}
				}
				DeleteImage(typeof(Board).Name, thread.Id);
			}
			deleteList.AddRange(archived);

			_context.Set<Board>().RemoveRange(deleteList);

			await _context.SaveChangesAsync();
		}

		/// <summary>
		/// Checks if post exists and if it is shadowbanned, determines if it should be returned.
		/// </summary>
		/// <param name="id">Post ID.</param>
		/// <param name="userIp">Connecting IP used to check if shadowbanned post should be displayed.</param>
		private async Task<bool> PostExists(int id, IPAddress userIp) {
			var post = await _context.Set<Board>().Where(e => e.Id == id).FirstOrDefaultAsync();
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
		private async Task<bool> ThreadExists(int id, IPAddress userIp) {
			var post = await _context.Set<Board>().Where(e => e.Id == id && e.Thread == null).FirstOrDefaultAsync();
			if (post != null) {
				return post.ShouldDisplayShadowbanned(userIp);
			}
			return false;
		}

		/// <summary>
		/// Default action on accessing the board. Returns the full name of the board.
		/// </summary>
		public virtual string GetBoardName() {
			return BoardName;
		}

		#endregion Tools

		#region localhost

		#region DELETE
		// DELETE: Entity/post/admin/42
		[HttpDelete("post/admin/{id}")]
		[LoopbackOnly]
		public virtual async Task<ActionResult<Board>> AuthorizedDeleteEntity(int id) {
			var entity = await _context.Set<Board>().FindAsync(id);
			if (entity == null) {
				return BadRequest("Post with this ID was not found.");
			}

			_context.Set<Board>().Remove(entity);
			if (entity.Thread == null) {
				var responses = await _context.Set<Board>().Where(x => x.Thread == id).ToArrayAsync();
				_context.Set<Board>().RemoveRange(responses);
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
		public virtual async Task<ActionResult<Board>> AdministrativeCreateThread([Bind("Archived,Content,Name,Options,Sticky,Thread")] Board entity) {
			var adminLevel = PosterExtensions.GetPosterLevel(HttpContext.User);
			return await AdministrativePost(entity, adminLevel);
		}

		// POST: Entity/admin/response/42
		[HttpPost("admin/response/{threadId}")]
		[LoopbackOnly]
		[Authorize]
		public virtual async Task<ActionResult<Board>> AdministrativeCreateResponse([Bind("Archived,Content,Name,Options,Sticky,Thread")] Board entity, int threadId) {
			var adminLevel = PosterExtensions.GetPosterLevel(HttpContext.User);
			return await AdministrativePost(entity, adminLevel, threadId);
		}

		private async Task<ActionResult<Board>> AdministrativePost(Board entity, AdministrativeLevel posterLevel, int threadId = 0) {
			var userIp = HttpContext.Connection.RemoteIpAddress;

			entity.Password = null;
			entity.PosterIP = "localhost";
			entity.PosterLevel = posterLevel;
			entity.PostedOn = DateTime.UtcNow;

			// response posting mode
			if (threadId != 0) {
				if (!await ThreadExists(threadId, userIp)) {
					return BadRequest($"There is no thread with ID {threadId}.");
				}

				entity.Thread = threadId;
				var topic = _context.Set<Board>().Find(threadId);
				if (entity.Options == null || entity.Options.ToUpper() != "SAGE") {
					topic.LastPostDate = DateTime.UtcNow;
				}
				_context.Set<Board>().Add(entity);

			// topic posting mode
			} else {
				entity.Thread = null;
				entity.LastPostDate = DateTime.UtcNow;
				_context.Set<Board>().Add(entity);
			}

			await _context.SaveChangesAsync();
			_logger.LogInformation(
				string.Format("An administrative post /{0}/{1} was made. It's a {2}.",
								typeof(Board).Name.ToLower(),
								entity.Id,
								entity.Thread == null ? "new thread" : "response to /" + typeof(Board).Name.ToLower() + "/" + entity.Thread
				)
			);
			return Created("post", new Dictionary<string, int> { { "id", entity.Id } });
		}
		#endregion POST

		#endregion localhost
	}
}
