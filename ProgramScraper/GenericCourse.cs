using System;
using System.Collections.Generic;
using System.Text;

namespace CoursesScraper
{
	abstract class GenericCourse
	{
		public string Note { get; set; }
		public string Credits { get; private set; }
		public string Department { get; private set; }
		public string Number { get; private set; }
		public bool Critical { get; private set; } = false;
		public bool Recommended { get; private set; } = false;
		public string[] AUCC { get; private set; }
		public bool SelectFrom { get; set; }
		public string AKA { get; set; } = "";
		public string AND { get; set; } = "";
		public string OR { get; set; } = "";
		public string Footnotes { get; set; } = "";

		public string csvFormat = "\"{0}\",";
		public GenericCourse(string note, string credits, string dept, string num, bool crit, bool rec, string aucc) {
			Note = note;
			Credits = credits;
			Department = dept;
			Number = num;
			Critical = crit;
			Recommended = rec;
			AUCC = aucc.Split(',');
		}

		public string HasAUCC(string category)
		{
			foreach (string aucc in AUCC)
				if (aucc.ToUpper().Equals(category.ToUpper()))
					return "TRUE";
			return "";
		}
		public string GetCreditNum() {
			string toReturn = Credits;
			if (Credits.Length == 0)
				toReturn = "0";
			else if (Credits.ToLower().Contains("var"))
				toReturn = "-1";
			//Is there a dash, and there are atleast 3 characters? [ *-* ]
			else if (Credits.Contains("-") && Credits.Length >= 3) {
				string[] credits = Credits.Split('-'); int biggest = int.Parse(credits[1]);
				//Find the biggest number in the credits string. Probably the same is just taking the last number found.
				foreach (string num in credits)
					if (int.Parse(num) > biggest)
						biggest = int.Parse(num);
				toReturn = biggest.ToString();
			}
			return toReturn;

		}
		public string GetAUCC() {
			return string.Join(",", AUCC);
		}
		public void GetFootnotes(List<string> Footnotes) {
			string footnote = string.Empty;
			if (this.Footnotes != string.Empty)
			{
				int j = 0; string[] footnums;
				//Split on spaces and commas (not all footnote indicators are separated by commas)
				footnums = this.Footnotes.Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);

				foreach (string numStr in footnums)
				{
					int num = int.Parse(numStr);
					if (num <= Footnotes.Count)
						footnote += String.Format("*{0} {1}{2}", numStr.Trim(), Footnotes[num - 1], (++j == footnums.Length ? "" : " | "));
				}
			}
			this.Footnotes = footnote;
		}
		public static string GetFootnums(string html) {
			string footnums = string.Empty;
			if (html.Contains("<sup>"))
			{
				int startsAt = 5 + html.IndexOf("<sup>");
				int endsAt = html.IndexOf("</sup>");
				footnums = html.Substring(startsAt, endsAt - startsAt);
			}
			return footnums;
		}
		public abstract override string ToString();
		public abstract string toCSV(Year yr, Semester sm, string program, string code, string key, bool isreq = false);
	}
}
