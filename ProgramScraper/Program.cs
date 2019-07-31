using System;
using System.Collections.Generic;
using HtmlAgilityPack;
using System.IO;
using Microsoft.VisualBasic.FileIO;
using System.Threading;
using System.Diagnostics;
using System.Text;
using CoursesScraper;

namespace ProgramScraper.New
{
	class Program
	{
		static List<CatalogProgram> programs = new List<CatalogProgram>();
		static List<string[]> unmatchedPrograms = new List<string[]>();
		static string DegreeFolderPath;
		//static DBInfo ProgramInfo = new DBInfo();
		static bool writeResults = true; static bool doThreading = true;
		static void Main(string[] args)
		{
			string path = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName;
			DegreeFolderPath = path + "\\DegreeData\\";
			generateProgramsData();
			MatchProgramsToCodes();
			if (writeResults)
				WriteResults();
		}

		public static void doOldProcessing() {/*
			Console.WriteLine("Starting...");
			Stopwatch t = new Stopwatch();
			t.Start();

			loadDBInformation();
			generateProgramsData();
			MatchProgramsToCodes();

			t.Stop();

			int mapCount = 0, reqCount = 0, codeSetCount = 0;
			string completionMap = CatalogProgram.CompletionCSVHeader, requirements = CatalogProgram.RequirementsCSVHeader;
			foreach (CatalogProgram cp in programs)
			{
				completionMap += cp.getCompletionCSV();
				requirements += cp.getRequirementsCSV();
				if (cp.HasCompletionMap)
					mapCount++;
				if (cp.HasRequirements)
					reqCount++;
				if (cp.Code != null)
					codeSetCount++;
			}

			Console.WriteLine("Took " + t.ElapsedMilliseconds / 1000 + " seconds to load program data");
			Console.WriteLine(mapCount + "/" + programs.Count + " programs with completion maps found");
			Console.WriteLine(reqCount + "/" + programs.Count + " programs with requirements found");
			Console.WriteLine(codeSetCount + "/" + Math.Max(mapCount, reqCount) + " programs have codes");
			Console.WriteLine(Math.Max(mapCount, reqCount) - codeSetCount + " programs need codes");

			if (writeResults)
			{
				using (StreamWriter sw = new StreamWriter(@"C:\Users\zackaryj\Desktop\Degree Data\CompletionMap.csv", false, System.Text.Encoding.Default))
				{
					sw.Write(completionMap);
				}
				using (StreamWriter sw = new StreamWriter(@"C:\Users\zackaryj\Desktop\Degree Data\Requirements.csv", false, System.Text.Encoding.Default))
				{
					sw.Write(requirements);
				}

				Console.WriteLine("Writing unmatched programs to csv...");
				using (StreamWriter sw = new StreamWriter(@"C:\Users\zackaryj\Desktop\Degree Data\Unmatchedprograms.csv", false, System.Text.Encoding.Default))
				{
					sw.WriteLine("\"\",\"\",\"Programs from the spreadsheet that didnt match any found online (by direct string compairson, so not perfect)\",");
					sw.WriteLine("Key, Code, Program");
					foreach (string[] row in unmatchedPrograms)
						sw.WriteLine("\"{0}\",\"{1}\",\"{2}\"", row[0], row[1], row[2]);
					sw.WriteLine(); sw.WriteLine(); sw.WriteLine(); sw.WriteLine();
					sw.WriteLine(",,Programs online that didnt match any in the spreadsheet");
					sw.WriteLine("Key, Code, Program, Title");
					foreach (CatalogProgram pro in programs)
						if (pro.Code == null && (pro.HasCompletionMap || pro.HasRequirements))
							sw.WriteLine("\"\",\"\",\"{0}\",\"{1}\",", pro.Program, pro.ProgramTitle);
				}
			}

			Console.WriteLine("Done");
		*/}

