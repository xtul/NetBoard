using Microsoft.AspNetCore.Identity;
using System;

namespace NetBoard.Model.Data {
	public class ImageQueue {
		public int Id { get; set; }
		public string Token { get; set; }
		public DateTime ExpiresOn { get; set; }
		public string Filename { get; set; }
		public int? AssignedPost { get; set; }
	}
}