using System;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.IO;
using System.Text.RegularExpressions;
using SixLabors.ImageSharp.Advanced;

namespace NetBoard.Controllers.Helpers {
	public static class ImageManipulation {
		public enum ThumbType {
			thread,
			response
		}

		/// <summary>
		/// Generates a thumbnail from <paramref name="imageUri"/> and saves it next to it with a 's' ending.
		/// </summary>
		/// <param name="type">Defines max thumb dimensions. Thread thumbs are twice as large as response.</param>
		/// <returns>Relative path to thumbnail, eg. /img/g/1/TESTs.jpg</returns>
		public static string GenerateThumbnail(string imageUri, ThumbType type) {
			try {
				using (var image = Image.Load(imageUri)) {
					int maxDim;
					int newWidth;
					int newHeight;

					if (type == ThumbType.thread) {
						maxDim = 250;
					} else {
						maxDim = 125;
					};

					if (image.Width > maxDim || image.Height > maxDim) {
						double ratioX = (double)maxDim / image.Width;
						double ratioY = (double)maxDim / image.Height;
						double ratio = Math.Min(ratioX, ratioY);

						newWidth = (int)(image.Width * ratio);
						newHeight = (int)(image.Height * ratio);
					} else {
						newWidth = image.Width;
						newHeight = image.Height;
					}

					image.Mutate(x => x
						 .Resize(newWidth, newHeight));

					var filenameNoExt = Path.GetFileNameWithoutExtension(imageUri);
					var fileExt = Path.GetExtension(imageUri);
					var thumbName = filenameNoExt + "s" + fileExt;

					image.Save(Path.Combine(Path.GetDirectoryName(imageUri), thumbName));
				}
			} catch { return "Error when storing image - make sure "; }

			return imageUri.ToRelativePath("wwwroot");
		}

		public static void DeleteImage(string board, int postId) {
			var wwwroot = Path.Combine(Path.GetDirectoryName(typeof(Startup).Assembly.Location), "wwwroot");
			Directory.Delete(Path.Combine(wwwroot, "img", board, postId.ToString()), true);
		}

		public static void DeleteTempImage(string filename) {
			var tempDirPath = Path.Combine(Path.GetDirectoryName(typeof(Startup).Assembly.Location), "tempImages");
			File.Delete(Path.Combine(tempDirPath, filename));
		}

		public static void DeleteTempImages(string[] filenameArray) {
			for (int i = 0; i < filenameArray.Length; i++) {
				var tempDirPath = Path.Combine(Path.GetDirectoryName(typeof(Startup).Assembly.Location), "tempImages");
				File.Delete(Path.Combine(tempDirPath, filenameArray[i]));
			}
		}

		/// <summary>
		/// Converts an absolute (full) path to path relative to <paramref name="pivotFolder"/>.
		/// For example, if <paramref name="pivotFolder"/> is "wwwroot", result will be /img/g/1/testimage.jpg.
		/// </summary>
		/// <returns>An absolute path. Empty string in case of error.</returns>
		public static string ToRelativePath(this string absolutePath, string pivotFolder) {
			Regex regex = new Regex(@$".*{pivotFolder}");
			Match match = regex.Match(absolutePath);

			if (match.Success) {
				return absolutePath.Replace(match.Value, "");
			} else {
				return string.Empty;	
			}
		}
	}
}
