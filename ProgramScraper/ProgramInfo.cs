using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace CoursesScraper
{
	class ProgramInfo : GenericCourse {

		public ProgramInfo(string desc, bool crit, bool req) : base(desc, "", "", "", crit, req, "") { }

		public override string ToString()
		{
			return Note;
		}

		public override string toCSV(Year yr, Semester sm, string program, string code, string key, bool isreq) {
			StringBuilder sb = new StringBuilder();
			sb.AppendFormat(csvFormat, key);                 //Key
			sb.AppendFormat(csvFormat, code);               //ProgramCode
			sb.AppendFormat(csvFormat, program);           //ProgramDescription
			sb.AppendFormat(csvFormat, "");               //Subject
			sb.AppendFormat(csvFormat, "");              //CourseNum
			sb.AppendFormat(csvFormat, yr != null ? yr.Level : "");       //stdClass
			sb.AppendFormat(csvFormat, sm != null ? sm.Description : "");//Semester
			sb.AppendFormat(csvFormat, Note);		  //Note
			sb.AppendFormat(csvFormat, "");          //AND
			sb.AppendFormat(csvFormat, "");         //OR
			sb.AppendFormat(csvFormat, "");		   //Credit
			sb.AppendFormat(csvFormat, "0");	  //Creditnum
			sb.AppendFormat(csvFormat, Critical ? "TRUE" : "");//Critical
			sb.AppendFormat(csvFormat, "");					  //AUCC_4A
			sb.AppendFormat(csvFormat, "");					 //AUCC_4B
			sb.AppendFormat(csvFormat, "");					//AUCC_4C
			sb.AppendFormat(csvFormat, Recommended ? "TRUE" : "");//Recommended
			sb.AppendFormat(csvFormat, SelectFrom ? "TRUE" : "");//SelectFrom
			sb.AppendFormat(csvFormat, "");						//AKA
			sb.AppendFormat(csvFormat, "");					   //IsCourse
			sb.AppendFormat(csvFormat, "");                   //Memo
			if (isreq)						                 //Footnotes
				sb.AppendFormat(csvFormat, Footnotes);          
			sb.AppendLine();
			return sb.ToString();
		}
	}
}
