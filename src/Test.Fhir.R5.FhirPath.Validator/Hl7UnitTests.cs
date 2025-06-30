using Hl7.Fhir.FhirPath.Validator;
using Hl7.FhirPath.Expressions;
using Hl7.FhirPath;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Model;
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Xml.Serialization;
using Test.Fhir.R4B.FhirPath.Validator;
using System.Collections.Generic;
using System.Linq;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.FhirPath;

namespace Test.Fhir.FhirPath.Validator
{
	/// <summary>
	/// Run all the tests from here (R4B) https://github.com/FHIR/fhir-test-cases
	/// </summary>
	[TestClass]
	public class Hl7UnitTestFileR5
	{
		FhirPathCompiler _compiler;
		static Dictionary<string, TestData> _testData;
		static Dictionary<string, Resource> _resources = new Dictionary<string, Resource>();

		[TestInitialize]
		public void Init()
		{
			Hl7.Fhir.FhirPath.ElementNavFhirExtensions.PrepareFhirSymbolTableFunctions();
			SymbolTable symbolTable = new(FhirPathCompiler.DefaultSymbolTable);
			_compiler = new FhirPathCompiler(symbolTable);

			TestDataKeys.Count(); // force the test data to be loaded
		}

		public class TestData
		{
			public string groupName;
			public string testName;
			public string resourceType;
			public string expression;
			public string expressionValid;
			public string outputType;
			public bool emptyOutput;
			public Resource resource;
			public List<Output> outputs;
		}

		public static IEnumerable<object[]> TestDataKeys
		{
			get
			{
				if (_testData == null)
				{
					_testData = new Dictionary<string, TestData>();
					foreach (var inputData in ExpressionsInTests)
					{
						_testData.Add($"{inputData.groupName}.{inputData.testName}", inputData);
					}
				}
				foreach (var key in _testData.Keys)
				{
					var data = _testData[key];
					yield return new object[] { data.groupName, data.testName };
				}
			}
		}

		public static IEnumerable<TestData> ExpressionsInTests
		{
			get
			{
				var result = new List<TestData>();
				string testFileXml = @"c:\git\hl7\fhir-test-cases\r5\fhirpath\tests-fhir-r5.xml";
				string testBasePath = @"c:\git\hl7\fhir-test-cases\r5\";
				XmlSerializer serializer = new XmlSerializer(typeof(Tests));
				var jsonSettings = new FhirJsonPocoDeserializerSettings() { Validator = null, AnnotateResourceParseExceptions = true, ValidateOnFailedParse = false };
				var jsonDS = new FhirJsonPocoDeserializer(jsonSettings);
				var xmlSettings = new FhirXmlPocoDeserializerSettings() { Validator = null, AnnotateResourceParseExceptions = true, ValidateOnFailedParse = false };
				var xmlDS = new FhirXmlPocoDeserializer(xmlSettings);

				using (StreamReader reader = new StreamReader(testFileXml))
				{
					Tests tests = (Tests)serializer.Deserialize(reader);
					//Console.WriteLine($"Name: {tests.Name}");
					//Console.WriteLine($"{tests.Description}");
					//Console.WriteLine("-----------------------------------");

					// Now run through these tests
					foreach (var g in tests.Groups)
					{
						//Console.WriteLine($"Group: {g.Name} {g.Description}");
						//Console.WriteLine("-----------------------------------");
						foreach (var t in g.Tests)
						{
							if (t.Mode == "cda") 
								continue; // not testing CDA documents

							// Now parse in the test file
							string content = null;
							if (!string.IsNullOrEmpty(t.InputFile))
							{
								if (File.Exists(Path.Combine(testBasePath, t.InputFile)))
									content = System.IO.File.ReadAllText(Path.Combine(testBasePath, t.InputFile));
								else if (File.Exists(Path.Combine(testBasePath, "examples", t.InputFile)))
									content = System.IO.File.ReadAllText(Path.Combine(testBasePath, "examples", t.InputFile));
								else
									content = "??????";
							}
							else
								content = "<Patient xmlns=\"http://hl7.org/fhir\"><id value=\"pat1\"/></Patient>";
							Resource r = null;
							if (t.InputFile?.EndsWith("json") == true)
								r = jsonDS.DeserializeResource(content);
							else
								r = xmlDS.DeserializeResource(content);

							var expression = t.Expression.Text;
							//if (expression.Contains("//") || expression.Contains("/*") && expression.Contains("*/"))
							//	continue;

							// Perform the static analysis on the expression!
							result.Add(new TestData
							{
								groupName = g.Name,
								testName = t.Name,
								resourceType = r.TypeName,
								expression = t.Expression.Text,
								expressionValid = t.Expression.Invalid,
								emptyOutput = t.Outputs?.Count == 0,
								outputType = t.Outputs?.FirstOrDefault()?.Type,
								outputs = t.Outputs,
								resource = r
							});
						}
					}
				}
				return result;
			}
		}

