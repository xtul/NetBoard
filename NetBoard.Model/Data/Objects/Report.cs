using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace NetBoard.Model.Data {
	public class Report {
		public int Id { get; set; }
		public DateTime Date { get; set; }
		public string ReportingIP { get; set; }
		[ForeignKey("ReportedPost")]
		public string PostBoard { get; set; }
		public int PostId { get; set; }
		[StringLength(128)]
		public string Reason { get; set; }
	}
}
