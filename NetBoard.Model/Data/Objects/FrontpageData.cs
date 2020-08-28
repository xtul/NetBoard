using NetBoard.Model.ExtensionMethods;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetBoard.Model.Data.Objects {
	public class FrontpageData {
		public int Id { get; set; }
		public string About { get; set; }
		public string News { get; set; }
		public string BoardsJson { get; set; }
	}
}
