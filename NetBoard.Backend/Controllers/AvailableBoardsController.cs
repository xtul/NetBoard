using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NetBoard.Controllers.Generic;
using NetBoard.Model.Data;

namespace NetBoard.Controllers.Boards {
	[Route("api/g")]
	[ApiController]
	public class GController : BoardController<G> {
		public GController(ApplicationDbContext context, IConfiguration configuration, ILogger<G> logger) : base(context, configuration, logger) {
			BoardName = "Technology";			
		}
	}
	
	[Route("api/diy")]
	[ApiController]
	public class DiyController : BoardController<Diy> {
		public DiyController(ApplicationDbContext context, IConfiguration configuration, ILogger<Diy> logger) : base(context, configuration, logger) {
			BoardName = "Do it yourself";
		}
	}
	
	[Route("api/meta")]
	[ApiController]
	public class MetaController : BoardController<Meta> {
		public MetaController(ApplicationDbContext context, IConfiguration configuration, ILogger<Meta> logger) : base(context, configuration, logger) {
			BoardName = "NetBoard discussion";
		}
	}
}
