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
	public static class ShadowBans<BoardPost> where BoardPost : PostStructure {
		/// <summary>
		/// Removes posts that are not supposed to be shown to a given <paramref name="ip"/>.
		/// </summary>
		/// <param name="posts">A list of posts to filter.</param>
		/// <param name="ip">Connecting user's IP.</param>
		public static void FilterShadowbanned(ref List<BoardPost> posts, IPAddress ip) {
			// filter shadowbanned threads
			var deletionList = new List<BoardPost>();
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
		/// Shadowbans this post's IP, updating info in DB and adding poster IP to banned list. Make sure to save changes to DB afterwards.
		/// </summary>
		public static async Task ShadowbanPost(BoardPost post, ApplicationDbContext context, IConfiguration config, DateTime expiresOn, string reason) {
			var ipToBan = post.PosterIP;

			// mark all posts from this IP as shadowbanned
			var postsToShadowban = await context.Set<BoardPost>().Where(x => x.PosterIP == ipToBan).ToListAsync();
			foreach (var postToShadowban in postsToShadowban) {
				postToShadowban.ShadowBanned = true;
			}

			// add this IP to appsettings
			var shadowBanList = config.GetSection("Bans:ShadowBanList").GetChildren().Select(c => c.Value).ToList();
			shadowBanList.Add(ipToBan);
			AppsettingsManipulation.AddOrUpdateAppSetting("Bans:ShadowBanList", shadowBanList);

			// store in DB
			context.Bans.Add(new Ban {
				ExpiresOn = expiresOn,
				Reason = reason
			});

			await context.SaveChangesAsync();
		}

		public static bool IsIpShadowbanned(IPAddress userIp, IConfiguration config) {
			var shadowBanList = config.GetSection("Bans:ShadowBanList").GetChildren().Select(c => c.Value).ToList();

			return shadowBanList.Contains(userIp.ToString());
		}
	}
}
