using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NetBoard.Controllers.Helpers;
using NetBoard.Model.Data;
using NetBoard.Model.ExtensionMethods;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static NetBoard.Controllers.Helpers.ImageManipulation;
using static NetBoard.Model.Data.PostStructure;

namespace NetBoard.Controllers.Generic {
	[ApiController]
	public class BoardController<BoardPosts> : ControllerBase where BoardPosts : PostStructure {
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
		private readonly ILogger<BoardPosts> _logger;

		public BoardController(ApplicationDbContext context, IConfiguration configuration, ILogger<BoardPosts> logger) {
			_context = context;
			_configuration = configuration;
			_logger = logger;
			MaxThreads = ThreadsPerPage * MaxPages;
		}

		#endregion Constructor

		#region GET

		// GET: Entity/2
		/// <summary>
		/// Gets all threads from provided board.
		/// </summary>
		/// <param name="mode">Either 'catalog', 'archive' or page number.</param>
		[HttpGet("{mode}")]
		public virtual async Task<ActionResult<Dictionary<string, object>>> GetThreads(string mode) {
			var catalog = false;
			var archive = false;
			var pageNumber = 0;
			if (mode == "catalog") {
				catalog = true;
			} else if (mode == "archive") {
				archive = true;
			} else if (int.TryParse(mode, out int n)) {
				pageNumber = n;
			} else {
				return BadRequest($"Expected page number, 'archive' or 'catalog', but received '{mode}'.");
			}

			int totalThreads = _context.Set<BoardPosts>().AsNoTracking().Where(x => x.Thread == null).Count();
			if (totalThreads < 1) return Ok("No threads on this board.");
			var pageCount = (int)Math.Ceiling((double)totalThreads / ThreadsPerPage);

			// don't allow navigating to pages that don't exist
			if (pageNumber < 1) pageNumber = 1;
			if (pageNumber > pageCount) pageNumber = pageCount;

			// grab and sort all threads (stickies first, then by activity)
			List<BoardPosts> threads;

			// load all threads in catalog mode
			if (catalog) {
				threads = await _context.Set<BoardPosts>()
										.AsNoTracking()
										.OrderByDescending(x => x.Sticky).ThenByDescending(x => x.LastPostDate)
										.Where(x => x.Thread == null)
										.ToListAsync();
			// otherwise implement pagination
			} else {
				var tableName = _context.GetSchemaAndTable<BoardPosts>();
				// if results need to be paginated and requested page isn't == 1...
				if (totalThreads > ThreadsPerPage && pageNumber != 1) {
					// EF Core doesn't seem to handle pagination well, had to make the query myself
					threads = await _context.Set<BoardPosts>()
												.FromSqlRaw(@$"
													SELECT * FROM {tableName} AS t
														WHERE (t.thread IS NULL) {(archive ? "AND (t.archived = true)" : "AND (t.archived = false)")}
														ORDER BY t.sticky DESC, t.last_post_date DESC
														LIMIT {ThreadsPerPage} OFFSET {(ThreadsPerPage * pageNumber) - ThreadsPerPage}
												")
												.ToListAsync();
				// otherwise just show first page
				} else {
					threads = await _context.Set<BoardPosts>()
											.FromSqlRaw(@$"
													SELECT * FROM {tableName} AS t
														WHERE (t.thread IS NULL) {(archive ? "AND (t.archived = true)" : "AND (t.archived = false)")}
														ORDER BY t.sticky DESC, t.last_post_date DESC
														LIMIT {ThreadsPerPage}
											")
											.ToListAsync();
				}
			}

			// prepare threads for response (ie. cut undesired data)
			int previewLength = int.Parse(_configuration["PreviewLength"]);
			string cutoffText = _configuration["CutoffText"];

			foreach (var thread in threads) {
				// don't display sensitive/useless data
				thread.Password = null;
				thread.PosterIP = null;
				if (thread.Image == null) thread.SpoilerImage = null;
				thread.Content = thread.Content.ReduceLength(previewLength, cutoffText);
				
				thread.ResponseCount = await _context.Set<BoardPosts>().CountAsync(x => x.Thread == thread.Id);
				thread.ImageCount = await _context.Set<BoardPosts>().CountAsync(x => x.Thread == thread.Id && x.Image != null);
				// also add last 3 replies if not in catalog mode
				if (!catalog) {
					var lastResponses = await _context.Set<BoardPosts>()
														.AsNoTracking()
														.Where(x => x.Thread == thread.Id)
														.OrderByDescending(x => x.Id)
														.Take(3)
														.Cast<PostStructure>()
														.ToListAsync();
					if (lastResponses.Count > 0) {
						var tempList = new List<PostStructure>();
						foreach (var r in lastResponses) {
							var dto = new PostStructure {
								Id = r.Id,
								Content = r.Content,
								Name = r.Name,
								PostedOn = r.PostedOn,
								PosterLevel = r.PosterLevel,
								Thread = thread.Id
							};
							dto.Content = dto.Content.ReduceLength(previewLength, cutoffText);
							if (r.Image != null) {
								dto.Image = r.Image;
								dto.SpoilerImage = null;
							}
							tempList.Insert(0, dto); // .Add() would reverse the list
						}
						thread.Responses = tempList;
					}
				}
			}

			Dictionary<string, object> pageData;
			if (catalog) {
				pageData = new Dictionary<string, object> {
					{ "board", typeof(BoardPosts).Name.ToLower() },
					{ "totalThreads", totalThreads },
				};
			} else {
				pageData = new Dictionary<string, object> {
					{ "board", typeof(BoardPosts).Name.ToLower() },
					{ "currentPage", pageNumber },
					{ "pageCount", pageCount },
					{ "totalThreads", totalThreads },
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
			// check if this thread exists
			if (!ThreadExists(id))
				return NotFound($"There's no thread with ID {id}.");

			var op = await _context.Set<BoardPosts>()
									.AsNoTracking()
									.Where(x => x.Id == id)
									.FirstOrDefaultAsync();

			if (op == null) {
				return NotFound($"There's no thread with ID {id}.");
			}

			// construct OP response
			var opDTO = new PostStructure {
				Id = op.Id,
				Subject = op.Subject,
				Name = op.Name,
				Content = op.Content,
				PostedOn = op.PostedOn,
				LastPostDate = op.LastPostDate,
				PosterLevel = op.PosterLevel,
				Thread = op.Id
			};
			if (op.Image != null) {
				opDTO.Image = op.Image;
				opDTO.SpoilerImage = op.SpoilerImage;
			}

			var posts = new List<PostStructure> {
				opDTO
			};

			// add responses if they exist
			var responses = await _context.Set<BoardPosts>()
											.AsNoTracking()
											.Where(x => x.Thread == id)
											.OrderBy(x => x.PostedOn)
											.ToListAsync();

			if (responses.Count > 0) {
				foreach (var r in responses) {
					var rDTO = new PostStructure {
						Id = r.Id,
						Subject = r.Subject,
						Name = r.Name,
						Content = r.Content,
						PostedOn = r.PostedOn,
						LastPostDate = r.LastPostDate,
						PosterLevel = r.PosterLevel,
						Thread = op.Id
					};
					if (r.Image != null) {
						rDTO.Image = r.Image;
						rDTO.SpoilerImage = r.SpoilerImage;
					}
					posts.Add(rDTO);
				}
			}

			// add thread data

			var result = new Dictionary<string, object> {
				{ "board", typeof(BoardPosts).Name.ToLower() },
				{ "posts", posts }
			};

			return result;
		}

		// GET: Entity/thread/42/149921324
		/// <summary>
		/// Gets all posts that are after certain ID.
		/// </summary>
		[HttpGet("thread/{id}/{lastId}")]
		public virtual async Task<ActionResult<PostStructure[]>> GetNewPosts(int threadId, int lastId) {
			// check if this thread exists
			if (!ThreadExists(threadId))
				return NotFound($"There's no thread with ID {threadId}.");

			// get new responses
			var responses = await _context.Set<BoardPosts>()
									.AsNoTracking()
									.Where(x => x.Thread == threadId && x.Id > lastId)
									.OrderBy(x => x.PostedOn)
									.ToListAsync();

			if (responses.Count == 0) {
				return NotFound("There are no new responses");
			}

			var responsesDto = new List<PostStructure>();
			foreach (var r in responses) {
				var rDTO = new PostStructure {
					Id = r.Id,
					Subject = r.Subject,
					Name = r.Name,
					Content = r.Content,
					PostedOn = r.PostedOn,
					LastPostDate = r.LastPostDate,
					PosterLevel = r.PosterLevel
				};
				if (r.Image != null) {
					rDTO.Image = r.Image;
					rDTO.SpoilerImage = r.SpoilerImage;
				}
				responsesDto.Add(rDTO);
			}

			return responsesDto.ToArray();
		}

		// GET: Entity/thread/42/posterCount
		/// <summary>
		/// Gets the count of posters in given thread.
		/// </summary>
		[HttpGet("thread/{id}/posterCount")]
		public virtual async Task<int> GetPosterCount(int id) {
			var query = await GetThread(id);
			var responses = query.Value["posts"] as List<PostStructure>;
			if (responses.Count < 2) {
				return 1;
			} else {
				return responses.Select(x => x.PosterIP).Distinct().Count();
			}
		}

		// GET: Entity/post/42
		/// <summary>
		/// Gets a single post.
		/// </summary>
		[HttpGet("post/{id}")]
		public virtual async Task<ActionResult<PostStructure>> GetPost(int id) {
			var post = await _context.Set<BoardPosts>().FindAsync(id);

			if (post == null) {
				return NotFound($"There's no post with ID {id}.");
			}

			// construct response
			var dto = new PostStructure {
				Id = post.Id,
				Subject = post.Subject,
				Name = post.Name,
				Content = post.ContentShort,
				PostedOn = post.PostedOn,
			};
			if (post.Image != null) {
				dto.Image = post.Image;
				dto.SpoilerImage = post.SpoilerImage;
			}

			return dto;
		}

		// GET: Entity/post/42/thread
		/// <summary>
		/// Gets a thread of provided post <paramref name="id"/>.
		/// </summary>
		/// <returns>Thread ID.</returns>
		[HttpGet("post/{id}/thread")]
		public virtual async Task<ActionResult> GetPostThread(int id) {
			var post = await _context.Set<BoardPosts>().FindAsync(id);

			if (post == null) {
				return NotFound($"There's no thread with ID {id}.");
			}

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
		public virtual async Task<ActionResult<BoardPosts>> CreateThread([Bind("Image, Content, Name, Password, Subject, Options")] BoardPosts entity) {
			return await Post(entity);
		}		

		// POST: Entity/thread/42
		[HttpPost("thread/{threadId}")]
		public virtual async Task<ActionResult<BoardPosts>> CreateResponse([Bind("Content, Name, Password, Options")] BoardPosts entity, int threadId) {
			return await Post(entity, threadId);
		}

		private async Task<ActionResult<BoardPosts>> Post(BoardPosts entity, int threadId = 0) {
			entity = FixPostedEntity(entity);
			if (!entity.Password.IsNullOrEmptyWithTrim()) {
				entity.SetPassword(entity.Password);
			}

			// response posting mode
			if (threadId != 0) {				
				var thread = _context.Set<BoardPosts>().Find(threadId);				
				if (thread == null) {
					return NotFound($"There is no thread with ID {threadId}.");
				}
				if (thread.Archived) {
					return BadRequest("You can't respond to an archived thread.");
				}

				HandleResponsePosting(entity, thread);
				HandleLimitExceedings(thread);

			// topic posting mode
			} else {
				entity.Thread = null;
				entity.LastPostDate = DateTime.UtcNow;
				entity.PostedOn = DateTime.UtcNow;
				entity.PosterIP = HttpContext.Connection.RemoteIpAddress.ToString();
				_context.Set<BoardPosts>().Add(entity);
				int boardThreadCount = _context.Set<BoardPosts>().AsNoTracking().Where(x => x.Thread == null).Count();
				ArchiveOldThread(boardThreadCount);
			}


			// save changes
			await _context.SaveChangesAsync();
			_logger.LogInformation(
				string.Format("A post /{0}/{1} was made. It's a {2}.",
								typeof(BoardPosts).Name.ToLower(),
								entity.Id,
								entity.Thread == null ? "new thread" : "response to /" + typeof(BoardPosts).Name.ToLower() + "/" + entity.Thread
				)
			);

			// if image token was provided try to handle it (done last because we need post ID)
			if (entity.Image != null) {
				entity = HandleImagePosting(entity, threadId == 0 ? ThumbType.thread : ThumbType.response);
				if (entity == null) {
					_logger.LogInformation("However, image saving has failed.");
					return BadRequest("Error when posting image. Contact administration if this happens consistently.");
				}
			}

			await _context.SaveChangesAsync();

			return Created("post", new Dictionary<string, int> { { "id", entity.Id } });
		}		

		// POST: Entity/report
		[HttpPost("report")]
		public virtual async Task<ActionResult> ReportPost([Bind("PostID, Reason")] Report report) {
			if (!PostExists(report.PostId)) {
				return BadRequest($"There is no post with ID {report.PostId}.");
			}

			report.Date = DateTime.UtcNow;
			report.PostBoard = typeof(BoardPosts).Name;
			report.ReportingIP = HttpContext.Connection.RemoteIpAddress.ToString();

			_context.Reports.Add(report);
			await _context.SaveChangesAsync();

			return Ok($"Post {report.PostId} was reported with the following reason: {report.Reason}");
		}

		#endregion POST

		#region DELETE

		// DELETE: Entity/post/42
		[HttpDelete("post/{id}")]
		public virtual async Task<ActionResult<BoardPosts>> DeleteEntity([FromBody] Delete delete, int id, [FromQuery] bool onlyImage = false){
			var entity = await _context.Set<BoardPosts>().FindAsync(id);
			if (entity == null) {
				return NotFound("Post with this ID was not found.");
			}
			if (entity.Password == null || !entity.TestPassword(delete.Password)) {
				return BadRequest("Wrong password.");
			}
			if (!onlyImage) {
				_context.Set<BoardPosts>().Remove(entity);
				if (entity.Thread == null) {
					var responses = await _context.Set<BoardPosts>().Where(x => x.Thread == id).ToArrayAsync();
					_context.Set<BoardPosts>().RemoveRange(responses);
				}
				DeleteImage(typeof(BoardPosts).Name, id);
			} else {
				entity.Image = null;
				DeleteImage(typeof(BoardPosts).Name, id);
			}
			await _context.SaveChangesAsync();
			_logger.LogInformation($"A post with ID {id} was deleted.");
			return Ok();
		}
		#endregion DELETE

		#region Tools

		/// <summary>
		/// Various stuff to clean up incoming entities, eg. nullify SpoilerImage if no Image was provided.
		/// </summary>
		/// <param name="entity">An entity to clean up.</param>
		/// <returns>A cleaned up entity.</returns>
		private BoardPosts FixPostedEntity(BoardPosts entity) {
			if (entity.Image.IsNullOrEmptyWithTrim()) {
				entity.Image = null;
				entity.SpoilerImage = null;
			}

			return entity;
		}

		private bool PostExists(int id) {
			return _context.Set<BoardPosts>().Any(e => e.Id == id);
		}

		private bool ThreadExists(int id) {
			return _context.Set<BoardPosts>().Any(e => e.Id == id && e.Thread == null);
		}

		public virtual string GetBoardName() {
			return BoardName;
		}

		/// <summary>
		/// Archives the oldest thread. Don't forget to save changes in DB.
		/// </summary>
		/// <param name="threadsAvailable">Amount of threads currently available.</param>
		private void ArchiveOldThread(int threadsAvailable) {
			if (MaxThreads == threadsAvailable) {
				var oldestTopic = _context	.Set<BoardPosts>()
											.Where(x => x.Archived == false)
											.OrderByDescending(x => x.LastPostDate)
											.Take(1).FirstOrDefault();
				oldestTopic.Archived = true;
				_context.MarkedForDeletion.Add(new MarkedForDeletion { 
					Board = typeof(BoardPosts).Name,
					PostId = oldestTopic.Id,
					UtcDeletedOn = DateTime.UtcNow + ArchivedLifetime
				});
			}
		}

		/// <summary>
		/// Marks the thread as past limits if required.
		/// </summary>
		/// <param name="thread"></param>
		private void HandleLimitExceedings(BoardPosts thread) {
			if (!thread.ResponseCount.HasValue) return;
			if (!thread.ImageCount.HasValue) return;
			if (MaxResponses >= thread.ResponseCount.Value || MaxImages >= thread.ImageCount.Value) {
				thread.PastLimits = true;
			}
		}

		/// <summary>
		/// Adds provided response to the database. Don't forget to save DB.
		/// </summary>
		private void HandleResponsePosting(BoardPosts response, BoardPosts thread) {
			response.Thread = thread.Id;
			response.PostedOn = DateTime.UtcNow;
			response.Sticky = false;
			response.PosterIP = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "localhost";
			response.Content = response.Content.Replace("\r", "");
			response.Content = response.Content.Trim('\n');

			thread.ResponseCount++;
			if (response.Image != null) {
				thread.ImageCount++;
			}

			if (response.PastLimits == true || (response.Options != null && response.Options.ToUpper() == "SAGE")) {
				// prevent bumping of the topic
				if (!response.PastLimits == true) {
					// if saged, save it for administration
					_context.Sages.Add(new Sage {
						TopicId = thread.Id,
						Board = typeof(BoardPosts).Name
					});
				}
			} else {
				thread.LastPostDate = DateTime.UtcNow;
			}
			_context.Set<BoardPosts>().Add(response);
		}

		/// <summary>
		/// Gets the image token from entity and verifies if it was enqueued.
		/// Turns image token into image URI and moves the image from temporary directory to wwwroot/img.
		/// It has to run after the post was made because it requires post ID to be assigned.
		/// Don't forget to save changes.
		/// </summary>
		/// <param name="entity">Entity to take the token from.</param>
		/// <returns>Modified entity or null if couldn't convert image.</returns>
		private BoardPosts HandleImagePosting(BoardPosts entity, ThumbType mode) {
			var errorString = "Tried to assign image token to a post";
			var tempDir = Path.Combine(Path.GetDirectoryName(typeof(Startup).Assembly.Location), "tempImages");
			var wwwroot = Path.Combine(Path.GetDirectoryName(typeof(Startup).Assembly.Location), "wwwroot");

			// find entry in database
			var queueEntry = _context.ImageQueue.Where(x => x.Token == entity.Image).FirstOrDefault();
			if (queueEntry == null) {
				_logger.LogError($"{errorString}, but no DB entry was found.");
				return null;
			}

			// get image from temporary directory
			var filePath = Path.Combine(tempDir, queueEntry.Filename);
			if (!System.IO.File.Exists(filePath)) {
				_logger.LogError($"{errorString}, but image was not found in temporary directory.");
				return null;
			}

			// move to a persistent directory
			var newFilePath = Path.Combine(wwwroot, "img", typeof(BoardPosts).Name.ToLower(), entity.Id.ToString());
			if (!Directory.Exists(newFilePath)) {
				Directory.CreateDirectory(newFilePath);
			}
			// skip timestamp that was added in image queue
			var finalFileName = Path.GetFileName(queueEntry.Filename).Split('.').Skip(1).ToArray();
			var newFileName = string.Join(".", finalFileName);
			System.IO.File.Move(filePath, Path.Combine(newFilePath, newFileName));

			// generate a thumbnail and save it to directory alongside original
			var thumbnail = GenerateThumbnail(Path.Combine(newFilePath, newFileName), mode);

			// since everything is OK, save everything to DB
			// DB needs a relative path
			var relativeImagePath = Path.Combine(newFilePath, newFileName).ToRelativePath("wwwroot");
			queueEntry.AssignedPost = entity.Id;
			entity.Image = relativeImagePath;

			return entity;
		}

		#endregion Tools

		#region localhost

		#region DELETE
		// DELETE: Entity/post/admin/42
		[HttpDelete("post/admin/{id}")]
		[LoopbackOnly]
		public virtual async Task<ActionResult<BoardPosts>> AuthorizedDeleteEntity(int id) {
			var entity = await _context.Set<BoardPosts>().FindAsync(id);
			if (entity == null) {
				return NotFound("Post with this ID was not found.");
			}

			_context.Set<BoardPosts>().Remove(entity);
			if (entity.Thread == null) {
				var responses = await _context.Set<BoardPosts>().Where(x => x.Thread == id).ToArrayAsync();
				_context.Set<BoardPosts>().RemoveRange(responses);
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
		public virtual async Task<ActionResult<BoardPosts>> AdministrativeCreateThread([Bind("Archived,Content,Name,Options,Sticky,Thread")] BoardPosts entity) {
			var adminLevel = PosterExtensions.GetPosterLevel(HttpContext.User);
			return await AdministrativePost(entity, adminLevel);
		}

		// POST: Entity/admin/response/42
		[HttpPost("admin/response/{threadId}")]
		[LoopbackOnly]
		[Authorize]
		public virtual async Task<ActionResult<BoardPosts>> AdministrativeCreateResponse([Bind("Archived,Content,Name,Options,Sticky,Thread")] BoardPosts entity, int threadId) {
			var adminLevel = PosterExtensions.GetPosterLevel(HttpContext.User);
			return await AdministrativePost(entity, adminLevel, threadId);
		}

		private async Task<ActionResult<BoardPosts>> AdministrativePost(BoardPosts entity, AdministrativeLevel posterLevel, int threadId = 0) {
			entity = FixPostedEntity(entity);

			entity.Password = null;
			entity.PosterIP = "localhost";
			entity.PosterLevel = posterLevel;
			entity.PostedOn = DateTime.UtcNow;

			// response posting mode
			if (threadId != 0) {
				if (!ThreadExists(threadId)) {
					return NotFound($"There is no thread with ID {threadId}.");
				}

				entity.Thread = threadId;
				var topic = _context.Set<BoardPosts>().Find(threadId);
				if (entity.Options == null || entity.Options.ToUpper() != "SAGE") {
					topic.LastPostDate = DateTime.UtcNow;
				}
				_context.Set<BoardPosts>().Add(entity);

			// topic posting mode
			} else {
				entity.Thread = null;
				entity.LastPostDate = DateTime.UtcNow;
				_context.Set<BoardPosts>().Add(entity);
			}

			await _context.SaveChangesAsync();
			_logger.LogInformation(
				string.Format("An administrative post /{0}/{1} was made. It's a {2}.",
								typeof(BoardPosts).Name.ToLower(),
								entity.Id,
								entity.Thread == null ? "new thread" : "response to /" + typeof(BoardPosts).Name.ToLower() + "/" + entity.Thread
				)
			);
			return Created("post", new Dictionary<string, int> { { "id", entity.Id } });
		}
		#endregion POST

		#endregion localhost
	}
}
