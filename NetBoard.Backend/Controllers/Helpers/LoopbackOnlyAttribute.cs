using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NetBoard.Controllers.Helpers {
	/// <summary>
	/// Makes sure only loopback IP can run this action.
	/// </summary>
	public class LoopbackOnlyAttribute : Attribute, IResourceFilter {
		public LoopbackOnlyAttribute() { }

		public void OnResourceExecuted(ResourceExecutedContext context) { }

		public void OnResourceExecuting(ResourceExecutingContext context) {
			if (!System.Net.IPAddress.IsLoopback(context.HttpContext.Connection.RemoteIpAddress)) {
				context.Result = new NotFoundResult();
				return;
			}
		}
	}
}
