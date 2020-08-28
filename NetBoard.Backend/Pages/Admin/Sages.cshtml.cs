using NetBoard.Model.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace NetBoard.Pages.Admin {
	[Authorize]
	public class SageModel : PageModel {

		public readonly ApplicationDbContext _context;

		public SageModel(ApplicationDbContext context) {
			_context = context;
		}

		public int Id { get; set; }
		public string Board { get; set; }
		public int TopicId { get; set; }

		public async Task OnGetAsync() {
			ViewData["SageList"] = await _context.Sages.ToListAsync();
		}
	}
}
