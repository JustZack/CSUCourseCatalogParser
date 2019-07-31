using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace CoursesScraper
{
	class Course : GenericCourse {
		public Course(string dept, string number, string note, bool critical, bool recommeded, string aucc, string credits) :
				 base(note, credits, dept, number, critical, recommeded, aucc) { }

		
		public override string ToString() {
			return Department + " " + Number + "\t" + Note + "\t" + Critical + "\t" + Recommended + "\t" + string.Join(",", AUCC) + "\t" + Credits + '\n';
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
			sb.AppendFormat(csvFormat, AND);              //AND
			sb.AppendFormat(csvFormat, OR);              //OR
			sb.AppendFormat(csvFormat, Credits);        //Credit
			sb.AppendFormat(csvFormat, GetCreditNum());//Creditnum
			sb.AppendFormat(csvFormat, Critical ? "TRUE" : "");//Critical
			sb.AppendFormat(csvFormat, HasAUCC("4A"));        //AUCC_4A
			sb.AppendFormat(csvFormat, HasAUCC("4B"));       //AUCC_4B
			sb.AppendFormat(csvFormat, HasAUCC("4C"));      //AUCC_4C
			sb.AppendFormat(csvFormat, Recommended ? "TRUE" : "");//Recommended
			sb.AppendFormat(csvFormat, SelectFrom ? "TRUE" : "");//SelectFrom
			sb.AppendFormat(csvFormat, AKA);                    //AKA
			sb.AppendFormat(csvFormat, "");                    //IsCourse
			sb.AppendFormat(csvFormat, GetAUCC());            //Memo
			if (isreq)										 //Footnotes
				sb.AppendFormat(csvFormat, Footnotes);          
			sb.AppendLine();
			return sb.ToString();
		}

		public static GenericCourse GetCourse(List<HtmlNode> nodes, ref int i) {
			HtmlNode n = nodes[i];
			HtmlNode code = null, title = null, critical = null, recommended = null, aucc = null, hours = null;
			foreach (HtmlNode data in n.ChildNodes) {
				if (data.HasClass("codecol ")) code = data;
				else if (data.HasClass("titlecol ")) title = data;
				else if (data.HasClass("criticalcol")) critical = data;
				else if (data.HasClass("recommendedcol")) recommended = data;
				else if (data.HasClass("aucccol")) aucc = data;
				else if (data.HasClass("hourscol")) hours = data;
			}

			bool hasCodecol = code != null;
			bool hasLink = hasCodecol && code.InnerHtml.Contains("class=\"bubblelink code\"");

			bool hasTitlecol = title != null;

			bool hasWideFirstCol = n.InnerHtml.Contains("<td colspan=\"2\"");

			bool hasCriticalCol = critical != null;

			bool hasRecommendedCol = recommended != null;

			bool hasAucccol = aucc != null;

			bool hasHourscol = hours != null;
			bool hasHourstext = hours != null && hours.InnerText.Length > 0;

			string firsttxt = n.FirstChild.InnerText.ToLower();
			bool hasElective = firsttxt.Contains("elective") || firsttxt.Contains("additional");
			//Check if the word select is in the row, but it isnt in the past tense (meaning it isnt a "Select x credits from the following" sort of row)
			bool hasSelect = firsttxt.Contains("select") && (firsttxt.IndexOf("select") != firsttxt.IndexOf("selected"));
			bool hasGroup = firsttxt.Contains("group");
			bool hasInfo = firsttxt.Contains("recommended") || firsttxt.Contains("benchmark") || firsttxt.Contains("must be");

			GenericCourse course = null;
			if (hasWideFirstCol) {
				if (hasSelect) {
					//[Select one course from the following] || [Select one group from the following]
					//Course groups always have the word select in the row above them, and this method finds groups if they are present.
					course = getCourseOptions(nodes, n, ref i);
				} else if (hasElective) {
					//[Elective(s)] || [Technical Electives List x]
					course = getElectiveCourse(n);
				} else if (hasInfo) {
					//Information about when courses must be completed
					course = getProgramInfo(n);
				} else {
					//[Arts and Humanites] (AUCC courses
					course = getAUCCCourse(n);
				}
			} else {
				//Normal courses
				course = getCourse(n);
			}
			return course;
		}

		private static Elective getElectiveCourse(HtmlNode n, string creditsCO = "") {
			string name = n.FirstChild.InnerText.Trim();
			if (n.FirstChild.InnerHtml.Contains("sup")) name = name.Remove(name.Length - 1);

			bool[] critrec = getCritRec(n);

			string aucc = getAucc(n);
			string credits = (!creditsCO.Equals(string.Empty)) ? creditsCO : n.LastChild.InnerText.Trim();
			return new Elective(name, critrec[0], critrec[1], credits, aucc);
		}

		private static AUCCCourse getAUCCCourse(HtmlNode n) {
			string auccCategory = n.FirstChild.InnerText.Trim();
			if (n.FirstChild.InnerHtml.Contains("sup")) auccCategory = auccCategory.Remove(auccCategory.Length - 1);
			bool[] critrec = getCritRec(n);

			string aucc = getAucc(n);


			string credits = n.LastChild.InnerText.Trim();
			return new AUCCCourse(auccCategory, critrec[0], critrec[1], credits, aucc);
		}

		private static ProgramInfo getProgramInfo(HtmlNode n) {
			string[] desc_footnums = Course.getDescAndFootnotes(n);
			bool[] critrec = getCritRec(n);
			ProgramInfo pi = new ProgramInfo(desc_footnums[0], critrec[0], critrec[1]);
			pi.Footnotes = desc_footnums[1];
			return pi;
		}

		private static GenericCourse getCourse(HtmlNode n, string creditsCO = "") {
			string[] desc_footnotes = getDescAndFootnotes(n);
			string crsCode = desc_footnotes[0], footnotes = desc_footnotes[1];

			//If this course text looks like an elective then parse it as one
			if (crsCode.ToLower().Contains("elective") || crsCode.ToLower().Contains("additional"))
				return getElectiveCourse(n, creditsCO);
			//If this "course" is information, parse it as such
			if (crsCode.ToLower().Contains("must be completed") || crsCode.ToLower().Contains("benchmark") || crsCode.ToLower().Contains("recommended"))
				return getProgramInfo(n);
			//If this course does not contain numbers (and was not caught by anything else), it is an AUCC course.
			if (!Regex.IsMatch(crsCode, @"\d"))
				return getAUCCCourse(n);
			//string desc = n.ChildNodes[1].InnerText.Trim();

			bool[] critrec = getCritRec(n);

			string aucc = getAucc(n);
			string credits = (!creditsCO.Equals(string.Empty)) ? creditsCO : n.LastChild.InnerText.Trim();

			GenericCourse course = null;
			string dept, num; 
			if (crsCode.Contains("&amp; ")) {
				string[] courses = crsCode.Split(new[] { "&amp; " }, StringSplitOptions.None);

				dept = courses[0].Split()[0]; num = courses[0].Split()[1];
				course = new Course(dept, num, "", critrec[0], critrec[1], aucc, credits);
				dept = courses[1].Split()[0]; num = courses[1].Split()[1];

				course.AND = dept + " " + num;
			} else if (crsCode.Contains(" or ")) {
				//[CS 163 or 164] for SEMESTER 2
				//http://catalog.colostate.edu/general-catalog/colleges/natural-sciences/physics/physics-major-applied-concentration/#majorcompletionmaptext
				//Want to parse as [CS 163 desc], [CS 164 desc]
				string[] crs = crsCode.Split(new[] { " or " }, StringSplitOptions.None);
				dept = crs[0].Split()[0].Trim();
				num = crs[0].Split()[1].Trim();
				string num2 = crs[1].Trim();


				//desc = n.ChildNodes[1].FirstChild.InnerText.Trim();
				//string desc2 = n.ChildNodes[1].LastChild.InnerText.Trim();
				course = new Course(dept, num, "", critrec[0], critrec[1], aucc, credits);
				course.OR = dept + "" + num2;
			} else if (crsCode.Contains("/")) {
				string[] courses = crsCode.Split('/');
				dept = courses[0].Split()[0];
				num = courses[0].Split()[1];
				course = new Course(dept, num, "", critrec[0], critrec[1], aucc, credits);
				course.AKA = courses[1];
			} else {
				dept = crsCode.Split()[0]; num = crsCode.Split()[1];
				course = new Course(dept, num, "", critrec[0], critrec[1], aucc, credits);
			}
			if (course.Department == "BMS" && course.Number == "495") Debugger.Break();
			course.Footnotes = footnotes;
			return course;
		}
		public static string getAucc(HtmlNode n) {
			int num_children = n.ChildNodes.Count;
			string aucc = string.Empty;
			if (n.ChildNodes[num_children - 2].HasClass("aucccol"))
				aucc = n.ChildNodes[num_children - 2].InnerText.Trim();
			return aucc;
		}
		public static bool[] getCritRec(HtmlNode n) {
			bool[] critrec = { false, false };

			int num_children = n.ChildNodes.Count;
			if (num_children - 3 >= 2)
				critrec[0] = n.ChildNodes[num_children - 4].InnerText.Trim().Length > 0;
			if (num_children - 2 >= 3)
				critrec[1] = n.ChildNodes[num_children - 3].InnerText.Trim().Length > 0;

			return critrec;
		}
		public static string[] getDescAndFootnotes(HtmlNode n) {
			string desc = n.FirstChild.InnerText.Trim();
			
			string footnotes = Course.GetFootnums(n.InnerHtml);
			//Only take the footnote <sup></sup> off if it is present in the description
			if (footnotes.Length > 0 && n.FirstChild.InnerHtml.Contains(footnotes)) desc = desc.Remove(desc.Length - (footnotes.Length + 1)).Trim();

			return new[] { desc, footnotes };
		}
		private static MultiCourse getCourseGroup(List<HtmlNode> found, HtmlNode n, ref int i, string credits) {
			string[] desc_footnotes = getDescAndFootnotes(n);
			MultiCourse mc = new MultiCourse(desc_footnotes[0], credits);
			mc.Footnotes = desc_footnotes[1];

			while (((n = found[++i]).ChildNodes[1].HasClass("titlecol") || n.ChildNodes[1].InnerText.Length > 0) 
			    && (!n.LastChild.HasClass("hourscol") || n.LastChild.InnerText.Trim().Length == 0)) {
				if (n.InnerText.Length == 0 && n.FirstChild.FirstChild == null && 
					found[i + 1].FirstChild.FirstChild != null && found[i + 1].FirstChild.FirstChild.Name == "div") {
					//This was an empty row in the data table, so move to the next one.
					continue;
				}
				//If the next row has the orclass, turn this section into a select from
				if (found[i + 1].FirstChild.HasClass("orclass"))
				{
					mc.Add(getOrCourses(found, ref i, credits));
				} else {
					mc.Add(getCourse(n, credits));
				}

			}
			--i;
			return mc;
		}

		private static CourseOption getCourseOptions(List<HtmlNode> found, HtmlNode n, ref int i) {
			string[] desc_footnotes = getDescAndFootnotes(n);
			string credits = n.LastChild.InnerText.Trim();

			CourseOption co = new CourseOption(desc_footnotes[0], credits);
			co.Footnotes = desc_footnotes[1];

			//while the next row is one of the course options (first child has div, last child lacks hourscol class)
			while (!(n = found[++i]).LastChild.HasClass("hourscol") || n.LastChild.InnerText.Trim().Length == 0 || n.FirstChild.InnerText.Contains("Group")) {
				if (n.InnerText.Length == 0 && n.FirstChild.FirstChild == null &&
					found[i + 1].FirstChild.FirstChild != null && found[i + 1].FirstChild.FirstChild.Name == "div") {
					//This was an empty row in the data table, so move to the next one.
					continue;
				}
				
				//If the next row has the orclass, turn this section into a select from
				if (found[i + 1].FirstChild.HasClass("orclass")) {
					co.Add(getOrCourses(found, ref i, credits));
				}
				//If the word group is found in this row, at the heading of this set, or the only text in this row is in the first column...
				//A course group was found.
				else if (n.FirstChild.InnerText.ToLower().Contains("group") || co.Note.ToLower().Contains("group") || n.InnerText.Trim().Equals(n.FirstChild.InnerText.Trim()))
				{
					co.Add(getCourseGroup(found, n, ref i, credits));
				}
				else
				{
					co.Add(getCourse(n, credits));
				}
			}
			--i;
			return co;
		}
		public static CourseOption getOrCourses(List<HtmlNode> found, ref int i, string credits) {
			CourseOption selectOr = new CourseOption("Select one course from the following:", credits);
			HtmlNode n = found[i];
			selectOr.Add(getCourse(n, credits));
			while ((n = found[++i]).HasClass("orclass")) {
				string orCrs = n.FirstChild.InnerText;
				int orIndex = orCrs.IndexOf("or") + 2;
				orCrs = orCrs.Substring(orIndex, orCrs.Length - orIndex).Trim();
				string[] crsParts = orCrs.Split();
				selectOr.Add(crsParts[0], crsParts[1], "", false, false, "");
			}
			--i;
			return selectOr;
		}
	}
}
