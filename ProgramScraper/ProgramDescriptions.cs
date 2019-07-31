using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoursesScraper
{
	class ProgramDescriptions : CodeDescriptions
	{
		public string Level { get; private set; }
		public string DegreeTypeCode { get; private set; }

		public ProgramDescriptions(string[] attrs) : base(attrs) {
			Level = attrs[2];
			DegreeTypeCode = attrs[3];
		}
	}
}
