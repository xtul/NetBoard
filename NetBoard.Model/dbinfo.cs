using System;
using System.Collections.Generic;
using System.Text;

namespace NetBoard.Model {
	public class Dbinfo {
		public string DatabaseType { get; set; }
		public Dictionary<string, string> ConnectionStrings { get; set; }
	}
}
