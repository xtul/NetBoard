using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NetBoard.Model.Data;
using NetBoard.Model.Data.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace NetBoard.Controllers.Helpers {
	public static class ShadowBans {
		/// <summary>
		/// Removes posts that are not supposed to be shown to a given <paramref name="ip"/>.
		/// </summary>
		/// <param name="posts">A list of posts to filter.</param>
		/// <param name="ip">Connecting user's IP.</param>
		public static void FilterShadowbanned(ref List<Post> posts, IPAddress ip) {
			// filter shadowbanned threads
			var deletionList = new List<Post>();
			foreach (var post in posts) {
				// if this shadowbanned post shouldn't be displayed, add it to deletion list
				if (!post.ShouldDisplayShadowbanned(ip)) {
					deletionList.Add(post);
				}
			}

			// delete posts that are shadowbanned
			// we can't do it within previous foreach because it throws InvalidOperation (modified collection)
			foreach (var post in deletionList) {
				posts.Remove(post);
			}
		}

		/// <summary>
		/// Shadowbans this post's IP.
		/// </summary>
		public static async Task ShadowbanPost(Post post, ApplicationDbContext context, IConfiguration queries, IConfiguration config, DateTime expiresOn, string reason, string board) {
			var ipToBan = post.PosterIP;
			var postsToShadowban = await context.Posts.FromSqlRaw(queries["GetPostsByIP"], board, ipToBan).ToListAsync();

			// mark all posts from this IP as shadowbanned
			string postsToBanString = "";
			for (int i = 0; i >= postsToShadowban.Count; i++) {
				if (i != postsToShadowban.Count) {
					postsToBanString += $"{postsToShadowban[i].Id}, ";
				} else {
					postsToBanString += $"{postsToShadowban[i].Id}";
				}
			}

			context.Posts.FromSqlRaw(queries["SetPostsShadowbanned"].Replace("{BOARD}", board), postsToBanString).AsNoTracking();

			// add this IP to appsettings
			var shadowBanList = config.GetSection("Bans:ShadowBanList").GetChildren().Select(c => c.Value).ToList();
			shadowBanList.Add(ipToBan);
			AppsettingsManipulation.AddOrUpdateAppSetting("Bans:ShadowBanList", shadowBanList);
		}

		public static bool IsIpShadowbanned(IPAddress userIp, IConfiguration config) {
			var shadowBanList = config.GetSection("Bans:ShadowBanList").GetChildren().Select(c => c.Value).ToList();

			return shadowBanList.Contains(userIp.ToString());
		}
	}
}
