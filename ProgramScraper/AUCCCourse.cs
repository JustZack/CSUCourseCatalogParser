using System;
using System.Collections.Generic;
using System.Text;

namespace CoursesScraper
{
	class AUCCCourse : GenericCourse
	{
		public AUCCCourse(string note, bool critical, bool recommended, string credits, string aucc) : 
			         base(note, credits, "", "", critical, recommended, aucc) { }

		
		public override string ToString() {
			return Note.Length >= 10 ? Note.Substring(0, 10) : Note + "...\t\t" + Critical + "\t" + Recommended + "\t" + string.Join(",", AUCC) + "\t" + Credits + "\n";
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
			sb.AppendFormat(csvFormat, Note);              //Note
			sb.AppendFormat(csvFormat, "");               //AND
			sb.AppendFormat(csvFormat, "");              //OR
			sb.AppendFormat(csvFormat, Credits);        //Credit
			sb.AppendFormat(csvFormat, GetCreditNum());//Creditnum
			sb.AppendFormat(csvFormat, Critical ? "TRUE" : "");//Critical
			sb.AppendFormat(csvFormat, HasAUCC("4A"));        //AUCC_4A
			sb.AppendFormat(csvFormat, HasAUCC("4B"));       //AUCC_4B
			sb.AppendFormat(csvFormat, HasAUCC("4C"));      //AUCC_4C
			sb.AppendFormat(csvFormat, Recommended ? "TRUE" : "");//Recommended
			sb.AppendFormat(csvFormat, SelectFrom ? "TRUE" : "");//SelectFrom
			sb.AppendFormat(csvFormat, "");                     //AKA
			sb.AppendFormat(csvFormat, "");                    //IsCourse
			sb.AppendFormat(csvFormat, GetAUCC());            //Memo
			if(isreq)                                        //Footnotes
				sb.AppendFormat(csvFormat, Footnotes);         

			sb.AppendLine();
			return sb.ToString();
		}

	}
}
