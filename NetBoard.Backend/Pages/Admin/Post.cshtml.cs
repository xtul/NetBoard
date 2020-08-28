using NetBoard.Model.Data;
using NetBoard.Model.ExtensionMethods;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace NetBoard.Pages.Admin {
	[Authorize]
	public class PostModel : PageModel {
		#region Constructor

		private readonly ApplicationDbContext _context;

		public PostModel(ApplicationDbContext context) {
			_context = context;
		}

		#endregion Constructor

		[BindProperty]
		public InputModel Input { get; set; }

		public int PostId { get; set; }
		public string Board { get; set; }
		public List<string> BoardOptions {
			get { return BoardFinder.GetBoardsAsStrings(); }
		}
		public PostStructure FoundPostData { get; set; }

		[TempData]
		public string ErrorMessage { get; set; }

		public class InputModel {
			[Required]
			[Display(Name = "Post ID")]
			[BindProperty]
			public int PostId { get; set; }

			[Required]
			[Display(Name = "Board")]
			[BindProperty]
			public string Board { get; set; }
		}

		public async Task<IActionResult> OnGetAsync([FromQuery] string board = null, [FromQuery] int postId = 0, [FromQuery] string remoteErr = null) {
			if (!string.IsNullOrEmpty(ErrorMessage)) {
				ModelState.AddModelError("", ErrorMessage);
			}
			if (remoteErr != null) {
				ModelState.AddModelError("", remoteErr);
			}

			if (board != null) {
				Board = board;
			}
			if (postId != 0) {
				PostId = postId;
			}

			if (board != null && postId != 0) {
				FoundPostData = await _context.FindAsync(BoardFinder.GetBoard(Board), postId) as PostStructure;
			}
			return Page();
		}
	}
}
