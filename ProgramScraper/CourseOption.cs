using System;
using System.Collections.Generic;
using System.Text;

namespace CoursesScraper
{
	class CourseOption : GenericCourse
	{
		public List<GenericCourse> Courses { get; } = new List<GenericCourse>();

		public CourseOption(string note, string credits) : 
			           base(note, credits, "", "", false, false, "") {

		}

		public override string toCSV(Year yr, Semester sm, string program, string code, string key, bool isreq) {
			StringBuilder sb = new StringBuilder();
			sb.AppendFormat(csvFormat, key);                 //Key
			sb.AppendFormat(csvFormat, code);               //ProgramCode
			sb.AppendFormat(csvFormat, program);           //ProgramDescription
			sb.AppendFormat(csvFormat, Department);       //Subject
			sb.AppendFormat(csvFormat, Number);          //CourseNum
			sb.AppendFormat(csvFormat, yr != null ? yr.Level : "");       //stdClass
			sb.AppendFormat(csvFormat, sm != null ? sm.Description : "");//Semester
			sb.AppendFormat(csvFormat, Note);         //Note
			sb.AppendFormat(csvFormat, "");          //AND
			sb.AppendFormat(csvFormat, "");         //OR
			sb.AppendFormat(csvFormat, Credits);   //Credit
			sb.AppendFormat(csvFormat, GetCreditNum());    //Creditnum
			sb.AppendFormat(csvFormat, "");			      //Critical
			sb.AppendFormat(csvFormat, "");				 //AUCC_4A
			sb.AppendFormat(csvFormat, "");				//AUCC_4B
			sb.AppendFormat(csvFormat, "");			   //AUCC_4C
			sb.AppendFormat(csvFormat, "");			  //Recommended
			sb.AppendFormat(csvFormat, "");			 //SelectFrom
			sb.AppendFormat(csvFormat, "");         //AKA
			sb.AppendFormat(csvFormat, "");        //IsCourse
			sb.AppendFormat(csvFormat, GetAUCC());//Memo
			if (isreq)                           //Footnotes
				sb.AppendFormat(csvFormat, Footnotes);
			sb.AppendLine();

			foreach (GenericCourse crs in Courses)
				sb.Append(crs.toCSV(yr, sm, program, code, key));

			return sb.ToString();
		}

		public override string ToString() {
			StringBuilder sb = new StringBuilder();
			sb.AppendFormat(csvFormat, Note + "...\n");
			foreach (GenericCourse crs in Courses)
				sb.AppendFormat(csvFormat, "    " + crs);

			return sb.ToString();
		}

		public void Add(string dept, string number, string desc, bool critical, bool recommended, string aucc) {
			Add(new Course(dept, number, desc, critical, recommended, aucc, this.Credits));
		}

		public void Add(GenericCourse course) {
			course.SelectFrom = true;
			course.Note += "<" + Note + ">";
			Courses.Add(course);
		}
	}
}
