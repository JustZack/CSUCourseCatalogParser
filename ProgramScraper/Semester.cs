using System;
using System.Collections.Generic;
using System.Text;

namespace CoursesScraper
{
	class Semester
	{
		public string Description { get; }
		public string Credits { get; set; }
		public List<GenericCourse> Courses { get; } = new List<GenericCourse>();

		public Semester(string description) {
			Description = description;
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("\n" + Description);
			sb.Append("\nCLASS\t\t\tDESC\t\t\tCRIT\tREC\tAUCC\tCREDITS\n");
			foreach (GenericCourse crs in Courses)
				sb.Append(crs);
			sb.Append("\t\t\tTotal Credits\t\t\t\t" + Credits + "\n");
			return sb.ToString();
		}

		public string toCSV(Year yr, string program, string code, string key) {
			StringBuilder sb = new StringBuilder();
			foreach (GenericCourse crs in Courses)
				sb.Append(crs.toCSV(yr, this, program, code, key));
			return sb.ToString();
		}

		public void AddCourse(GenericCourse course) {
			this.Courses.Add(course);
		}
	}
}