		//Generate an object for each program found on the 'Programs A-Z' list on the course catalog
		public static void generateProgramsData() {
			Console.WriteLine("Pulling programs from catalog.colostate.edu...");
			Scraper s = new Scraper("http://catalog.colostate.edu/general-catalog/programsaz/");
			List<HtmlNode> nodes = s.query("//table[@id=\"tbl_degrees\"]//tbody//tr");
			string[] data = new string[7]; int i = 0;

			//Build the list of CatalogPrograms
			foreach (HtmlNode p in nodes) { //For each row
				foreach (HtmlNode c in p.Descendants("td")) { //For each td in the row
					//Grab the value
					data[i++] = c.InnerText;
					//If there is a link in the cell, grab the link. (this is always the last cell, which links to the course requirements page.
					if (c.HasChildNodes && c.FirstChild.Name.Equals("a")) //If the first child is an anchor tag, grab the href attribute from it.
						foreach (HtmlAttribute attr in c.FirstChild.Attributes)
							if (attr.Name.Equals("href"))
								data[i++] = s.Link.Scheme + "://" + s.Link.Host + attr.Value;
				}
				//Add the parsed program to the list of programs
				programs.Add(new CatalogProgram(data, !doThreading));
				data = new string[7]; i = 0;
			}

			if (doThreading)
			{
				Thread[] loadthreads = new Thread[programs.Count];
				//Make a thread for loading each program
				for (i = 0; i < programs.Count; i++) loadthreads[i] = new Thread(new ThreadStart(programs[i].load));
				Console.WriteLine("Starting threads for loading program data...");
				//Start each thread
				foreach (Thread t in loadthreads) t.Start();
				Console.WriteLine("Waiting for {0} threads to finish execution...", loadthreads.Length);
				//Wait for each thread to terminate
				//for (i = 0; i < loadthreads.Length; i++) loadthreads[i].Join();
				foreach (Thread t in loadthreads) t.Join();
			}
		} 

		//Load information gathered from Tae's excel spreadsheet
		private static List<string[]> loadProgramData() {
			Console.WriteLine("Pulling program data from list of unique programs...");

			List<string[]> progData = new List<string[]>();

			using (TextFieldParser parser = new TextFieldParser(DegreeFolderPath + "programsUnique.csv")) {
				parser.HasFieldsEnclosedInQuotes = true;
				parser.SetDelimiters(",");
				parser.ReadFields(); //Read the header rows and discard them
				while (parser.PeekChars(1) != null) progData.Add(parser.ReadFields());
			}

			return progData;
		}
		private static string removeSpacesDotsCommasQuotes(string input) {
			return input.Trim().Replace(" ", "").Replace(".", "").Replace(",", "").Replace("'", "").Replace("\"","").ToLower();
		}
		public static void MatchProgramsToCodes() {
			List<string[]> uniqueProgs = loadProgramData();
			unmatchedPrograms = new List<string[]>(uniqueProgs);

			Console.WriteLine("Attempting to match program codes to the programs found on catalog.colostate.edu...");

			//Foreach major found in Tae's file
			foreach (string[] p in uniqueProgs) {
				//Remove all spaces, commas, and periods and make the string lower-case
				string actual = removeSpacesDotsCommasQuotes(p[2]);

				//foreach major found on the website
				foreach (CatalogProgram cp in programs) {
					//TODO: COMMENTED OUT FOR TESTING FIX THIS AT SOME POINT
					//if (cp.CompletionMap.Count == 0 && cp.Requirements.Count == 0) continue;
					//Remove all spaces, commas, and periods and make the string lower-case
					string program = removeSpacesDotsCommasQuotes(cp.ProgramTitle == string.Empty ? cp.Program : cp.ProgramTitle);

					if (actual == program) {
						unmatchedPrograms.Remove(p);
						cp.Key = p[0];
						cp.Code = p[1];
					}
				}
			}
		}

