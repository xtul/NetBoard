using NetBoard.Model.Data;
using NetBoard.Model.ExtensionMethods;
using Flurl.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Net.Http;
using Microsoft.AspNetCore.Authorization;

namespace NetBoard.Pages.Admin {
	[Authorize]
	public class PostCreateModel : PageModel {
		#region Constructor

		private readonly ApplicationDbContext _context;

		public PostCreateModel(ApplicationDbContext context) {
			_context = context;
		}

		#endregion Constructor

		[BindProperty]
		public InputModel Input { get; set; }

		public List<string> BoardOptions {
			get { return BoardFinder.GetBoardsAsStrings(); }
		}

		[TempData]
		public string ErrorMessage { get; set; }

		public class InputModel {
			[Required]
			[Display(Name = "Thread ID")]
			[BindProperty]
			public int ThreadId { get; set; }

			[Required]
			[Display(Name = "Board")]
			[BindProperty]
			public string Board { get; set; }

			[Required]
			[Display(Name = "Sticky?")]
			[BindProperty]
			public bool Sticky { get; set; }

			[Required]
			[Display(Name = "Archived?")]
			[BindProperty]
			public bool Archived { get; set; }

			[Display(Name = "Options")]
			[MaxLength(32)]
			[BindProperty]
			public string Options { get; set; }

			[Required]
			[Display(Name = "Post content")]
			[MaxLength(4000)]
			[BindProperty]
			public string Content { get; set; }

			[Display(Name = "Subject")]
			[MaxLength(128)]
			[BindProperty]
			public string Subject { get; set; }
		}

		public IActionResult OnGet() {
			if (!string.IsNullOrEmpty(ErrorMessage)) {
				ModelState.AddModelError("", ErrorMessage);
			}

			return Page();
		}
	}
}
