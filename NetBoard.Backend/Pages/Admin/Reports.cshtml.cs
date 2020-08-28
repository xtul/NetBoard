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
	public class ReportModel : PageModel {

		public readonly ApplicationDbContext _context;

		public ReportModel(ApplicationDbContext context) {
			_context = context;
		}

		public int Id { get; set; }
		public DateTime Date { get; set; }
		public string ReportingIP { get; set; }
		public string PostBoard { get; set; }
		public int PostId { get; set; }
		public string Reason { get; set; }

		public async Task OnGetAsync() {
			ViewData["ReportList"] = await _context.Reports.ToListAsync();
		}
	}
}
