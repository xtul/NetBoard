using System;

namespace NetBoard.Model.Data {
	public class MarkedForDeletion {
		public int Id { get; set; }
		public DateTime UtcDeletedOn { get; set; }
		public int PostId { get; set; }
		public string Board { get; set; }
	}
}