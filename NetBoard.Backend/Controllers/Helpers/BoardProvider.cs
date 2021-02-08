using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NetBoard.Model.Data;
using NetBoard.Model.ExtensionMethods;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace NetBoard.Controllers.Helpers {

	/// <summary>
	/// Class providing commonly used post/thread-retrieving methods.
	/// </summary>
	public class BoardProvider {
		private readonly ApplicationDbContext _context;
		private readonly IConfiguration _queries;

		public BoardProvider(ApplicationDbContext context, IConfiguration config) {
			_context = context;

			var databaseType = config["DatabaseType"];
			_queries = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile($"queries.{databaseType}.json", false, true).Build();
		}

		/// <summary>
		/// Returns total amount of threads on this board.
		/// </summary>
		public int GetTotalThreadCount(string board) {
			return _context.Posts.FromSqlRaw(_queries["GetAllThreadsMin"].Replace("{BOARD}", board)).AsNoTracking().Count();
		}

		/// <summary>
		/// Returns the oldest thread on this board.
		/// </summary>
		public async Task<Post> GetOldestThread(string board) {
			return await _context.Posts.FromSqlRaw(_queries["GetOldestThread"].Replace("{BOARD}", board)).AsNoTracking().FirstOrDefaultAsync();
		}

		/// <summary>
		/// Returns a single post.
		/// </summary>
		public async Task<Post> GetPostById(int id, string board) {
			return await _context.Posts.FromSqlRaw(_queries["GetPostById"].Replace("{BOARD}", board), id).AsNoTracking().FirstOrDefaultAsync();
		}

		public async Task<Post[]> GetArchivedThreads() {
			return await _context.Posts.FromSqlRaw(_queries["GetArchivedThreads"]).ToArrayAsync();
		}

		/// <summary>
		/// Gets image, response and unique poster count of provided thread ID.
		/// </summary>
		/// <param name="posts">Post list.</param>
		/// <param name="maxResponses">Max responses a thread on this board can take (used to calculate being past limits)</param>
		/// <param name="maxImages">Max images this a thread on this board can take (used to calculate being past limits)</param>
		/// <returns></returns>
		public Dictionary<string, int> GetThreadInfo(List<Post> posts, int maxResponses, int maxImages) {
			var imageCount = posts.Where(x => x.Image != null).Count();
			var responseCount = posts.Count - 1; // exclude OP

			var posters = posts.Select(p => p.PosterIP).ToArray();
			var uniquePosters = posters.Distinct().Count();

			var isPastLimits = responseCount >= maxResponses || imageCount >= maxImages;

			var result = new Dictionary<string, int>() {
				{ "imageCount", imageCount },
				{ "responseCount", responseCount },
				{ "uniquePosters", uniquePosters },
				{ "pastLimits", isPastLimits ? 1 : 0 }
			};

			return result;
		}

		/// <summary>
		/// Returns a list of threads.
		/// </summary>
		public async Task<List<Post>> GetThreadList(string board) {
			return await _context.Posts.FromSqlRaw(_queries["GetThreadList"].Replace("{BOARD}", board)).ToListAsync();
		}

		/// <summary>
		/// Gets a thread by ID.
		/// </summary>
		public async Task<Post> GetThread(int id, string board) {
			var post = await GetPostById(id, board);

			if (post.Thread is null) {
				return post;
			} else {
				return null;
			}
		}

		/// <summary>
		/// Gets thread and it's last responses. Also see <see cref="GetLastResponses(int, int)"/>.
		/// </summary>
		/// <param name="threadId">Thread ID.</param>
		/// <param name="responseCount">How many responses to fetch? "0" will fetch all.</param>
		/// <param name="removeOp">Should OP be removed from fetching? Only used when <paramref name="responseCount"/> is above 0.</param>
		/// <returns></returns>
		public async Task<List<Post>> GetThreadAndResponses(int threadId, int responseCount, string board, bool removeOp = false) {
			var responseLimit = responseCount > 0;			
			var result = await _context.Posts.FromSqlRaw(_queries["GetThreadAndResponses"].Replace("{BOARD}", board), threadId).AsNoTracking().ToListAsync();
			result = result.OrderBy(x => x.Id).ToList();

			if (responseLimit) {
				if (removeOp) {
					result.RemoveAt(0);
					result.RemoveRange(0, Math.Clamp(result.Count - 3, 0, result.Count)); 
				} else {
					// if we want to keep OP, need to remove 1 from count
					result.RemoveRange(1, Math.Clamp(result.Count - 4, 0, result.Count)); 
				}
			}

			return result;
		}

		/// <summary>
		/// Gets thread's last responses.
		/// </summary>
		/// <param name="responseCount">Response count. If 0 or less, returns all.</param>
		/// <param name="threadId">Thread ID.</param>
		/// <returns>A list of last responses.</returns>
		public async Task<List<Post>> GetLastResponses(int responseCount, int threadId, string board) {
			return await GetThreadAndResponses(threadId, responseCount, board, true);
		}

		public async Task<List<Post>> GetResponsesPastId(int threadId, int lastId, string board) {
			return await _context.Posts.FromSqlRaw(_queries["GetResponsesPastId"].Replace("{BOARD}", board), threadId, lastId).AsNoTracking().ToListAsync();
		}

		/// <summary>
		/// Gets threads on a specified <paramref name="page"/>.
		/// </summary>
		/// <param name="page">A page to look for threads in.</param>
		/// <param name="totalThreadCount">How many threads there is in total?</param>
		/// <param name="threadsPerPage">How many threads a page can hold?</param>
		/// <param name="isArchive">Should only look for archived threads?</param>
		/// <returns></returns>
		public async Task<List<Post>> GetThreadPage(int page, int totalThreadCount, int threadsPerPage, bool isArchive, string board) {
			List<Post> threads;

			// if results need to be paginated and requested page isn't == 1...
			if (totalThreadCount > threadsPerPage && page != 1) {
				if (isArchive) {
					threads = await _context.Posts.FromSqlRaw(_queries["GetThreadPage_Archived"].Replace("{BOARD}", board), threadsPerPage, (threadsPerPage * page) - threadsPerPage).ToListAsync();
				} else {
					threads = await _context.Posts.FromSqlRaw(_queries["GetThreadPage"].Replace("{BOARD}", board), threadsPerPage, (threadsPerPage * page) - threadsPerPage).ToListAsync();
				}
			// otherwise just show first page, don't paginate
			} else {
				if (isArchive) {
					threads = await _context.Posts.FromSqlRaw(_queries["GetThreadPage_OnlyFirstPage_Archived"].Replace("{BOARD}", board), threadsPerPage, (threadsPerPage * page) - threadsPerPage).ToListAsync();
				} else {
					threads = await _context.Posts.FromSqlRaw(_queries["GetThreadPage_OnlyFirstPage"].Replace("{BOARD}", board), threadsPerPage, (threadsPerPage * page) - threadsPerPage).ToListAsync();
				}
			}
			return threads;
		}

		/// <summary>
		/// Iterates over all objects and turns them into DTO objects. It directly modifies items in the list, so make sure NOT to save!
		/// </summary>
		/// <param name="thread">Post list.</param>
		/// <param name="userIp">Connecting user IP.</param>
		public void MakeThreadDTO(List<Post> thread, IPAddress userIp) {
			for (int i = 0; i < thread.Count; i++) {
				if (i == 0) {
					thread.ElementAt(i).AsDTO(userIp, null, true);
				} else {
					thread.ElementAt(i).AsDTO(userIp, thread.First().Id);
				}
			}
		}

		/// <summary>
		/// Removes thread and all it's responses.
		/// </summary>
		public async Task RemoveThread(int threadId, string board) {
			var thread = await GetThreadAndResponses(threadId, 0, board);

			if (thread is null) {
				return;
			}

			_context.Posts.FromSqlRaw(_queries["RemoveThread"].Replace("{BOARD}", board), threadId);

			// we need to get rid of images as well
			foreach (var post in thread) {
				ImageManipulation.DeleteImage(board, post.Id);
			}

		}

		/// <summary>
		/// Removes threads and all their responses.
		/// </summary>
		public async Task RemoveThreads(int[] threads, string board) {
			// not at all efficient, but you rarely need to purge an entire board
			// definitely could use an entirely new query though
			foreach (var threadId in threads) {
				await RemoveThread(threadId, board);
			}
		}

		/// <summary>
		/// Removes a specific post. It can't delete threads, use <see cref="RemoveThread(int)"/> instead.
		/// </summary>
		public async Task RemovePost(int postId, string board) {
			var post = await GetPostById(postId, board);

			if (post is not null && post.Thread is not null) {
				_context.Posts.FromSqlRaw(_queries["RemovePost"], postId);
			}
		}
	}
}
