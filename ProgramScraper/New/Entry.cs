using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace ProgramScraper.New
{
	class Entry
	{
		private string note = string.Empty;
		public string Note {
			get {
				return this.note;
			}
			set {
				if (this.Note == string.Empty)
					this.note = value.ToString();
				else
					this.note = this.Note.Insert(0, value.ToString());
			}
		}
		public string Description { get; set; } = string.Empty;
		public bool SelectFrom { get; set; } = false;
		public string Footnums { get; set; } = string.Empty;
		public string Footnotes { get; set; } = string.Empty;
		public string Name { get; set; } = string.Empty;
		public bool isYear { get; set; } = false;
		public bool isTerm { get; set; } = false;
		public bool isSum { get; set; } = false;
		public bool isTotal { get; set; } = false;
		public bool isCourse { get; set; } = false;
		public bool isSelect { get; set; } = false;
		public bool isGroup { get; set; } = false;
		public bool isAUCC { get; set; } = false;
		public bool isElective { get; set; } = false;
		public bool isInfo { get; set; } = false;
		public bool isAreaHeader { get; set; } = false;
		public bool isOrClass { get; set; } = false;


		private string credits = string.Empty;
		public string Credits {
			get {
				return credits;
			}
			set {
				this.credits = value;
				GetCreditNum();
			}
		}
		public int  CreditNum { get; private set; }

		public HtmlNode row;
		public static string csvFormat = "\"{0}\",";
		public Entry(HtmlNode row)
		{
			this.row = row;
		}
		public void GetFootnums()
		{	
			if (row.InnerHtml.Contains("<sup>"))
			{
				string html = row.InnerHtml;
				int startsAt = 5 + html.IndexOf("<sup>");
				int endsAt = html.IndexOf("</sup>");
				this.Footnums = html.Substring(startsAt, endsAt - startsAt);
			}
		}
		public bool HasCredits() {
			return !this.Credits.Equals(string.Empty);
		}	 
		public void GetCreditNum()
		{
			this.CreditNum = 0; int result = -1;
			if (this.Credits.ToLower().Contains("var"))
			{
				this.CreditNum = -1;
			}
			else if (Credits.Contains("-") && Credits.Length >= 3)
			{
				string[] credits = Credits.Split('-');
				if (int.TryParse(credits[1], out result))
					this.CreditNum = result;
				//Find the biggest number in the credits string. Probably the same is just taking the last number found.
				foreach (string num in credits)
					if (int.Parse(num) > this.CreditNum)
						this.CreditNum = int.Parse(num);
			} else if (int.TryParse(this.Credits, out result)) {
				this.CreditNum = result;
			}
		}
		public static void RemoveEmptyTextNodes(HtmlNode n)
		{
			for (int i = 0; i < n.ChildNodes.Count; i++)
			{
				HtmlNode c = n.ChildNodes[i];
				if (c.Name.Equals("#text") && c.InnerText.Trim().Equals(string.Empty))
				{
					n.ChildNodes.Remove(c);
					i--;
				}
			}
		}
		public string[] RemoveWhiteSpaceEntires(string[] array) {
			List<string> result = new List<string>();
			foreach (string s in array)
				if (s != null && s.Trim().Length > 0)
					result.Add(s.Trim());
			return result.ToArray();
					
		}
		public override string ToString()
		{
			StringBuilder toReturn = new StringBuilder();
			if (this.Note != string.Empty) toReturn.Append(this.Note);
			else toReturn.Append(this.Name + " " + this.Credits);
			return toReturn.ToString();
		}
		public static string ExtractCreditsFromString(string note) {
			int start = -1, end = -1;
			for (int i = 0; i < note.Length;i++) {
				if (char.IsDigit(note[i])) {
					if (start == -1) start = i;
					end = i;
				}
			}
			if (start != -1 && end != -1)
				return note.Substring(start, (end - start) + 1);
			else
				return string.Empty;
		}
		public static string boolAsString(bool b) {
			return b ? "TRUE" : "";
		}
		public string ToCSV(string key, string code, string program, string year, string semester, bool isreq)
		{
			StringBuilder csv = new StringBuilder();
			csv.AppendFormat(csvFormat, key);                 //Key
			csv.AppendFormat(csvFormat, code);               //ProgramCode
			csv.AppendFormat(csvFormat, program);           //ProgramDescription
			csv.AppendFormat(csvFormat, "");       //Subject
			csv.AppendFormat(csvFormat, "");          //CourseNum
			csv.AppendFormat(csvFormat, year);       //stdClass
			csv.AppendFormat(csvFormat, semester);//Semester
			csv.AppendFormat(csvFormat, this.Note);              //Note
			csv.AppendFormat(csvFormat, "");              //AND
			csv.AppendFormat(csvFormat, "");              //OR
			csv.AppendFormat(csvFormat, this.Credits);        //Credit
			csv.AppendFormat(csvFormat, this.CreditNum);//Creditnum
			csv.AppendFormat(csvFormat, "");//Critical
			csv.AppendFormat(csvFormat, "");        //AUCC_4A
			csv.AppendFormat(csvFormat, "");       //AUCC_4B
			csv.AppendFormat(csvFormat, "");      //AUCC_4C
			csv.AppendFormat(csvFormat, "");//Recommended
			csv.AppendFormat(csvFormat, "");//SelectFrom
			csv.AppendFormat(csvFormat, "");                    //AKA
			csv.AppendFormat(csvFormat, "");                    //IsCourse
			csv.AppendFormat(csvFormat, "");            //Memo
			if (isreq)                                       //Footnotes
				csv.AppendFormat(csvFormat, this.Footnotes);
			csv.AppendLine();
			return csv.ToString();
		}
		public static string ToCSV(string key, string code, string program, string year, string semester, string note, bool isreq) {
			StringBuilder csv = new StringBuilder();
			csv.AppendFormat(csvFormat, key);      //Key
			csv.AppendFormat(csvFormat, code);    //ProgramCode
			csv.AppendFormat(csvFormat, program);//ProgramDescription
			csv.AppendFormat(csvFormat, "");    //Subject
			csv.AppendFormat(csvFormat, "");   //CourseNum
			csv.AppendFormat(csvFormat, year);//stdClass
			csv.AppendFormat(csvFormat, semester);//Semester
			csv.AppendFormat(csvFormat, note);   //Note
			csv.AppendFormat(csvFormat, "");    //AND
			csv.AppendFormat(csvFormat, "");   //OR
			csv.AppendFormat(csvFormat, "");  //Credit
			csv.AppendFormat(csvFormat, "0");//Creditnum
			csv.AppendFormat(csvFormat, "");//Critical
			csv.AppendFormat(csvFormat, "");	   //AUCC_4A
			csv.AppendFormat(csvFormat, "");	  //AUCC_4B
			csv.AppendFormat(csvFormat, "");	 //AUCC_4C
			csv.AppendFormat(csvFormat, "");    //Recommended
			csv.AppendFormat(csvFormat, "");   //SelectFrom
			csv.AppendFormat(csvFormat, "");  //AKA
			csv.AppendFormat(csvFormat, ""); //IsCourse
			csv.AppendFormat(csvFormat, "");//Memo
			if (isreq)                     //Footnotes
				csv.AppendFormat(csvFormat, "");
			csv.AppendLine();
			return csv.ToString();
		}
	}
}
