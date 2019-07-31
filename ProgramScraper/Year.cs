using System;
using System.Collections.Generic;
using System.Text;

namespace CoursesScraper
{
	class Year
	{
		public Semester Fall { get; private set; } = null;
		public Semester Spring { get; private set; } = null;
		public Semester Summer { get; private set; } = null;
		public string Level { get; }

		public Year(string level) {
			Level = level;
		}

		public override string ToString() {
			StringBuilder sb = new StringBuilder();
			sb.Append(Level);
			if (Fall != null) sb.Append(Fall);
			if (Spring != null) sb.Append(Spring);
			if (Summer != null) sb.Append(Summer);
			return sb.ToString();
		}

		public string toCSV(string program, string code, string key) {
			StringBuilder sb = new StringBuilder();
			if(Fall != null)    sb.Append(Fall.toCSV(this, program, code, key));
			if (Spring != null) sb.Append(Spring.toCSV(this, program, code, key));
			if (Summer != null) sb.Append(Summer.toCSV(this, program, code, key));
			return sb.ToString();
		}

		public void AddSemester(Semester semester) {
			if (this.Fall == null) this.Fall = semester;
			else if (this.Spring == null) this.Spring = semester;
			else if (this.Summer == null) this.Summer = semester;
			else throw new Exception("Tried adding 4th semester to year");
		}
	}
}
