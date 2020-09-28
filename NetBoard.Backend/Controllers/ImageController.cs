using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using AutoMapper.Configuration;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using NetBoard.Controllers.Helpers;
using NetBoard.Model.Data;

namespace NetBoard.Controllers {
	[Route("api/image")]
	[ApiController]
	public class ImageController : ControllerBase {
		private readonly ApplicationDbContext _context;
		private readonly string _tempImageDir;

		public TimeSpan ImageExpiration = TimeSpan.FromMinutes(30);

		public ImageController(ApplicationDbContext context) {
			_context = context;
			_tempImageDir = Path.Combine(Path.GetDirectoryName(typeof(Startup).Assembly.Location), "tempImages");
			if (!Directory.Exists(_tempImageDir)) {
				Directory.CreateDirectory(_tempImageDir);
			}
		}

		[HttpPost("upload")]
		[DisableRequestSizeLimit]
		public IActionResult UploadImage() {
			try {
				if (Request.Form.Files.Count < 1) {
					return BadRequest("Expected an image, but it wasn't provided.");
				}
				var file = Request.Form.Files[0];
				var pathToSave = Path.Combine(Directory.GetCurrentDirectory(), _tempImageDir);

				if (file.Length > 3000000) { // over 3MB
					return BadRequest("Image size exceeds 3MB.");
				}

				if (file.Length > 0) {
					var fileName = $"{DateTime.UtcNow.ToUnixtime()}.{ContentDispositionHeaderValue.Parse(file.ContentDisposition).FileName.Trim('"')}";

					// if this file somehow exists add a random number
					if (System.IO.File.Exists(fileName)) {
						return BadRequest("You can't post the same image twice!");
					}

					var fullPath = Path.Combine(pathToSave, fileName);

					var imageQueue = new ImageQueue {
						Filename = fileName,
						Token = Guid.NewGuid().ToString(),
						ExpiresOn = DateTime.UtcNow.AddMinutes(ImageExpiration.TotalMinutes)
					};

					_context.ImageQueue.Add(imageQueue);

					using (var stream = new FileStream(fullPath, FileMode.Create)) {
						file.CopyTo(stream);
					}

					var response = new Dictionary<string, string> {
						{ "token", imageQueue.Token },
						{ "minutesUntilExpires", ImageExpiration.TotalMinutes.ToString() }
					};

					_context.SaveChanges();

					return Ok(response);
				} else {
					return BadRequest("Image size is 0.");
				}
			} catch (Exception ex) {
				return StatusCode(500, $"Internal server error: {ex}");
			}
		}
	}
}
