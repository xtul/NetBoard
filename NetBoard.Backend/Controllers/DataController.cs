using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NetBoard.Controllers.Generic;
using NetBoard.Model.Data;
using NetBoard.Model.Data.Objects;
using NetBoard.Model.ExtensionMethods;
using Newtonsoft.Json;

namespace NetBoard.Controllers {
	[Route("api/frontpage")]
	[ApiController]
	public class DataController : ControllerBase {
		#region Constructor
		private readonly ApplicationDbContext _context;

		public DataController(ApplicationDbContext context) {
			_context = context;
		}
		#endregion

		#region GET
		[HttpGet("about")]
		public ActionResult<Dictionary<string, string>> GetAboutData() {
			var dbResult = _context.FrontpageData.Find(1);
			var responseData = new Dictionary<string, string> {
				{ "message", dbResult.About }
			};

			return responseData;
		}

		[HttpGet("news")]
		public ActionResult<Dictionary<string, string>> GetNewsData() {
			var dbResult = _context.FrontpageData.Find(1);
			var responseData = new Dictionary<string, string> {
				{ "message", dbResult.News }
			};

			return responseData;
		}

		[HttpGet("boards")]
		public ActionResult<Dictionary<string, string>> GetBoardsData() {
			var dbResult = _context.FrontpageData.Find(1);
			var responseData = JsonConvert.DeserializeObject<Dictionary<string, string>>(dbResult.BoardsJson);

			return responseData;
		}
		#endregion

		#region POST
		[Authorize]
		[ValidateAntiForgeryToken]
		[HttpPost("about")]
		public async Task<ActionResult> UpdateAbout(Dictionary<string, string> data) {
			var oldData = await _context.FrontpageData.FindAsync(1);
			oldData.About = data["about"];
			await _context.SaveChangesAsync();
			return Ok();
		}

		[Authorize]
		[ValidateAntiForgeryToken]
		[HttpPost("news")]
		public async Task<ActionResult> UpdateNews(Dictionary<string, string> data) {
			var oldData = await _context.FrontpageData.FindAsync(1);
			oldData.News = data["news"];
			await _context.SaveChangesAsync();
			return Ok();
		}
		#endregion
	}
}
