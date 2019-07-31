using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace ProgramScraper.New
{
	class Term : Entry
	{
		public Term(HtmlNode row) : base(row) {
			this.isTerm = true;
			this.Name = row.FirstChild.InnerText.Trim();
		}
	}
}
