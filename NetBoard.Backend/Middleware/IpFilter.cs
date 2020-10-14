using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NetBoard.Model.Data;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Flurl;
using Flurl.Http;
using System.IO;
using System;
using System.Collections.Generic;

namespace NetBoard.Middleware {
	public class IPFilter {
		private readonly RequestDelegate _next;
		private readonly IConfiguration _config;
		public IPFilter(RequestDelegate next, IConfiguration config) {
			_next = next;
			_config = config;
		}

		public async Task InvokeAsync(HttpContext context, ApplicationDbContext db) {
			var connectingIp = context.Connection.RemoteIpAddress;
			var bannedArray = _config.GetSection("Bans:IpList").Get<string[]>();
			var areVPNsBanned = _config.GetValue<bool>("Bans:BanVPNs");

			if (bannedArray != null && bannedArray.Length > 0) {
				var isBanned = bannedArray
					.Where(a => IPAddress.Parse(a).Equals(connectingIp))
					.Any();
				if (isBanned) {

					var ban = await db.Bans.Where(x => connectingIp.Equals(IPAddress.Parse(x.Ip))).FirstOrDefaultAsync();

					if (ban != null) {
						await context.Response.WriteAsync($"You are banned. Reason: \"{ban.Reason}\". Expires on {ban.ExpiresOn}.");
					} else {
						await context.Response.WriteAsync("You are banned.");
					}

					context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
					return;
				} else if (areVPNsBanned) {
					var ipv4 = Path.Combine(Directory.GetCurrentDirectory(), _config.GetValue<string>("Bans:VPNIPv4ListUri"));
					var ipv6 = Path.Combine(Directory.GetCurrentDirectory(), _config.GetValue<string>("Bans:VPNIPv6ListUri"));

					if (File.Exists(ipv4)) {
						var ips = await File.ReadAllLinesAsync(ipv4);

						if (ips.Contains(connectingIp.ToString())) {
							context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
							return;
						}
					}

					if (File.Exists(ipv6)) {
						var ips = await File.ReadAllLinesAsync(ipv6);

						if (ips.Contains(connectingIp.ToString())) {
							context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
							return;
						}
					}

				}
			}
			await _next.Invoke(context);
		}
	}

	public static partial class MiddlewareExtensions {
		/// <summary>
		/// This middleware reads Bans section in appsettings.json and makes sure banned IPs get 404 response on all requests.
		/// </summary>
		/// <param name="builder"></param>
		/// <returns></returns>
		public static IApplicationBuilder UseIPFilter(this IApplicationBuilder builder) {
			return builder.UseMiddleware<IPFilter>();
		}
	}
}
