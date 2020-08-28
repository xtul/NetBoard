using NetBoard.Model.Data;
using NetBoard.Model.ExtensionMethods;
using Flurl.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using System.Linq;

namespace NetBoard.Controllers {
	[Authorize]
	public class AdminController : Controller {
		#region Constructor

		private readonly SignInManager<ApplicationUser> _signInManager;
		private readonly ApplicationDbContext _context;
		public AdminController(SignInManager<ApplicationUser> signInManager, ApplicationDbContext context) {
			_signInManager = signInManager;
			_context = context;
		}

		#endregion Constructor

		#region GET

		public IActionResult Index() {
			return LocalRedirect("~/admin/reports");
		}

		public IActionResult Reports() {
			return View();
		}
		
		public IActionResult Sages() {
			return View();
		}
		
		public IActionResult Post() {
			return View();
		}
		
		[AllowAnonymous]
		public IActionResult Login() {
			return View();
		}
		#endregion GET

		#region POST
		public async Task<IActionResult> ArchivePostAsync([FromQuery] string board, [FromRoute] int id, [FromQuery] string unarchive = "false") {
			var post = (PostStructure)await _context.FindAsync(BoardFinder.GetBoard(board), id);
			if (post == null) return NotFound();
			if (unarchive == "true") {
				post.Archived = false;
			} else {
				post.Archived = true;
			}
			await _context.SaveChangesAsync();
			return Redirect(Request.Headers["Referer"].ToString());
		}

		public async Task<IActionResult> DeletePostAsync([FromQuery] string board, [FromRoute] int id) {
			var post = (PostStructure)await _context.FindAsync(BoardFinder.GetBoard(board), id);
			if (post == null) return NotFound();

			// I tried to avoid making server-only API endpoints
			// but since I need to access topic responses I can't 
			// make queries as freely as I could when working with 
			// a strongly typed board definitions...
			var result = await $"http://localhost:5934/{board}/post/admin/{id}"
				.AllowAnyHttpStatus()
				.DeleteAsync();

			if (result.IsSuccessStatusCode) {
				return Redirect(Request.Headers["Referer"].ToString() + $"&remoteErr=\"Couldn't delete post: {result.StatusCode}: {result.ReasonPhrase}\"");
			} else {
				return Redirect("~/admin/post");
			}
		}

		public async Task<IActionResult> DeleteReportAsync([FromRoute] int id) {
			var report = await _context.Reports.FindAsync(id);
			if (report == null) return NotFound();
			_context.Remove(report);
			await _context.SaveChangesAsync();
			return Redirect(Request.Headers["Referer"].ToString());
		}

		public async Task<IActionResult> DeleteSageAsync([FromRoute] int id) {
			var sage = await _context.Sages.FindAsync(id);
			if (sage == null) return NotFound();
			_context.Remove(sage);
			await _context.SaveChangesAsync();
			return Redirect(Request.Headers["Referer"].ToString());
		}

		public async Task<IActionResult> DeletePictureAsync([FromQuery] string board, [FromRoute] int id) {
			var post = await _context.FindAsync(BoardFinder.GetBoard(board), id) as PostStructure;
			post.Image = null;
			await _context.SaveChangesAsync();
			return Redirect(Request.Headers["Referer"].ToString());
		}

		public async Task<IActionResult> StickyPostAsync([FromQuery] string board, [FromRoute] int id, [FromQuery] string unstick = "false") {
			var post = await _context.FindAsync(BoardFinder.GetBoard(board), id) as PostStructure;
			if (unstick == "true") {
				post.Sticky = false;
			} else {
				post.Sticky = true;
			}
			await _context.SaveChangesAsync();
			return Redirect(Request.Headers["Referer"].ToString());
		}

		public async Task<IActionResult> LogoutAsync() {
			await _signInManager.SignOutAsync();
			return LocalRedirect("~/admin/login");
		}

		#endregion POST
	}
}
