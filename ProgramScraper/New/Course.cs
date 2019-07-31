using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace ProgramScraper.New
{
	class Course : Entry
	{
		public string Department { get; private set; } = string.Empty;
		public string Number { get; private set; } = string.Empty;
		public bool Critical { get; set; } = false;
		public bool Recommended { get; set; } = false;
		public string AUCC { get; set; } = string.Empty;
		public Course AKA { get; set; } = null;
		public Course OR { get; set; } = null;
		public Course AND { get; set; } = null;
		public List<Course> List { get; set; } = new List<Course>();

		private static string[] courseSeprators = new string[] { "/", "or", "&amp;", ","};
		public Course(HtmlNode row, bool isChild = false) : base(row) {
			if (!isChild) this.load();
		}

		public void load() {
			this.GetFootnums();
			//Parses the important aspects of the parent element (<tr>);
			this.ParseParent();
			//Removes empty text nodes that appear due to spaces between elements.
			RemoveEmptyTextNodes(row);
			//Gets the course codes / note for this row
			this.ParseFirstCol(row.FirstChild);
			//Gets the description of the course
			this.ParseSecondCol(row.ChildNodes[1]);
			//If this.List has items set...
			if (this.List.Count > 0)
				this.ParseCourseList();
			//Gets the Critical, Recommended, AUCC, and hours column (if they exist)
			this.ParseLastColumns();
		}

		//Returns true if no further processing of the row is required.
		public void ParseParent() {
			//This is the start of a section of courses
			this.isAreaHeader = row.HasClass("areaheader");
			//This row is the or option for the class above it
			this.isOrClass = row.HasClass("orclass");
		}

		public void ParseFirstCol(HtmlNode col) {
			string html = col.OuterHtml;

			bool isWide = html.Contains("colspan=\"2");
			bool isCodeCol = col.HasClass("codecol");

			string marginRegex = @"margin-left:[\s*]20px;";
			bool hasMargin = (col.HasChildNodes && (Regex.IsMatch(col.FirstChild.OuterHtml, marginRegex) || col.FirstChild.HasClass("blockindent")));

			//If footnotes are present in this column they need to be cut off the text in the column
			string fullName = RemoveFootnoteNumbersIfPresent(col);

			//Indicates this row is 'nested' because it is indented.
			//Courses which are nested organizationally do not always have a margin, but usually they do.
			//So using this.SelectFrom should be used in conjuction with other 'metrics' about the row to 
			//Determine if it is actually a select from row.
			if (hasMargin) this.SelectFrom = true;
			//Indicates this is NOT just a single course (could be aucc/elective/info/select from)
			if (isWide)
			{
				string lowerName = fullName.ToLower();
				this.isSelect = (lowerName.Contains("select") || lowerName.Contains("the following"));
				this.isGroup = lowerName.Contains("group") && !this.isSelect;
				this.isElective = lowerName.Contains("elective") || (lowerName.Contains("additional") && !this.isInfo);
				this.isAUCC = (!this.isSelect && !this.isGroup && !this.isElective && html.Contains("<a href="));
				this.isInfo = new string[] { "must be completed", "benchmark", "recommended" }.Any(lowerName.Contains);
				this.isInfo = this.isInfo || (!this.isSelect && !this.isGroup && !this.isElective && !this.isAUCC);
				this.Note = fullName;
			}
			//Indicates this is a course row
			else if (isCodeCol)
			{
				this.isCourse = true;
				if (this.isOrClass) SetupCourse(this, col.InnerText);
				else this.ParseCourseCode(fullName);
			}
		}
		public void ParseCourseCode(string course) {
			//If there is anything (in parenthesis) in the course code chop it off before processing and store it in the this.Note.
			course = RemoveAndStoreParenthesisEnclosedNoteIfPresent(course);
			//If any of the separating characters exist in the string
			if (courseSeprators.Any(course.Contains))
			{
				bool hasSetThisCourse = false; string courses = course; int i = 0;
				string firstHalf = string.Empty, secondHalf = string.Empty;

				//An action for performing the similar tasks of setup the current course and the 'other' (AKA/OR/AND) course.
				var SetupCourses = new Action<Course, Course>((first, other) => {
					if (!hasSetThisCourse) { SetupCourse(first, firstHalf); hasSetThisCourse = true; }
					SetupCourse(other, secondHalf); i = 1; courses = secondHalf;
				});

				//Another action for adding
				var SetupCoursesWithList = new Action(() => {
					Course first = new Course(this.row, true), other = new Course(this.row, true);
					SetupCourses(first, other); if (this.List.Count == 0) this.List.Add(first);
					this.List.Add(other);
				});
				//For every char in the courses string....
				//The courses string is set the string after the AKA/OR/AND separator is found parsed.
				for (i = 0; i < courses.Length; i++)
				{
					firstHalf = courses.Substring(0, i);
					secondHalf = courses.Substring(i);
					//Found a comma, which puts the course into the course list
					if (courses[i] == ',')
						SetupCoursesWithList();
					//Found the AKA separator
					else if (courses[i] == '/')
						SetupCourses(this, this.AKA = new Course(this.row, true));
					//Found the OR separator
					else if (courses.Length > i + 2 && courses.Substring(i, 3) == "or ")
						SetupCourses(this, this.OR = new Course(this.row, true));
					//Found the AND separator
					else if (courses.Length > i + 5 && courses.Substring(i, 5) == "&amp;")
						SetupCourses(this, this.AND = new Course(this.row, true));
				}
			}
			//Just one course is in the row
			else SetupCourse(this, course);
		}
		private void SetupCourse(Course c, string currentCourse) {
			if (c == null) c = new Course(this.row, true);
			string[] crsInf = DetermineCourseInfo(currentCourse);
			if (crsInf[0].Equals(string.Empty))
			{
				crsInf[0] = this.Department;
			}
			c.Department = crsInf[0];
			c.Number = crsInf[1];
		}
		public string[] DetermineCourseInfo(string fullCode) {
			//Slice of the remainder of the string off if there more ORs, ANDs, AKAs or commas.
			string[] trimmedCode = RemoveWhiteSpaceEntires(fullCode.Split(courseSeprators, StringSplitOptions.RemoveEmptyEntries));
			string[] parts = RemoveWhiteSpaceEntires(trimmedCode[0].Split());
			string[] Course = new string[2];
			if (parts.Length == 1)
			{
				int result = 0;
				//If the only result of the split is a number then
				if (int.TryParse(parts[0], out result))
				{
					Course[0] = string.Empty;
					Course[1] = result.ToString();
				}
				else
				{
					Course[0] = parts[0];
					Course[1] = string.Empty;
				}
			}
			else if (parts.Length == 2)
			{
				Course[0] = parts[0];
				Course[1] = parts[1];
			}
			else
			{
				throw new NotImplementedException("Course code split into more than 2 peices!");
			}
			return Course;
		}

		public void ParseSecondCol(HtmlNode col)
		{
			if (this.isCourse && col.InnerText.Length > 0) {
				this.Description = RemoveFootnoteNumbersIfPresent(col);
			}
		}

		public void ParseLastColumns() {
			foreach (HtmlNode col in this.row.ChildNodes) {
				if (col.InnerText.Trim().Length > 0)
				{
					if (col.HasClass("criticalCol"))		 this.Critical = true;
					else if (col.HasClass("recommendedCol")) this.Recommended = true;
					else if (col.HasClass("aucccol"))		 this.AUCC = col.InnerText.Trim();
					else if (col.HasClass("hourscol"))		 this.Credits = col.InnerText.Trim();
				}
			}
		}
		public string RemoveAndStoreParenthesisEnclosedNoteIfPresent(string fullname) {
			if (new string[] { "(", ")" }.All(fullname.Contains)) {
				int start = fullname.IndexOf("(") + 1;
				int end = fullname.IndexOf(")");
				string note = fullname.Substring(start, end - start).Trim();
				if (note.ToLower().Contains("group"))
					note = "<" + note + ">";

				string credits = Entry.ExtractCreditsFromString(note);
				if (note.ToLower().Contains("credits") && credits.Length <= 5)
				{
					this.Credits = credits;
				}
				else
				{
					this.Note = note;
				}
				fullname = fullname.Remove(start - 1) + fullname.Remove(0, end + 1);
			}
			return fullname.Trim();
		}
		public string RemoveFootnoteNumbersIfPresent(HtmlNode col)
		{
			string html = col.OuterHtml, text = col.InnerText;
			bool hasFootnotes = html.Contains("<sup>") && html.Contains("</sup>");
			if (hasFootnotes)
			{
				text = text.Remove(text.Length - this.Footnums.Length, this.Footnums.Length);
			}
			return text.Trim();
		}
		public bool HasAUCC(string category)
		{
			return AUCC.Contains(category);
		}
		public void ParseCourseList() {
			StringBuilder coursesNote = new StringBuilder();

			if (!this.Department.Equals(string.Empty) && !this.Number.Equals(string.Empty))
			{
				coursesNote.Append(this.ShortTitle() + ", ");
				this.Department = string.Empty;
				this.Number = string.Empty;
			}

			foreach (Course c in this.List)
				coursesNote.Append(c.ShortTitle() + ", ");

			if (this.AND != null)
				coursesNote.Append("and " + this.AND.ShortTitle());
			else if (this.OR != null)
				coursesNote.Append("or " + this.OR.ShortTitle());
			else if (this.AND == null && this.OR == null)
				coursesNote.Remove(coursesNote.Length - 3, 2);

			this.OR = null; this.AND = null;
			this.Note = coursesNote.ToString();
		}
		public string ShortTitle() {
			return this.Department + " " + this.Number;
		}
		public override string ToString()
		{
			StringBuilder toReturn = new StringBuilder();
			if (this.Note != string.Empty && this.Department == string.Empty) toReturn.Append(this.Note);
			else toReturn.Append(this.Department + " " + this.Number);
			if (this.OR != null) toReturn.Append(" OR " + this.OR.ToString());
			if (this.AND != null) toReturn.Append(" AND " + this.AND.ToString());
			if (this.AKA != null) toReturn.Append(" AKA " + this.AKA.ToString());
			return toReturn.ToString();
		}
		public string ToCSV(string key, string code, string program, string year, string semester, bool isreq)
		{
			string and = this.AND != null ? this.AND.ShortTitle() : "";
			string or = this.OR != null ? this.OR.ShortTitle() : "";
			string aka = this.AKA != null ? this.AKA.ShortTitle() : "";

			StringBuilder csv = new StringBuilder();
			csv.AppendFormat(csvFormat, key);                 //Key
			csv.AppendFormat(csvFormat, code);               //ProgramCode
			csv.AppendFormat(csvFormat, program);           //ProgramDescription
			csv.AppendFormat(csvFormat, this.Department);       //Subject
			csv.AppendFormat(csvFormat, this.Number);          //CourseNum
			csv.AppendFormat(csvFormat, year);       //stdClass
			csv.AppendFormat(csvFormat, semester);//Semester
			csv.AppendFormat(csvFormat, this.Note);              //Note
			csv.AppendFormat(csvFormat, and);              //AND
			csv.AppendFormat(csvFormat, or);              //OR
			csv.AppendFormat(csvFormat, this.Credits);        //Credit
			csv.AppendFormat(csvFormat, this.CreditNum);//Creditnum
			csv.AppendFormat(csvFormat, boolAsString(this.Critical));//Critical
			csv.AppendFormat(csvFormat, boolAsString(HasAUCC("4A")));        //AUCC_4A
			csv.AppendFormat(csvFormat, boolAsString(HasAUCC("4B")));       //AUCC_4B
			csv.AppendFormat(csvFormat, boolAsString(HasAUCC("4C")));      //AUCC_4C
			csv.AppendFormat(csvFormat, boolAsString(this.Recommended));//Recommended
			csv.AppendFormat(csvFormat, boolAsString(this.SelectFrom));//SelectFrom
			csv.AppendFormat(csvFormat, aka);                    //AKA
			csv.AppendFormat(csvFormat, boolAsString(this.isCourse));                    //IsCourse
			csv.AppendFormat(csvFormat, this.AUCC);            //Memo
			if (isreq)                                       //Footnotes
				csv.AppendFormat(csvFormat, this.Footnotes);
			csv.AppendLine();
			return csv.ToString();
		}
	}
}
