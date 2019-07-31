using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Linq;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace ProgramScraper.New
{
	class CatalogProgram
	{
		public string Code { get; set; } = null;
		public string Program { get; set; }
		public string ProgramTitle { get; set; } = string.Empty;
		public string Key { get; set; } = string.Empty;
		public string Department { get; }
		public string College { get; }
		public string Level { get; }
		public string OfferedAs { get; }
		public string[] DegreeType { get; }
		public Uri Link { get; }
		public string Credits { get; private set; }
		public List<CourseTable> CourseTables { get; } = new List<CourseTable>();
		public Dictionary<string, List<List<string>>> AdditionalInformation { get; } = new Dictionary<string, List<List<string>>>();
		public List<string> Footnotes { get; } = new List<string>();
		private CoursesScraper.Scraper Document { get; }
		private static string[] InfoElements = new string[] { "h2", "h3", "h4", "p" };
		public static string CompletionCSVHeader = "Key, ProgramCode, ProgramDescription, Subject, CourseNum, stdtClass, Semester, Note, AND, OR, Credit, CreditNum, Critical, AUCC_4A, AUCC_4B, AUCC_4C, Recommended, SelectFrom, AKA, IsCourse, Memo\n";
		public static string RequirementsCSVHeader = "Key, ProgramCode, ProgramDescription, Subject, CourseNum, stdtClass, Semester, Note, AND, OR, Credit, CreditNum, Critical, AUCC_4A, AUCC_4B, AUCC_4C, Recommended, SelectFrom, AKA, IsCourse, Memo, Footnote\n";
		private static bool printStatus = false;
		public CatalogProgram(string[] attrs, bool loadnow = false) : this(attrs[0], attrs[1], attrs[2], attrs[3], attrs[4], attrs[5], attrs[6], loadnow) { }
		public CatalogProgram(string prog, string dept, string college, string lvl, string offeredAs, string type, string link, bool loadnow = false) {
			Program = prog;
			Department = dept;
			College = college;
			Level = lvl;
			OfferedAs = offeredAs;
			DegreeType = type.Split(new char[2] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
			Link = new Uri(link);
			Document = new CoursesScraper.Scraper(Link.ToString());
			if (loadnow) 
				this.load();
		}

		public override string ToString() {
			return this.ProgramTitle;
		}
		public string ToCSV(bool asRequirements = false) {
			if (this.CourseTables.Count == 0) return "";
			StringBuilder csv = new StringBuilder();
			foreach (CourseTable ct in CourseTables) {
				if (ct.isRequirements == asRequirements)
					csv.Append(ct.ToCSV(this.Key, this.Code, this.ProgramTitle, AdditionalInformation[ct.parentID]));
			}
			return csv.ToString();
		}
		public void load() {
			this.loadTitle();
			this.loadFootnotes();
			this.loadCourseTables();
			this.loadAdditionalInformation();
			this.loadCourseFootnotes();
		}
		public void loadTitle()
		{
			List<HtmlNode> found = Document.query("//h1[@class='page-title']");
			if (found != null && found.Count > 0)
				ProgramTitle = found[0].InnerText;
		}
		public void loadFootnotes()
		{
			List<HtmlNode> found = Document.query("//table[@class='sc_footnotes']//tr");
			foreach (HtmlNode n in found)
			{
				string note = n.LastChild.InnerText;
				Footnotes.Add(note);
			}
		}

		private void loadCourseTables() {
			//Load the courses for each distinct type of table that may be on the page.

			//Completion Map
			//Present on: Majors
			CourseTables.Add(new CourseTable("majorcompletionmaptextcontainer", Document, false));
			//Requirements when there is a Completion Map for this program too. 
			//Present on: Majors
			CourseTables.Add(new CourseTable("requirementstextcontainer", Document, true));
			//Requirements when the table is the only content. 
			//Present on: Certificates
			CourseTables.Add(new CourseTable("textcontainer", Document, true));

			/* old way of doing things, turns out to be too specific when the tables class is included.
			//Completion Map
			//Present on: Majors
			courseTables.Add(new CourseTable("majorcompletionmaptextcontainer", "sc_mcgrid", Document, false));
			//Requirements when there is a Completion Map for this program too. 
			//Present on: Majors
			//courseTables.Add(new CourseTable("requirementstextcontainer", "sc_plangrid", Document, true));
			//Requirements when no Completion Map is present, but there is a 'Requirements' button to click. 
			//Present on: Minors, Masters, Ph.D's
			courseTables.Add(new CourseTable("requirementstextcontainer", "sc_courselist", Document, true));
			//Requirements when the table is the only content. 
			//Present on: Certificates
			courseTables.Add(new CourseTable("textcontainer", "sc_courselist", Document, true));
			*/
			//Since some of the course tables will be empty, remove any without courses.
			CourseTables.RemoveAll(tbl => tbl.Courses.Count == 0);
		}

		private void loadAdditionalInformation() {
			//load the additional information found before, between, and after tables.

			//Get major completion info
			getInfoFor("majorcompletionmaptextcontainer");

			//Get requirements info
			getInfoFor("requirementstextcontainer");

			//Get requirements info in a slightly different place
			getInfoFor("textcontainer");
		}

		private void getInfoFor(string id) {
			AdditionalInformation.Add(id, new List<List<string>>());
			List<HtmlNode> found = Document.query(String.Format("//div[@id=\"{0}\"]", id));
			if (found.Count == 0) return;

			int tbl = 0; List<List<string>> info = AdditionalInformation[id];
			info.Add(new List<string>());
			//Foreach child node of this div
			foreach (HtmlNode row in found[0].ChildNodes) {
				//If we reach the footnotes table all of the information has been processed.
				if (row.HasClass("sc_footnotes")) break;
				//Everytime a (nested?) table is encoutered create a new list of additional information for the new table.
				else if (row.Name == "table" || (row.HasClass("onecol") && row.HasChildNodes && row.ChildNodes.FindFirst("table") != null))
				{
					info.Add(new List<string>());
					tbl++;
				}
				//If this element is an h2, h3, or p then add its text to the additional information list.
				else if (InfoElements.Any(row.Name.Equals))
					info[tbl].Add(row.InnerText.Trim());
			}

		}

		//Go through every course row,
		//	If the row has footnumbers:
		//		Attempt to assoicate the footnotes found on the page with the footnums found in the row.
		//	If the footnum does not match a footnote:
		//		Leave a message explaining that there was not matching footnote.	
		public void loadCourseFootnotes() {
			foreach (CourseTable ct in CourseTables) {
				foreach (List<Entry> block in ct.Courses) {
					foreach (Entry row in block) {
						if (!row.Footnums.Equals(string.Empty)) {
							string[] nums = row.Footnums.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
							foreach (string num in nums) {
								int fNum = -1;
								if (int.TryParse(num, out fNum))
								{
									string toAdd = string.Empty;
									if (fNum <= this.Footnotes.Count)
										toAdd = String.Format("*{0} {1} ", num, this.Footnotes[fNum - 1]);
									else
										toAdd = String.Format("*{0} Footnote #{0} does not exist on this page. ", num);
									row.Footnotes += toAdd;
								}
							}
						}
					}
				}
			}
		}
	}
}
