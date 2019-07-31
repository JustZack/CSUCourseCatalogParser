using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace ProgramScraper.New
{
	class Year : Entry
	{
		public Year(HtmlNode row) : base(row) {
			this.isYear = true;
			this.Name = row.FirstChild.InnerText.Trim();
		}
	}
}
