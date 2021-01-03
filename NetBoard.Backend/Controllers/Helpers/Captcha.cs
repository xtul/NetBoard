using Flurl.Http;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NetBoard.Controllers.Helpers {
	public static class Captcha {
		public static async Task<bool> IsCaptchaValid(string code, IConfiguration config) {
			var response = await "https://hcaptcha.com/siteverify"
				.PostUrlEncodedAsync(new Dictionary<string, string>() {
					{ "response", code },
					{ "secret", config.GetValue<string>("HCaptcha:Secret") },
					{ "sitekey", config.GetValue<string>("HCaptcha:SiteKey") }
				});

			var result = JsonConvert.DeserializeObject<Dictionary<string, object>>(await response.ResponseMessage.Content.ReadAsStringAsync());
			return (bool)result["success"];
		}
	}
}
