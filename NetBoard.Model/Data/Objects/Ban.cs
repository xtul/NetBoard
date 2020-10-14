using Org.BouncyCastle.Utilities.Net;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetBoard.Model.Data.Objects {
	public class Ban {
		public int Id { get; set; }
		public string Ip { get; set; }
		public string Reason { get; set; }
		public DateTime ExpiresOn { get; set; }
	}
}
