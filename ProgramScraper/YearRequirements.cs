using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoursesScraper
{
	class YearRequirements : Semester
	{
		public YearRequirements(string level) : base(level) {

		}

		public string toCSV(string program, string code, string key)
		{
			StringBuilder sb = new StringBuilder();
			foreach (GenericCourse crs in Courses)
			{
				sb.Append(crs.toCSV(new Year(Description), new Semester(""), program, code, key, true));
			}
			return sb.ToString();
		}
	}
}
