using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoursesScraper
{
	class CodeDescriptions
	{
		public string Code { get; private set; } = null;
		public string Description { get; private set; } = null;

		public CodeDescriptions(string[] attrs) {
			Code = attrs[0];
			Description = attrs[1];
		}
	}
}
