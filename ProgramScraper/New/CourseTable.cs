using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using CoursesScraper;

namespace ProgramScraper.New
{
	class CourseTable
	{
		public List<List<Entry>> Courses { get; private set; } = new List<List<Entry>>();
		public List<List<string>> AdditionalInformation { get; private set; } = new List<List<string>>();
		public bool isRequirements { get; private set; } = false;
		public string parentID { get; } = string.Empty;
		private string tableClass = string.Empty;
		private Scraper Document;
		public CourseTable(string parent_id, string table_class, Scraper document, bool is_requirements) {
			this.parentID = parent_id;
			this.tableClass = table_class;
			this.Document = document;
			this.isRequirements = is_requirements;
			this.load();
		}
		public CourseTable(string parent_id, Scraper document, bool is_requirements)
		{
			this.parentID = parent_id;
			this.Document = document;
			this.isRequirements = is_requirements;
			this.load();
		}
		public string ToCSV(string key, string code, string program, List<List<string>> AdditionalInfo) {
			StringBuilder csv = new StringBuilder();
			string year = string.Empty, semester = string.Empty;

			for (int i = 0; i < this.Courses.Count;i++) {
				//Add all of the additional information for this CourseTable first
				if (i < AdditionalInfo.Count)
					foreach (string info in AdditionalInfo[i])
						csv.Append(Entry.ToCSV(key, code, program, year, semester, info, this.isRequirements));

				//Then add the courses in this course table
				List<Entry> ct = this.Courses[i];
				year = string.Empty; semester = string.Empty;
				foreach (Entry e in ct)
				{
					if (e.isYear) year = e.Name;
					else if (e.isTerm) semester = e.Name;
					else if (e.isSum) ; //Do nothing for now, this number is not needed in the output.
					else if (e.isTotal) ; //Do nothing for now, this isnt needed either.
					else if (e.isOrClass) ;//Skip these, OrClass Courses are consolidated in this.ParseCourseRowsAndInfo()
					else {
						if (e is Course)
						{
							Course c = e as Course;
							csv.Append(c.ToCSV(key, code, program, year, semester, this.isRequirements));
						}
						else
						{
							csv.Append(e.ToCSV(key, code, program, year, semester, this.isRequirements));
						}
					}
				}
				year = string.Empty; semester = string.Empty;
			}
			return csv.ToString();

		}
		public void load() {
			this.LoadCourseRows();
			this.ParseCourseRows();
		}
		
		//Gets each row in the selected course tables on the webpage
		//Does not attempt to parse all aspects of the table, this method simply gets each row.
		public void LoadCourseRows() {
			string xPath = "//div[@id='{0}']//table";
			if (!this.tableClass.Equals(string.Empty))
				xPath += "[contains(@class, '{1}')]";

			List<HtmlNode> found = Document.query(String.Format(xPath, this.parentID, this.tableClass));
			int tableIndex = 0;
			//Each table found from the supplied xpath.
			foreach (HtmlNode table in found)
			{
				if (table.HasClass("sc_footnotes")) break;
				//Query for table rows AFTER querying for the table itself
				//This ensures each separation of table rows when many tables are present
				List<HtmlNode> rows = Document.query(table.XPath + @"//tr");
				//Initialize the next list of courses before iterating over the courses.
				Courses.Add(new List<Entry>());
				//Each childnode
				foreach (HtmlNode row in rows)
				{
					//If this is a hidden, header, or empty then skip it outright.
					if (row.HasClass("hidden") || row.ParentNode.Name == "thead" || row.InnerText.Trim().Length == 0)
					{
						continue;
					}
					else if (row.HasClass("plangridyear"))
					{
						Courses[tableIndex].Add(new Year(row));
					}
					else if (row.HasClass("plangridterm"))
					{
						Courses[tableIndex].Add(new Term(row));
					}
					else if (row.HasClass("plangridsum"))
					{
						Courses[tableIndex].Add(new SumCredits(row));
					}
					else if (row.HasClass("plangridtotal") || row.HasClass("listsum"))
					{
						Courses[tableIndex].Add(new TotalCredits(row));
					}
					else
					{
						Courses[tableIndex].Add(new Course(row));
					}
				}
				//Move onto the next table
				tableIndex++;
			}
		}

		//Goes each row found and parses them relative to eachother.
		//Ex: Courses under a 'select from'/'group X' list get the '<select from>'/'<group>' message.
		//Ex: OR courses / lists of OR courses are consolidated into their parent courses.
		public void ParseCourseRows()
		{
			string selectNote = string.Empty, groupNote = string.Empty, sectionCredits = string.Empty;
			for (int i = 0; i < Courses.Count; i++) {
				List<Entry> ct = Courses[i];
				for (int j = 0; j < ct.Count; j++)
				{
					Entry row = ct[j];

					//Parse the OR course, which is on the line following its 'parent' course.
					if (row.isOrClass && j > 0) ParseOrClass(ct, j);
					//Parse lists of courses (groups, select from)
					else if (row.isSelect || row.isGroup || row.SelectFrom)
						ParseSelect(row, ref selectNote, ref groupNote, ref sectionCredits);
					else {
						selectNote = string.Empty;
						groupNote = string.Empty;
						sectionCredits = string.Empty;
					}
				}
			}
		 }
		private void ParseSelect(Entry row, ref string selectNote, ref string groupNote, ref string sectionCredits) {
			if (row.isSelect || row.isGroup)
			{
				if (row.HasCredits()) sectionCredits = row.Credits;

				if (row.isSelect)
				{
					selectNote = String.Format("<{0}>", row.Note);
					groupNote = string.Empty;
				}
				else if (row.isGroup) groupNote = String.Format("<{0}>", row.Note);
			}
			else if (row.SelectFrom)
			{
				string courseNote = row.Note, note = selectNote + groupNote;
				//If there is already a note and it is group related text
				if (!courseNote.Equals(string.Empty) && courseNote.ToLower().Contains("group "))
					courseNote = note.Insert(0, String.Format("<{0}>", courseNote));
				else
					courseNote = note;

				row.Note = courseNote;
				if (!row.HasCredits()) row.Credits = sectionCredits;
			}
		}
		private void ParseOrClass(List<Entry> ct, int index) {
			//Grab the previous course, which will hold refrences to all of the following OrClass's.
			Course previous = ct[index - 1] as Course, current;
			List<Course> OrList = new List<Course>();
			
			//While the current course is an OrClass
			//Add the current course to the list and remove it from the courseList.
			while (index < ct.Count && (current = ct[index] as Course) != null && current.isOrClass){
				OrList.Add(current); ct.RemoveAt(index);
			}

			//This should NEVER be true because 
			//the only way into this method is for a row to be an OrClass
			if (OrList.Count == 0) ; //Keep this to avoid confusion.
			else if (OrList.Count == 1) previous.OR = OrList[0];
			else
			{
				//Reverse the list. 
				//Reasoning: So I can omit the last element using .Skip(1) (.ReverseSkip(1) isnt a thing)
				OrList.Reverse();
				//Take the first (last) element
				previous.OR = OrList[0];
				//Store all but the first (last) element of the list into th course.List
				previous.List = OrList.Skip(1).ToList();
				//Reverse the List back to its original order
				previous.List.Reverse();
				previous.ParseCourseList();
			}
		}
	}
}
