using NetBoard.Model.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using NetBoard.Model.Data.Objects;
using NetBoard.Model.ExtensionMethods;
using System.Collections.Generic;
using Newtonsoft.Json;
using NetBoard.ExtensionMethods;
using System.Reflection;
using NetBoard.Controllers.Helpers;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Flurl.Http;

namespace NetBoard {
	public class HangfireJobs {
		#region Constructor
		public IConfiguration _configuration;
		public UserManager<ApplicationUser> _userManager;
		public RoleManager<IdentityRole<int>> _roleManager;
		public readonly ApplicationDbContext _context;
		public readonly ILogger<HangfireJobs> _logger;

		public HangfireJobs(
			UserManager<ApplicationUser> userManager,
			ApplicationDbContext context,
			IConfiguration configuration,
			ILogger<HangfireJobs> logger,
			RoleManager<IdentityRole<int>> roleManager) {
			_userManager = userManager;
			_context = context;
			_configuration = configuration;
			_logger = logger;
			_roleManager = roleManager;
		}
		#endregion Constructor

		public void EnsureDatabase() {
			_context.Database.Migrate();
		}


		public async Task CleanupImageQueue() {
			// clean orphaned and assigned images
			var obsoleteEntries = await _context.ImageQueue
										.Where(x => 
											(	x.ExpiresOn < DateTime.UtcNow 
												&& x.AssignedPost.HasValue == false
											)
											|| x.AssignedPost.HasValue == true)
										.ToListAsync();
			var imageFiles = obsoleteEntries.Select(x => x.Filename).ToArray();
			ImageManipulation.DeleteTempImages(imageFiles);
			_context.ImageQueue.RemoveRange(obsoleteEntries);
			await _context.SaveChangesAsync();
		}

		public async Task DeleteArchivedThreadsAsync() {
			foreach (var board in BoardFinder.GetBoardsAsStrings()) {
				try {
					await $"http://localhost:5934/api/{board}/del"
						.PostAsync(null);
				} catch { }
			}
		}

		public async Task SeedDB() {
			_logger.LogDebug("Ensuring roles...");
			await EnsureRolesAsync();
			_logger.LogDebug("Ensuring admin user...");
			await EnsureAdminAsync();
			_logger.LogDebug("Ensuring frontpage data...");
			await EnsureFrontpageAsync();

			_logger.LogDebug("Done ensuring DB, saving changes.");
			await _context.SaveChangesAsync();
		}

		/// <summary>
		/// Clears bans that are meant to expire.
		/// </summary>
		/// <returns></returns>
		public async Task CheckBans() {
			var expiredBans = await _context.Bans.AsNoTracking().Where(x => x.ExpiresOn <= DateTime.UtcNow).ToListAsync();

			foreach (var ban in expiredBans) {
				// remove IP from appsettings
				var shadowBanList = _configuration.GetSection("Bans:ShadowBanList").GetChildren().Select(c => c.Value).ToList();
				shadowBanList.Remove(ban.Ip);
				AppsettingsManipulation.AddOrUpdateAppSetting("Bans:ShadowBanList", shadowBanList);
			}

			_context.Bans.RemoveRange(expiredBans);

			await _context.SaveChangesAsync();
		}

		private async Task EnsureRolesAsync() {
			if (await _roleManager.FindByNameAsync("admin") == null) {
				var role = new IdentityRole<int> {
					Name = "admin"
				};
				await _roleManager.CreateAsync(role);
			}
			if (await _roleManager.FindByNameAsync("moderator") == null) {
				var role = new IdentityRole<int> {
					Name = "moderator"
				};
				await _roleManager.CreateAsync(role);
			}
			if (await _roleManager.FindByNameAsync("janitor") == null) {
				var role = new IdentityRole<int> {
					Name = "janitor"
				};
				await _roleManager.CreateAsync(role);
			}
		}

		private async Task EnsureAdminAsync() {
			var login = _configuration["AdminLogin"];
			var pw = _configuration["AdminPassword"];

			var admin = await _userManager.FindByNameAsync(login);

			if (admin == null) {
				_logger.LogInformation($"Attempting to create {login} account...");
				var newAdmin = new ApplicationUser {
					UserName = login,
					Email = $"{login}@NetBoard.nl",
					EmailConfirmed = true
				};
				await _userManager.CreateAsync(newAdmin, pw);
				await _userManager.AddToRoleAsync(await _userManager.FindByNameAsync(login), login);
			}

			var passwordIsCurrent = await _userManager.CheckPasswordAsync(admin, pw);

			if (admin != null && passwordIsCurrent == false) {
				await _userManager.DeleteAsync(admin);
				await EnsureAdminAsync();
			}
		}

		private async Task EnsureFrontpageAsync() {
			var frontpageData = await _context.FrontpageData.FindAsync(1);

			var boards = BoardControllerFinder.GetBoardControllers();
			var tempDict = new Dictionary<string, string>();

			foreach (var board in boards) {
				Type boardType = Type.GetType(board.FullName, true, true);
				MethodInfo method = boardType.GetMethod("GetBoardName", BindingFlags.Public | BindingFlags.Instance);
				var boardName = method.Invoke(Activator.CreateInstance(boardType, null, null, null), null) as string;
				var boardShortName = board.Name.Replace("Controller", "").ToLower();
				tempDict.Add(boardShortName, boardName);
			}

			var boardsJson = JsonConvert.SerializeObject(tempDict);

			if (frontpageData == null) {
				_context.FrontpageData.Add(new FrontpageData {
					About = "The About section is yet to be filled. Please login as an administrator to do so.",
					BoardsJson = boardsJson,
					News = "No news yet. Please log in to update."
				});
			} else {
				frontpageData.BoardsJson = boardsJson;
			}
		}
	}
}