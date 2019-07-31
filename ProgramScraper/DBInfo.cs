using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoursesScraper
{
	class DBInfo
	{
		public List<CodeDescriptions> Levels { get; } = new List<CodeDescriptions>();
		public List<CodeDescriptions> Colleges { get; } = new List<CodeDescriptions>();
		public List<CodeDescriptions> Degrees { get; } = new List<CodeDescriptions>();
		public List<CodeDescriptions> Majors { get; } = new List<CodeDescriptions>();
		public List<CodeDescriptions> Minors { get; } = new List<CodeDescriptions>();
		public List<CodeDescriptions> Concentrations { get; } = new List<CodeDescriptions>();
		public List<ProgramDescriptions> Programs { get; } = new List<ProgramDescriptions>();

		public List<CodeDescriptions> GetField(string name)
		{
			return (List<CodeDescriptions>)this.GetType().GetField(name).GetValue(this);
		}
	}
}