		public static void WriteResults() { 
			StringBuilder reqCSV = new StringBuilder(), compCSV  = new StringBuilder();
			int codeSetCount = 0, programsWithTablesCount = 0;

			reqCSV.Append(CatalogProgram.RequirementsCSVHeader);
			compCSV.Append(CatalogProgram.CompletionCSVHeader);
				
			foreach (CatalogProgram ncp in programs) {
				compCSV.Append(ncp.ToCSV(false));
				reqCSV.Append(ncp.ToCSV(true));

				if (ncp.Code != null) codeSetCount++;
				if (ncp.CourseTables.Count > 0) programsWithTablesCount++;
			}

			Console.WriteLine("{0}/{1} degrees programs have codes", codeSetCount, programsWithTablesCount);

			if (writeResults)
			{
				Console.Write("Writing Completion Map...");
				using (StreamWriter sw = new StreamWriter(DegreeFolderPath + "CompletionMap.csv", false, System.Text.Encoding.Default))
				{
					sw.Write(compCSV.ToString());
				}
				Console.WriteLine("Done");
				Console.Write("Writing Requirements...");
				using (StreamWriter sw = new StreamWriter(DegreeFolderPath + "Requirements.csv", false, System.Text.Encoding.Default))
				{
					sw.Write(reqCSV.ToString());
				}
				Console.WriteLine("Done");
				Console.Write("Writing unmatched programs to csv...");
				using (StreamWriter sw = new StreamWriter(DegreeFolderPath + "Unmatchedprograms.csv", false, System.Text.Encoding.Default))
				{
					sw.WriteLine("\"\",\"\",\"Programs from the spreadsheet that didnt match any found online (by direct string compairson, so not perfect)\",");
					sw.WriteLine("Key, Code, Program");
					foreach (string[] row in unmatchedPrograms)
						sw.WriteLine("\"{0}\",\"{1}\",\"{2}\"", row[0], row[1], row[2]);
					sw.WriteLine(); sw.WriteLine(); sw.WriteLine(); sw.WriteLine();
					sw.WriteLine(",,Programs online that didnt match any in the spreadsheet");
					sw.WriteLine("Key, Code, Program, Title");
					foreach (New.CatalogProgram pro in programs)
						if (pro.Code == null && (pro.CourseTables.Count > 0))
							sw.WriteLine("\"\",\"\",\"{0}\",\"{1}\",", pro.Program, pro.ProgramTitle);
				}
				Console.WriteLine("Done");
			}
		}

		//Load information gathered from the database
		/*
		public static void loadDBInformation() {
			string basePath = @"C:\Users\zackaryj\Desktop\Degree Data\";
			string[] codeFiles = { "Levels.csv", "Colleges.csv", "Degrees.csv", "Majors.csv", "Minors.csv", "Concentrations.csv", "Programs.csv" };
			LoadList(basePath + codeFiles[0], ProgramInfo.Levels);
			LoadList(basePath + codeFiles[1], ProgramInfo.Colleges);
			LoadList(basePath + codeFiles[2], ProgramInfo.Degrees);
			LoadList(basePath + codeFiles[3], ProgramInfo.Majors);
			LoadList(basePath + codeFiles[4], ProgramInfo.Minors);
			LoadList(basePath + codeFiles[5], ProgramInfo.Concentrations);
			LoadList(basePath + codeFiles[6], ProgramInfo.Programs);
		}
		public static void LoadList(string path, List<CodeDescriptions> dataset) {
			using (TextFieldParser parser = new TextFieldParser(path))
			{
				parser.HasFieldsEnclosedInQuotes = true;
				parser.SetDelimiters(",");

				//Read the headers and discard them
				parser.ReadFields();
				while (parser.PeekChars(1) != null)
					dataset.Add(new CodeDescriptions(parser.ReadFields()));
			}
		}
		public static void LoadList(string path, List<ProgramDescriptions> dataset)
		{
			using (TextFieldParser parser = new TextFieldParser(path))
			{
				parser.HasFieldsEnclosedInQuotes = true;
				parser.SetDelimiters(",");

				//Read the headers and discard them
				parser.ReadFields();
				while (parser.PeekChars(1) != null)
					dataset.Add(new ProgramDescriptions(parser.ReadFields()));
			}
		}
	*/}
}