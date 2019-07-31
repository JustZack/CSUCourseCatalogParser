using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace CoursesScraper
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
		public List<AdditionalInfo> CompletionInfo { get; private set; } = new List<AdditionalInfo>();
		public List<Year> CompletionMap { get; } = new List<Year>();
		public List<AdditionalInfo> RequirementsInfo { get; private set; } = new List<AdditionalInfo>();
		public List<YearRequirements> Requirements { get; } = new List<YearRequirements>();
		public List<CourseList> CourseLists { get; } = new List<CourseList>();
		public bool HasCompletionMap { get; private set; } = false;
		public bool HasRequirements { get; private set; } = false;
		public List<string> Footnotes { get; } = new List<string>();
		private Scraper _scraper { get; }
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
			_scraper = new Scraper(Link.ToString());
			if (loadnow) this.load();
		}
		public override string ToString() {
			StringBuilder sb = new StringBuilder();
			sb.Append(Code + " | ");
			sb.Append(Program + " | ");
			sb.Append(Department + " | ");
			sb.Append(College + " | ");
			sb.Append(Level + " | ");
			sb.Append(OfferedAs + " | ");
			sb.Append(string.Join(" ", DegreeType) + "\n");

			sb.AppendLine("-----COMPLETION MAP-----");
			foreach (Year yr in CompletionMap) {
				sb.Append(yr);
			}

			sb.AppendLine("-----REQUIREMENTS-----");
			foreach (YearRequirements yr in Requirements) {
				sb.Append(yr);
			}

			return sb.ToString();
		}

		//Exports the degree map and requirements to csv files.
		//Returns true if either of the files existed prior to exporting, false otherwise.
		public bool Export(string basePath) {
			string file = basePath + @"\" + Program;
			string map = file + "_map.csv";
			string req = file + "_req.csv";

			bool filesExist = false;
			if (File.Exists(map) || File.Exists(req)) filesExist = true;

			Console.Write("Begging to export Completion Map for " + Program + "...\t");
			using (StreamWriter sw = new StreamWriter(map))
			{
				sw.Write(getCompletionCSV());
			}
			Console.WriteLine("Exported " + Program + " Completion Map");

			Console.Write("Beggining to export Requirements for " + Program + "...\t");
			using (StreamWriter sw = new StreamWriter(req))
			{
				sw.Write(getRequirementsCSV());
			}
			Console.WriteLine("Exported " + Program + " Requirements");
			return filesExist;
		}

		public string getCompletionCSV() {
			if (!HasCompletionMap) return "";

			StringBuilder sb = new StringBuilder();

			if (CompletionInfo.Count > 0)
				foreach (AdditionalInfo info in CompletionInfo)
					sb.Append(info.toCompletionCSV(ProgramTitle, Code, Key));

			if (CompletionMap.Count > 0)
				foreach (Year yr in CompletionMap)
					sb.Append(yr.toCSV(ProgramTitle, Code, Key));

			return sb.ToString();
		}
		public string getRequirementsCSV() {
			if (!HasRequirements) return "";

			StringBuilder sb = new StringBuilder();

			if (RequirementsInfo.Count > 0)
				foreach (AdditionalInfo info in RequirementsInfo)
					sb.Append(info.toCompletionCSV(ProgramTitle, Code, Key, true));

			if (Requirements.Count > 0)
				foreach (YearRequirements yr in Requirements)
					sb.Append(yr.toCSV(ProgramTitle, Code, Key));

			else if (CourseLists.Count > 0)
				foreach (CourseList cl in CourseLists)
					sb.Append(cl.toCSV(ProgramTitle, Code, Key));

			return sb.ToString();
		}

		public void load() {
			this.loadFootnotes();
			this.loadTitle();
			this.loadMajorCompletion();
			this.loadRequirements();
			this.loadCourseFootnotes();
		}
		public void loadFootnotes() {
			List<HtmlNode> found = _scraper.query("//table[@class='sc_footnotes']//tr");
			foreach (HtmlNode n in found) {
				string note = n.LastChild.InnerText;
				Footnotes.Add(note);
			}
		}
		public void loadTitle()
		{
			List<HtmlNode> found = _scraper.query("//h1[@class='page-title']");
			if (found != null && found.Count > 0)
				ProgramTitle = found[0].InnerText;
		}
		public List<AdditionalInfo> GetAdditionalRequirements(string id) {
			string baseXpath = "//div[@id='" + id + "']";

			//Pulls things like: "Effective Fall 2018"
			//Need to check for there being an additional div containing as it is inconsistant.
			List<HtmlNode> found = _scraper.query(baseXpath + "/h2");
			found.AddRange(_scraper.query(baseXpath + "/div/h2"));

			//Pulls things like: "This major requires a 2.0 in all AUCC courses."
			//Need to check for there being an additional div containing as it is inconsistant.
			found.AddRange(_scraper.query(baseXpath + "/p"));
			found.AddRange(_scraper.query(baseXpath + "/div/p"));

			List<AdditionalInfo> info = new List<AdditionalInfo>();
			//Adds each peice of information to the info list
			foreach (HtmlNode n in found)
				if (n.InnerText.Trim().Length > 1)
					info.Add(new AdditionalInfo(n.InnerText));

			return info;
		}
		private void loadMajorCompletion() {
			//Query for all table rows in the major completion table
			List<HtmlNode> found = _scraper.query("//table[@class='sc_mcgrid']//tr");
			if (found == null || found.Count == 0) return;
			//Mark that this degree program has a completion map on the page.
			HasCompletionMap = true;

			string msg = "Loading Major Completion for " + Program + ", " + Level + ", " + string.Join(" ", DegreeType);
			msg = msg.Substring(0, (msg.Length < 125 ? msg.Length : 125));
			if (printStatus) Console.Write(String.Format("{0, -125} | ", msg) + "\t\t");

			CompletionInfo = GetAdditionalRequirements("majorcompletionmaptextcontainer");

			Year yr = null;
			Semester sm = null;

			HtmlNode n = null;
			for (int i = 0; i < found.Count; i++) {
				n = found[i];
				if (n.HasClass("hidden") || n.ParentNode.Name == "thead") continue;
				//If this is the level row (FRESHMAN | SOPHOMORE | JUNIOR | SENIOR)
				if (n.HasClass("plangridyear"))
				{
					//Add the year, which by this point has been filled out with each course, to the completion map
					if (yr != null) CompletionMap.Add(yr);
					//Start the next year with the level found in this row
					yr = new Year(n.InnerText.Trim());
				}
				//If this is the term row (SEMESTER #)
				else if (n.HasClass("plangridterm"))
				{
					//Start the next semester
					sm = new Semester(n.FirstChild.InnerText.Trim());
				}
				//If this is the credit sum (of the semester) row
				else if (n.HasClass("plangridsum"))
				{
					//Add the total credits to the semester
					sm.Credits = n.LastChild.InnerText.Trim();
					//Add the semester to the year
					if (sm != null) yr.AddSemester(sm);
				}
				//If this is the credit total row OR is marked as the last row
				else if (n.HasClass("plangridtotal") && n.HasClass("lastrow")) {
					//Grab the total credits required for this program
					this.Credits = n.LastChild.InnerText;
					//Add the final year to the degree map.
					if (yr != null) CompletionMap.Add(yr);
					break;
				}
				//Otherwise this is a course row of some sort.
				else
				{
					sm.AddCourse(Course.GetCourse(found, ref i));
				}
			}
			if (printStatus) Console.WriteLine(CompletionMap.Count + " Years found");
		}
		private void loadRequirements() {
			//Query for all table rows in the major completion table
			List<HtmlNode> found = _scraper.query("//table[@class='sc_plangrid ']//tr");
			if (found != null && found.Count > 0) this.loadFromPlanGrid(found);
			else {
				found = _scraper.query("//table[@class='sc_courselist']//tr");
				if (found != null && found.Count > 0) this.loadFromCourseList(found);
			}

		}
		private void SetupRequirements() {
			//Mark that this degree program has requirements on the page.
			HasRequirements = true;

			string msg = "Loading Requirements for " + Program + ", " + Level + ", " + string.Join(" ", DegreeType);
			msg = msg.Substring(0, (msg.Length < 125 ? msg.Length : 125));
			if (printStatus) Console.Write(String.Format("{0, -125} | ", msg) + "\t\t");

			//Grabs hidden text too, so maybe not usefull.
			/*foreach (string id in new []{ "textcontainer", "requirementstextcontainer"})
				RequirementsInfo.AddRange(GetAdditionalRequirements(id));*/
			RequirementsInfo.AddRange(GetAdditionalRequirements("requirementstextcontainer"));
		}
		private void loadFromPlanGrid(List<HtmlNode> found) {

			this.SetupRequirements();

			YearRequirements yr = null;

			HtmlNode n = null;
			for (int i = 0; i < found.Count; i++)
			{
				n = found[i];
				if (n.HasClass("hidden") || n.ParentNode.Name == "thead") continue;

				//If this is the level row (FRESHMAN | SOPHOMORE | JUNIOR | SENIOR)
				if (n.HasClass("plangridyear"))
				{
					//Add the year, which by this point has been filled out with each course, to the completion map
					if (yr != null) Requirements.Add(yr);
					//Start the next year with the level found in this row
					yr = new YearRequirements(n.InnerText.Trim());
				}
				//Requirements have these, but they are not labeled, so skip the row.
				else if (n.HasClass("plangridterm")) continue;
				//If this is the credit sum (of the semester) row
				else if (n.HasClass("plangridsum"))
				{
					//Add the total credits to the semester
					yr.Credits = n.LastChild.InnerText.Trim();
				}
				//If this is the credit total row OR is marked as the last row
				else if (n.HasClass("plangridtotal") && n.HasClass("lastrow"))
				{
					//Add the final year to the degree map.
					if (yr != null) Requirements.Add(yr);
					break;
				}
				//Otherwise this is a course row of some sort.
				else
				{
					GenericCourse crs = Course.GetCourse(found, ref i);
					yr.AddCourse(crs);
				}
			}

			if (printStatus) Console.WriteLine(Requirements.Count + " Years found");
		}
		private void loadFromCourseList(List<HtmlNode> found) {

			this.SetupRequirements();

			CourseList cl = null;
			HtmlNode n = null;
			GenericCourse crs = null;
			//if (ProgramTitle == "Minor in Agricultural Business") Debugger.Break();
			for (int i = 0; i < found.Count; i++)
			{
				n = found[i];

				if (n.HasClass("hidden") || n.ParentNode.Name == "thead") continue;

				if (n.HasClass("orclass")) {
					string orCrs = n.FirstChild.InnerText;
					if (orCrs.Contains("or")) {
						int orIndex = orCrs.IndexOf("or") + 2;
						orCrs = orCrs.Substring(orIndex, orCrs.Length - orIndex).Trim();
					}
					cl.Courses[cl.Courses.Count - 1].OR = orCrs;
					continue;
				}

				if (n.HasClass("firstrow")) {
					cl = new CourseList();
				} else if (n.HasClass("listsum")) {
					if (cl != null)
						CourseLists.Add(cl);

					if (Credits == null /*|| int.Parse(Credits) < int.Parse(n.LastChild.InnerText)*/)
						this.Credits = n.LastChild.InnerText;

					continue;
				}

				if (n.HasClass("areaheader")) {
					string[] desc_footnums = Course.getDescAndFootnotes(n);
					crs = new ProgramInfo(desc_footnums[0], false, false);
					crs.Footnotes = desc_footnums[1];
				} else {
					crs = Course.GetCourse(found, ref i);
				}


				//TODO: Write a function for getting all footnotes at once.
				if (crs != null) {
					cl.AddCourse(crs);
				}
			}

			if (printStatus) Console.WriteLine(CourseLists.Count + " Requirements<Course Lists> found");
		}
		private void loadCourseFootnotes() {
			//Load footnotes for the completion map
			foreach (Year yr in CompletionMap) {
				if (yr.Fall != null) loadListOfCourseFootnotes(yr.Fall.Courses);
				if (yr.Spring != null) loadListOfCourseFootnotes(yr.Spring.Courses);
				if (yr.Summer != null) loadListOfCourseFootnotes(yr.Summer.Courses);
			}
			//Load footnotes for the requirements (by year)
			foreach (YearRequirements yr in Requirements) loadListOfCourseFootnotes(yr.Courses);
			//Load footnotes for the requirements (by course list)
			foreach (CourseList cl in CourseLists) loadListOfCourseFootnotes(cl.Courses);
		}
		private void loadListOfCourseFootnotes(List<GenericCourse> courses) {
			if (courses.Count == 0) return;
			foreach (GenericCourse crs in courses)
				getAllFootnotes(crs);
		}
		private void getAllFootnotes(GenericCourse root) {
			if (root is CourseOption) {
				iterateListOfCourses(root);
			} else {
				root.GetFootnotes(this.Footnotes);
			}
		}
		private void iterateListOfCourses(GenericCourse co) {
			var crsOp = co as CourseOption;
			co.GetFootnotes(this.Footnotes);
			foreach (GenericCourse crs in crsOp.Courses) {
				getAllFootnotes(crs);
			}
		}
	}
}
