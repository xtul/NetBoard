﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NetBoard.Model.Data;
using NetBoard.Model.ExtensionMethods;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using static NetBoard.Controllers.Helpers.ImageManipulation;

namespace NetBoard.Controllers.Helpers {
	public class PostCreator<Board> where Board : PostStructure {
		private readonly ApplicationDbContext _context;
		private readonly BoardProvider<Board> _boardProvider;
		private readonly ILogger<Board> _logger;

		public PostCreator(ApplicationDbContext context, BoardProvider<Board> boardProvider, ILogger<Board> logger) {
			_logger = logger;
			_context = context;
			_boardProvider = boardProvider;
		}

		/// <summary>
		/// Prepares the post for storage.
		/// </summary>
		private static void SetupPost(ref Board post, IPAddress userIp, IConfiguration config) {
			if (ShadowBans<Board>.IsIpShadowbanned(userIp, config)) {
				post.ShadowBanned = true;
			}

			if (!post.Password.IsNullOrEmptyWithTrim()) {
				post.SetPassword(post.Password);
			}

			post.PosterIP = userIp.ToString() ?? "localhost";
			post.PostedOn = DateTime.UtcNow;
		}

		private void CreateResponse(Board post, Board thread) {
			post.Thread = thread.Id;
			post.Sticky = false;
			post.Content = post.Content.Replace("\r", "");
			post.Content = post.Content.Trim('\n');
			thread.LastPostDate = DateTime.UtcNow;

			var isSaged = !string.IsNullOrEmpty(post.Options) && post.Options.ToUpper() == "SAGE";

			// if saged, save it for administration
			if (isSaged) {
				_context.Sages.Add(new Sage {
					TopicId = thread.Id,
					Board = typeof(Board).Name
				});
			}

			// bump the thread 
			if ((!thread.PastLimits.HasValue || thread.PastLimits.Value == false) && !isSaged)
			{
				thread.LastPostDate = DateTime.UtcNow;
			}

			_context.Set<Board>().Add(post);
		}

		private void CreateThread(Board post) {
			post.Thread = null;
			post.LastPostDate = DateTime.UtcNow;

			_context.Set<Board>().Add(post);
		}

		private void CheckIfPastLimits(ref List<Board> thread, int maxResponses, int maxImages) {
			var threadInfo = _boardProvider.GetThreadInfo(thread, maxResponses, maxImages);

			var responseCountExists = threadInfo.TryGetValue("responseCount", out int responseCount);
			var imageCountExists = threadInfo.TryGetValue("imageCount", out int imageCount);

			if (!responseCountExists) responseCount = 0;
			if (!imageCountExists) imageCount = 0;

			if (responseCount >= maxResponses || imageCount >= maxImages) {
				thread.First().PastLimits = true;
			}
		}

		/// <summary>
		/// Archives the oldest thread. Don't forget to save changes in DB.
		/// </summary>
		/// <param name="threadsAvailable">Amount of threads currently available.</param>
		private async Task ArchiveOldThreadAsync(int maxThreads, TimeSpan archivedLifetime) {
			var threadsAvailable = _boardProvider.GetTotalThreadCount();

			if (threadsAvailable >= maxThreads) {
				var oldestThread = await _boardProvider.GetOldestThread();
				oldestThread.Archived = true;
				_context.MarkedForDeletion.Add(new MarkedForDeletion
				{
					Board = typeof(Board).Name,
					PostId = oldestThread.Id,
					UtcDeletedOn = DateTime.UtcNow + archivedLifetime
				});
			}
		}

		/// <summary>
		/// Creates a new post 
		/// </summary>
		/// <param name="thread">If set to 0 or below, a new thread is created.</param>
		public async Task Create(Board post, IPAddress userIp, IConfiguration config, List<Board> thread, int maxResponses, int maxImages, int maxThreads, TimeSpan archivedLifetime) {
			SetupPost(ref post, userIp, config);
			var op = await _boardProvider.GetPostById(thread.First().Id);

			if (thread is null) {
				CreateThread(post);
				await ArchiveOldThreadAsync(maxThreads, archivedLifetime);
			} else {
				op.LastPostDate = DateTime.UtcNow;
				CreateResponse(post, thread.First());
				CheckIfPastLimits(ref thread, maxResponses, maxImages);
			}

			// save changes
			await _context.SaveChangesAsync();
			_logger.LogInformation(
				string.Format("A post /{0}/{1} was made. It's a {2}.",
								typeof(Board).Name.ToLower(),
								post.Id,
								post.Thread == null ? "new thread" : "response to /" + typeof(Board).Name.ToLower() + "/thread/" + post.Thread
				)
			);

			// if image token was provided try to handle it (done last because we need post ID)
			if (post.Image != null) {
				post = HandleImagePosting(post, op.Id == 0 ? ThumbType.thread : ThumbType.response);
				if (post == null) {
					_logger.LogInformation("However, image saving has failed.");
				}
			}

			await _context.SaveChangesAsync();
		}

		/// <summary>
		/// Gets the image token from entity and verifies if it was enqueued.
		/// Turns image token into image URI and moves the image from temporary directory to wwwroot/img.
		/// It has to run after the post was made because it requires post ID to be assigned.
		/// Don't forget to save changes.
		/// </summary>
		/// <param name="entity">Entity to take the token from.</param>
		/// <returns>Modified entity or null if couldn't convert image.</returns>
		private Board HandleImagePosting(Board entity, ThumbType mode) {
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
			if (!File.Exists(filePath)) {
				_logger.LogError($"{errorString}, but image was not found in temporary directory.");
				return null;
			}

			// move to a persistent directory
			var newFilePath = Path.Combine(wwwroot, "img", typeof(Board).Name.ToLower(), entity.Id.ToString());
			if (!Directory.Exists(newFilePath)) {
				Directory.CreateDirectory(newFilePath);
			}
			// skip timestamp that was added in image queue
			var finalFileName = Path.GetFileName(queueEntry.Filename).Split('.').Skip(1).ToArray();
			var newFileName = string.Join(".", finalFileName).Replace(" ", "_");
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
	}
}
