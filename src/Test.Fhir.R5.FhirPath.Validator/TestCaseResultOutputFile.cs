using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test.Fhir.R5.FhirPath.Validator
{
	internal class TestCaseResultOutputFile
	{
		public string EngineName { get; set; }
		public List<GroupOutput> Groups { get; set; } = new List<GroupOutput>();
	}

	public class GroupOutput
	{
		public string Name { get; set; }
		public List<TestCaseOutput> TestCases { get; set; } = new List<TestCaseOutput>();
	}

	public class TestCaseOutput
	{
		public string Name { get; set; }
		public string Description { get; set; }
		public string Expression { get; set; }

		public bool? Result { get; set; }
		public bool? NotImplemented { get; set; }
		public string FailureMessage { get; set; }
	}
}
