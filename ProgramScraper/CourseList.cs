using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoursesScraper
{
	class CourseList {
		public List<GenericCourse> Courses { get; } = new List<GenericCourse>();

		public CourseList() {

		}

		public string toCSV(string program, string code, string key) {
			StringBuilder sb = new StringBuilder();
			foreach (GenericCourse crs in Courses)
				sb.Append(crs.toCSV(null, null, program, code, key, true));
			return sb.ToString();
		}

		public void AddCourse(GenericCourse course) {
			this.Courses.Add(course);
		}
	}
}
