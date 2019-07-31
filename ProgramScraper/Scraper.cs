using System;
using System.Collections.Generic;
using System.Text;
using HtmlAgilityPack;

namespace CoursesScraper
{
	class Scraper
	{
		public Uri Link { get; }
		private HtmlWeb browser = new HtmlWeb();
		private HtmlDocument document = null;
		public Scraper(string url) {
			Link = new Uri(url);
		}

		public List<HtmlNode> query(string selector) {
			if (document == null) {
				try { document = browser.Load(Link); }
				catch (Exception) { document = null; }
			}
			List<HtmlNode> nodes = new List<HtmlNode>();
			if (document != null) {
				//Query DOM for selector
				HtmlNodeCollection found = document.DocumentNode.SelectNodes(selector);
				//Try to put contents into List (instead of just returning an HTMLNodeCollection)
				if (found != null && found.Count > 0) {
					foreach (HtmlNode node in found) {
						nodes.Add(node);
					}
				}
			}
			//Return the nodes matching the selector.
			return nodes;
		}

	}
}
