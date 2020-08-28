using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NetBoard.Model.Data {
	public class Sage {
		public int Id { get; set; }
		public string Board { get; set; }
		public int TopicId { get; set; }
		public DateTime SagedOn { get; set; }
	}
}
