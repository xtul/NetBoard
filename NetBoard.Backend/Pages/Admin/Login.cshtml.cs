using NetBoard.Model.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace NetBoard.Pages.Admin {
	[AllowAnonymous]
	public class LoginModel : PageModel {
		#region Constructor

		private readonly UserManager<ApplicationUser> _userManager;
		private readonly SignInManager<ApplicationUser> _signInManager;

		public LoginModel(SignInManager<ApplicationUser> signInManager,
			UserManager<ApplicationUser> userManager) {
			_userManager = userManager;
			_signInManager = signInManager;
		}

		#endregion Constructor

		[BindProperty]
		public InputModel Input { get; set; }

		public string ReturnUrl { get; set; }

		[TempData]
		public string ErrorMessage { get; set; }

		public class InputModel {
			[Required]
			[Display(Name = "Username")]
			[StringLength(32)]
			[BindProperty]
			public string Name { get; set; }

			[Required]
			[DataType(DataType.Password)]
			[Display(Name = "Password")]
			[BindProperty]
			public string Password { get; set; }
		}

		public async Task<IActionResult> OnGetAsync(string returnUrl = null) {
			if (!string.IsNullOrEmpty(ErrorMessage)) {
				ModelState.AddModelError(string.Empty, ErrorMessage);
			}
			if (HttpContext.User.Identity.IsAuthenticated) {
				return Redirect("~/admin/");
			}

			returnUrl ??= Url.Content("~/");

			// Clear the existing external cookie to ensure a clean login process
			await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

			ReturnUrl = returnUrl.ToLower();
			return Page();
		}

		public async Task<IActionResult> OnPostAsync(string returnUrl = null) {
			returnUrl ??= Url.Content("~/admin/");
			if (ModelState.IsValid) {
				var user = await _userManager.FindByNameAsync(Input.Name);
				if (user == null) {
					ModelState.AddModelError("", "Invalid credientals.");
					return Page();
				}

				var passwordIsCorrect = await _userManager.CheckPasswordAsync(user, Input.Password);
				if (!passwordIsCorrect) {
					ModelState.AddModelError("", "Invalid credientals.");
					return Page();
				}

				var isAdmin = await _userManager.IsInRoleAsync(user, "admin");

				if (!isAdmin) {
					ModelState.AddModelError("", "Invalid credientals.");
					return Page();
				}

				await _signInManager.SignInAsync(user, null);
				return LocalRedirect(returnUrl);
			}

			// If we got this far, something failed, redisplay form
			ModelState.AddModelError(string.Empty, "Invalid credientals.");
			return Page();
		}

		public class Token {
			public string Value { get; set; }
		}
	}
}
