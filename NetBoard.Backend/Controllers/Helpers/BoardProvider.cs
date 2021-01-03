using Microsoft.EntityFrameworkCore;
using NetBoard.Model.Data;
using NetBoard.Model.ExtensionMethods;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace NetBoard.Controllers.Helpers {
	
	/// <summary>
	/// Class providing commonly used post/thread-retrieving methods.
	/// </summary>
	public class BoardProvider<Board> where Board : PostStructure {
		private readonly ApplicationDbContext _context;

		public BoardProvider(ApplicationDbContext context) {
			_context = context;
		}

		/// <summary>
		/// Returns total amount of threads on this board.
		/// </summary>
		public int GetTotalThreadCount() {
			return _context.Set<Board>().AsNoTracking().Where(x => x.Thread == null).Count();
		}

		/// <summary>
		/// Returns the oldest thread on this board.
		/// </summary>
		public async Task<Board> GetOldestThread() {
			return await _context.Set<Board>()
							.Where(x => x.Archived == false)
							.OrderByDescending(x => x.LastPostDate)
							.Take(1).FirstOrDefaultAsync();
		}

		/// <summary>
		/// Returns a single post.
		/// </summary>
		public async Task<Board> GetPostById(int id) {
			return await _context.Set<Board>()
							.FindAsync(id);
		}

		/// <summary>
		/// Gets image, response and unique poster count of provided thread ID.
		/// </summary>
		/// <param name="posts">Post list.</param>
		/// <param name="maxResponses">Max responses a thread on this board can take (used to calculate being past limits)</param>
		/// <param name="maxImages">Max images this a thread on this board can take (used to calculate being past limits)</param>
		/// <returns></returns>
		public Dictionary<string, int> GetThreadInfo(List<Board> posts, int maxResponses, int maxImages) {
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
		public async Task<List<Board>> GetThreadList() {
			return await _context.Set<Board>()
								.AsNoTracking()
								.OrderByDescending(x => x.Sticky).ThenByDescending(x => x.LastPostDate)
								.Where(x => x.Thread == null)
								.ToListAsync();
		}

		/// <summary>
		/// Gets a thread by ID.
		/// </summary>
		public async Task<Board> GetThread(int id) {
			return await _context.Set<Board>().FindAsync(id);
		}

		public async Task<List<Board>> GetThreadAndResponses(int id, int responseCount) {
			var noResponseLimit = responseCount < 1;
			List<Board> result;

			if (noResponseLimit) {
				result = await _context.Set<Board>()
									.AsNoTracking()
									.Where(x => x.Id == id || x.Thread == id)
									.OrderBy(x => x.Id)
									.ToListAsync();
			} else {
				result = await _context.Set<Board>()
									.AsNoTracking()
									.Where(x => x.Id == id)
									.Where(x => x.Thread == id)
									.OrderByDescending(x => x.Id)
									.ToListAsync();
			}

			return result;
		}

		/// <summary>
		/// Gets thread's last responses.
		/// </summary>
		/// <param name="count">Response count. If 0 or less, returns all.</param>
		/// <param name="threadId">Thread ID.</param>
		/// <returns>A list of last responses.</returns>
		public async Task<List<Board>> GetLastResponses(int count, int threadId) {
			var noResponseLimit = count < 1;
			List<Board> result;

			if (noResponseLimit) {
				result = await _context.Set<Board>()
									.AsNoTracking()
									.Where(x => x.Thread == threadId)
									.OrderByDescending(x => x.Id)
									.ToListAsync();
			} else {
				result = await _context.Set<Board>()
									.AsNoTracking()
									.Where(x => x.Thread == threadId)
									.OrderByDescending(x => x.Id)
									.Take(count)
									.ToListAsync();
			}

			return result;
		}

		public async Task<List<Board>> GetResponsesPastId(int threadId, int lastId) {
			var result = await _context.Set<Board>()
									.AsNoTracking()
									.Where(x => x.Thread == threadId && x.Id > lastId)
									.OrderBy(x => x.PostedOn)
									.ToListAsync();

			return result;
		}

		/// <summary>
		/// Gets threads on a specified <paramref name="page"/>.
		/// </summary>
		/// <param name="page">A page to look for threads in.</param>
		/// <param name="totalThreadCount">How many threads there is in total?</param>
		/// <param name="threadsPerPage">How many threads a page can hold?</param>
		/// <param name="isArchive">Should only look for archived threads?</param>
		/// <returns></returns>
		public async Task<List<Board>> GetThreadPage(int page, int totalThreadCount, int threadsPerPage, bool isArchive) {
			List<Board> threads;
			var tableName = _context.GetSchemaAndTable<Board>();
			// if results need to be paginated and requested page isn't == 1...
			if (totalThreadCount > threadsPerPage && page != 1) {
				// EF Core in this version seems to shit the bed when doing paginations (or it's Postgres support, at least)
				threads = await _context.Set<Board>()
											.FromSqlRaw(@$"
												SELECT * FROM {tableName} AS t
												WHERE (t.thread IS NULL) {(isArchive ? "AND (t.archived = true) OR (t.archived = true and (t.sticky = false))" : "AND (t.archived = false) OR (t.archived = true and (t.sticky = true))")}
												ORDER BY t.sticky DESC, t.last_post_date DESC
												LIMIT {threadsPerPage} OFFSET {(threadsPerPage * page) - threadsPerPage}
												")
											.ToListAsync();

			// otherwise just show first page, don't paginate
			} else {
				threads = await _context.Set<Board>()
										.FromSqlRaw(@$"
											SELECT * FROM {tableName} AS t
											WHERE (t.thread IS NULL) {(isArchive ? "AND (t.archived = true) OR (t.archived = true and (t.sticky = false))" : "AND (t.archived = false) OR (t.archived = true and (t.sticky = true))")}
											ORDER BY t.sticky DESC, t.last_post_date DESC
											LIMIT {threadsPerPage}
											")
										.ToListAsync();

			}
			return threads;
		}

		/// <summary>
		/// Iterates over all objects and turns them into DTO objects. It directly modifies items in the list, so make sure NOT to save!
		/// </summary>
		/// <param name="thread">Post list.</param>
		/// <param name="userIp">Connecting user IP.</param>
		public void MakeThreadDTO(List<Board> thread, IPAddress userIp) {
			for (int i = 0; i < thread.Count; i++) {
				if (i == 0) {
					thread.ElementAt(i).AsDTO(userIp, null, true);
				} else {
					thread.ElementAt(i).AsDTO(userIp, thread.First().Id);
				}
			}
		}

		public void RemoveThread(Board thread) {
			_context.Set<Board>().Remove(thread);
		}

		public void RemoveThreads(Board[] threads) {
			_context.Set<Board>().RemoveRange(threads);
		}
	}
}
