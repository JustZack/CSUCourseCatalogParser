using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoursesScraper
{
	class AdditionalInfo : GenericCourse
	{
		public AdditionalInfo(string note) : base(note, "", "", "", false, false, "") { }
		public override string ToString()
		{
			return "";
		}

		public string toCompletionCSV(string program, string code, string key, bool isreq = false) {
			StringBuilder sb = new StringBuilder();
			sb.AppendFormat(csvFormat, key);           //Key
			sb.AppendFormat(csvFormat, code);         //ProgramCode
			sb.AppendFormat(csvFormat, program);     //ProgramDescription
			sb.AppendFormat(csvFormat, "");         //Subject
			sb.AppendFormat(csvFormat, "");        //CourseNum
			sb.AppendFormat(csvFormat, "");       //stdClass
			sb.AppendFormat(csvFormat, "");      //Semester
			sb.AppendFormat(csvFormat, Note);   //Note
			sb.AppendFormat(csvFormat, "");    //AND
			sb.AppendFormat(csvFormat, "");   //OR
			sb.AppendFormat(csvFormat, "");  //Credit
			sb.AppendFormat(csvFormat, GetCreditNum());//Creditnum
			sb.AppendFormat(csvFormat, "");        //Critical
			sb.AppendFormat(csvFormat, "");       //AUCC_4A
			sb.AppendFormat(csvFormat, "");      //AUCC_4B
			sb.AppendFormat(csvFormat, "");     //AUCC_4C
			sb.AppendFormat(csvFormat, "");    //Recommended
			sb.AppendFormat(csvFormat, "");   //SelectFrom
			sb.AppendFormat(csvFormat, "");  //AKA
			sb.AppendFormat(csvFormat, ""); //IsCourse
			sb.AppendFormat(csvFormat, "");//Memo
			if (isreq)                    //Footnotes
				sb.AppendFormat(csvFormat, Footnotes);         
			sb.AppendLine();
			return sb.ToString();
		}

		public override string toCSV(Year yr, Semester sm, string program, string code, string key, bool isreq = false)
		{
			throw new NotImplementedException();
		}
	}
}