		[TestMethod]
		[DynamicData(nameof(TestDataKeys))]
		public void CheckStaticReturnTypes(string groupName, string testName)
		{
			var testData = _testData[$"{groupName}.{testName}"];

			// string expression = "(software.empty() and implementation.empty()) or kind != 'requirements'";
			Console.WriteLine($"{groupName} - {testName}");
			Console.WriteLine($"Resource Type: {testData.resourceType}");
			Console.WriteLine($"Expression:\r\n{testData.expression}");
			Console.WriteLine("---------");
			var visitor = new FhirPathExpressionVisitor();
			visitor.SetContext(testData.resourceType);
			try
			{
				var pe = _compiler.Parse(testData.expression);
				var r = pe.Accept(visitor);
				Console.WriteLine($"Result: {r}");
				Console.WriteLine("---------");

				Console.WriteLine(visitor.ToString());
				Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
				if (testData.expressionValid == "semantic" || testData.expressionValid == "execution")
				{
					if (visitor.Outcome.Success)
					{
						Assert.IsFalse(r.Types.Any());
					}
					// Assert.IsFalse(visitor.Outcome.Success);
				}
				else
					Assert.IsTrue(visitor.Outcome.Success, $"expected: {testData.expressionValid}");

				if (string.IsNullOrEmpty(testData.expressionValid) && string.IsNullOrEmpty(testData.outputType) && !string.IsNullOrEmpty(r.ToString()))
				{
					if (!testData.emptyOutput)
					{
						Assert.Inconclusive($"Test did not have an output type defined, result returned {r}");
						Assert.AreEqual(testData.outputType, r.ToString());
					}
				}
			}
			catch (FormatException ex)
			{
				Assert.IsTrue(testData.expressionValid == "syntax", $"unexpected compilation error: {ex.Message}");
			}
		}

		[TestMethod]
		[DynamicData(nameof(TestDataKeys))]
		public void TestEvaluateExpression(string groupName, string testName)
		{
			var testData = _testData[$"{groupName}.{testName}"];

			// string expression = "(software.empty() and implementation.empty()) or kind != 'requirements'";
			Console.WriteLine($"{groupName} - {testName}");
			Console.WriteLine($"Resource Type: {testData.resourceType}");
			Console.WriteLine($"Expression:\r\n{testData.expression}");
			Console.WriteLine("---------");
			if (testData.outputs.Any())
			{
				Console.WriteLine("Expecting results:");
				foreach (var o in testData.outputs)
				{
					Console.WriteLine($"	{o.Text} ({o.Type})");
				}
				Console.WriteLine("---------");
			}

			try
			{
				var ce = _compiler.Compile(testData.expression);
				var r = testData.resource.ToTypedElement().ToScopedNode();
				IEnumerable<ITypedElement> results = ce(r, new FhirEvaluationContext());

				if (results.Any())
				{
					Console.WriteLine("Returned results:");
					foreach (var item in results)
					{
						Console.WriteLine($"	{item.Value} ({NormalizeTypeName(item.InstanceType)})");
					}
					Console.WriteLine("---------");
				}

				// Check the count of results is the same as the expected output
				if (testData.outputs != null && testData.outputs.Count > 0)
				{
					Assert.AreEqual(testData.outputs.Count, results.Count(), $"Expected {testData.outputs.Count} results, got {results.Count()}");

					// verify the values too
					int missMatchingResults = 0;
					for (int n=0; n< testData.outputs.Count; n++)
					{
						var expectedItem = testData.outputs[n];
						var actualItem = results.ElementAt(n);
						// Check type
						if (expectedItem.Type != NormalizeTypeName(actualItem.InstanceType))
						{
							missMatchingResults++;
							Console.WriteLine($"Mismatch at index {n}: expected type {expectedItem.Type}, got {actualItem.InstanceType}");
						}
						else
						{
							// Check the data too
							if (!DataEquals(expectedItem, actualItem))
							{
								missMatchingResults++;
								Console.WriteLine($"Mismatch at index {n}: expected value '{expectedItem.Text}', got '{actualItem.Value}'");
							}
						}
					}
					Assert.AreEqual(0, missMatchingResults, "Some results didn't match expected values");
				}
				else
				{
					Assert.IsFalse(results.Any(), "Expected no results, but got some.");
				}
			}
			catch (InvalidOperationException ex)
			{
				Assert.IsTrue(testData.expressionValid == "semantic" || testData.expressionValid == "execution", $"unexpected compilation error: {ex.Message}");
			}
			catch (FormatException ex)
			{
				Assert.IsTrue(testData.expressionValid == "syntax" || testData.expressionValid == "execution", $"unexpected compilation error: {ex.Message}");
			}
			catch(ArgumentException ex)
			{
				if (ex.Message.StartsWith("Unknown symbol '"))
				{
					Assert.Inconclusive(ex.Message);
				}
				else
				{
					Assert.Fail(ex.Message);
				}
			}
		}

		private string NormalizeTypeName(string typeName)
		{
			if (!typeName.Contains(".") && !typeName.EndsWith("."))
				return typeName;
			if (typeName == "System.Quantity")
				return "Quantity";
			return typeName.Substring(typeName.LastIndexOf('.') + 1, 1).ToLower() + typeName.Substring(typeName.LastIndexOf('.') + 2);
		}

		private bool DataEquals(Output expectedItem, ITypedElement actualItem)
		{
			if (expectedItem.Type == "boolean")
				return expectedItem.Text.ToLower() == actualItem.Value?.ToString().ToLower();
			if (expectedItem.Type == "date")
				return expectedItem.Text.ToLower().Substring(1) == actualItem.Value?.ToString().ToLower();
			if (expectedItem.Type == "dateTime")
			{
				var expectedDateTime = Hl7.Fhir.ElementModel.Types.DateTime.Parse(expectedItem.Text.Substring(1));
				var actualDateTime = Hl7.Fhir.ElementModel.Types.DateTime.Parse(actualItem.Value?.ToString());
				return expectedDateTime.Equals(actualDateTime);
			}
			return expectedItem.Text == actualItem.Value?.ToString();
		}
	}
}