using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace ProgramScraper.New
{
	class SumCredits : Entry
	{
		public SumCredits(HtmlNode row) : base(row) {
			this.isSum = true;
			this.Credits = row.LastChild.InnerText.Trim();
			this.Name = row.InnerText.Trim();
			this.Name = this.Name.Remove(this.Name.Length - this.Credits.Length, this.Credits.Length);
		}
	}
}
