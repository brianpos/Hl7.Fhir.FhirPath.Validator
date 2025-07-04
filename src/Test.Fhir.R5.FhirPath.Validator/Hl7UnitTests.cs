using Hl7.Fhir.ElementModel;
using Hl7.Fhir.FhirPath;
using Hl7.Fhir.FhirPath.Validator;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Utility;
using Hl7.FhirPath;
using Hl7.FhirPath.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Xml.Serialization;
using Test.Fhir.R4B.FhirPath.Validator;
using Test.Fhir.R5.FhirPath.Validator;

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
			public string testDescription;
			public string resourceType;
			public string expression;
			public bool? isPredicate;
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
								testDescription = t.Description,
								resourceType = r.TypeName,
								expression = t.Expression.Text,
								isPredicate = t.Predicate,
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
					for (int n = 0; n < testData.outputs.Count; n++)
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
			catch (ArgumentException ex)
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
			if (expectedItem.Type == "Quantity")
			{
				var expectedQ = Hl7.Fhir.ElementModel.Types.Quantity.Parse(expectedItem.Text);
				var actualQ = actualItem.ParseQuantity().ToQuantity();
				return expectedQ.CompareTo(actualQ) == 0;
			}
			return expectedItem.Text == actualItem.Value?.ToString();
		}

		public void RecordResult(string engineName, string groupName, string testName, string testDescription, string expression, bool testPass, string failureMessage = null)
		{
			// Serialize the results 
			string fileName = Path.Combine(@"C:\git\Production\fhirpath-lab\static\results", $"{engineName.Replace("(", "").Replace(")", "")}.json");
			TestCaseResultOutputFile results = new TestCaseResultOutputFile();
			results.EngineName = engineName;
			string jsonContent = null;
			if (File.Exists(fileName))
			{
				jsonContent = File.ReadAllText(fileName);
				results = System.Text.Json.JsonSerializer.Deserialize<TestCaseResultOutputFile>(jsonContent);
			}

			var group = results.Groups.FirstOrDefault(g => g.Name == groupName);
			if (group == null)
			{
				group = new GroupOutput() { Name = groupName };
				results.Groups.Add(group);
			}
			var testCase = group.TestCases.FirstOrDefault(t => t.Name == testName);
			if (testCase == null)
			{
				testCase = new TestCaseOutput() { Name = testName, Description = testDescription, Expression = expression };
				group.TestCases.Add(testCase);
			}
			testCase.Description = testDescription;
			testCase.Expression = expression;
			testCase.FailureMessage = failureMessage;
			if (failureMessage?.Contains("Not implemented") == true || failureMessage?.Contains("Unhandled function '") == true)
			{
				testCase.NotImplemented = true;
				testCase.Result = null;
			}
			else
			{
				testCase.NotImplemented = null;
				testCase.Result = testPass;
			}

			// write the json test file out
			var json = System.Text.Json.JsonSerializer.Serialize(results,
				new JsonSerializerOptions()
				{
					WriteIndented = true,
					DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
					Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
				});

			// only write the output if it is different
			if (jsonContent != json)
			{
				File.WriteAllText(fileName, json);
			}
		}

		class CustomSerializer : IFhirSerializationEngine
		{
			public Resource DeserializeFromJson(string data)
			{
				try
				{
					var s = new FhirJsonPocoDeserializer();
					return s.DeserializeResource(data);
				}
				catch (DeserializationFailedException ex)
				{
					Console.WriteLine($"Error deserializing JSON: {ex.Message}, proceeding with partial if available");
					if (ex.PartialResult != null)
						return ex.PartialResult as Resource;

					Console.WriteLine($"Error deserializing JSON: {ex.Message}");
					throw new NotImplementedException("Deserialization from JSON is not implemented in this test.");
				}
			}

			public Resource DeserializeFromXml(string data)
			{
				throw new NotImplementedException();
			}

			public string SerializeToJson(Resource instance)
			{
				return instance.ToJson();
			}

			public string SerializeToXml(Resource instance)
			{
				return instance.ToXml();
			}
		}

		public record ServerDetails
		{
			public ServerDetails(string engineName, string serverUrl, string opName)
			{
				EngineName = engineName;
				ServerUrl = serverUrl;
				OperationName = opName;
			}
			public string EngineName { get; init; }
			public string ServerUrl { get; init; }
			public string OperationName { get; init; }
		}

		public static IEnumerable<ServerDetails> servers = new List<ServerDetails>
		{
			new ServerDetails("Firely-5.11.4 (R5)", "https://fhirpath-lab-dotnet2.azurewebsites.net/api", "fhirpath-r5"),
			new ServerDetails("fhirpath.js-4.4.0 (r5)", "http://localhost:3000/api", "fhirpath-r5"),
			new ServerDetails("Java 6.5.27 (R5)", "https://fhirpath-lab-java-g5c4bfdrb8ejamar.australiaeast-01.azurewebsites.net/fhir5", "fhirpath-r5"),
			new ServerDetails("fhirpath-py 1.0.3", "https://fhirpath.emr.beda.software/fhir", "fhirpath"),
			new ServerDetails("Aidbox (R5)", "https://fhir-validator.aidbox.app/r5", ""),
		};

		public static IEnumerable<object[]> TestDataKeysForServers
		{
			get
			{
				var allTestData = TestDataKeys.ToList();
				foreach (var sd in servers)
				{
					foreach (var testData in allTestData)
					{
						yield return new object[] { sd.EngineName, testData[0], testData[1] };
					}
				}
			}
		}

		[TestMethod]
		[DynamicData(nameof(TestDataKeysForServers))]
		public void TestEvaluateOnServer(string engineName, string groupName, string testName)
		{
			var serverDetails = servers.FirstOrDefault(s => s.EngineName == engineName);
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

			// Call the FhirPath Lab compatible server call

			// var engineName = "fhirpath.js-4.4.0 (r5)";
			// var serverUrl = "http://localhost:3000/api"; // Fhirpath.js engine

			// var engineName = "Firely-5.11.4 (R5)";
			// var serverUrl = "https://fhirpath-lab-dotnet2.azurewebsites.net/api"; // Firely engine

			// var engineName = "Java 6.5.27 (R5)";
			// var serverUrl = "https://fhirpath-lab-java-g5c4bfdrb8ejamar.australiaeast-01.azurewebsites.net/fhir5"; // HAPI engine
			// var serverUrl = "http://localhost:7071/api";

			// var serverUrl = "https://fhir-validator.aidbox.app/r5"; // Aidbox engine

			// var engineName = "fhirpath-py 1.0.3";
			// var serverUrl = "https://fhirpath.emr.beda.software/fhir"; // Python engine

			FhirClient server = new FhirClient(serverDetails.ServerUrl,
				new FhirClientSettings()
				{
					SerializationEngine = new CustomSerializer(),
					VerifyFhirVersion = false,
					PreferredFormat = ResourceFormat.Json,
					ParserSettings = new ParserSettings()
					{
						AcceptUnknownMembers = true,
						PermissiveParsing = true,
						ExceptionHandler = (object source, ExceptionNotification args) =>
						{
							Console.WriteLine($"Error: {args.Message}");
						}
					}
				});

			var parameters = new Parameters();
			parameters.Parameter.Add(new Parameters.ParameterComponent()
			{
				Name = "expression",
				Value = new FhirString(testData.expression)
			});
			parameters.Parameter.Add(new Parameters.ParameterComponent()
			{
				Name = "validate",
				Value = new FhirBoolean(true)
			});
			parameters.Parameter.Add(new Parameters.ParameterComponent()
			{
				Name = "variables"
			});
			parameters.Parameter.Add(new Parameters.ParameterComponent()
			{
				Name = "resource",
				Resource = testData.resource
			});
			List<ITypedElement> results = new List<ITypedElement>();
			try
			{
				var result = server.WholeSystemOperation(serverDetails.OperationName, parameters);

				if (result is Parameters resultParams)
				{
					var partParams = resultParams.Parameter.FirstOrDefault(p => p.Name == "parameters");
					var partEngine = partParams?.Part.FirstOrDefault(p => p.Name == "evaluator")?.Value?.ToString();
					if (!string.IsNullOrEmpty(partEngine))
					{
						engineName = partEngine;
					}
					var partResults = resultParams.Parameter.Where(p => p.Name == "result").ToList();

					foreach (var pr in partResults)
					{
						Console.WriteLine($"Result: {pr.Value}");
						foreach (var part in pr.Part)
						{
							if (part.Name == "trace")
								continue;
							Console.WriteLine($" Part Value: {part.Value} ({part.Name}) ({NormalizeTypeName(part.Value.TypeName)})");
							var resultItem = part.Value.ToTypedElement();
							var normalizedTypeName = NormalizeTypeName(part.Name);
							if (resultItem.InstanceType != normalizedTypeName && resultItem.InstanceType == "string")
							{
								// normalize the value
								if (normalizedTypeName == "integer")
									resultItem = Hl7.Fhir.ElementModel.ElementNode.ForPrimitive(int.Parse(part.Value.ToString()));
								if (normalizedTypeName == "boolean")
									resultItem = Hl7.Fhir.ElementModel.ElementNode.ForPrimitive(bool.Parse(part.Value.ToString()));
							}
							results.Add(resultItem);
						}
					}
				}

				if (result is OperationOutcome outcome)
				{
					// an error occurred, check that is expected.
					var errMessage = string.Join("\n", outcome.Issue?.Select(i => $"{i.Severity} ({i.Code}) {i.Details?.Text}"));
					if (testData.expressionValid != null)
						RecordResult(engineName, groupName, testName, testData.testDescription, testData.expression, true);
					else
						RecordResult(engineName, groupName, testName, testData.testDescription, testData.expression, false, errMessage);
					Assert.Inconclusive(errMessage);
					return;
				}

				// Console.WriteLine("Returned results:" + result.ToJson(new FhirJsonSerializationSettings() { Pretty = true }));

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
					if (testData.outputs.Count != results.Count())
						RecordResult(engineName, groupName, testName, testData.testDescription, testData.expression, false, $"Expected {testData.outputs.Count} results, got {results.Count()}");
					Assert.AreEqual(testData.outputs.Count, results.Count(), $"Expected {testData.outputs.Count} results, got {results.Count()}");

					// verify the values too
					int missMatchingResults = 0;
					for (int n = 0; n < testData.outputs.Count; n++)
					{
						var expectedItem = testData.outputs[n];
						var actualItem = results.ElementAt(n);
						// Check type
						if (expectedItem.Type == NormalizeTypeName(actualItem.InstanceType)
							// JS engine can;t differentiate between a decimal that is an integer
							|| (expectedItem.Type == "decimal" && actualItem.InstanceType == "integer"))
						{
							// Check the data too
							if (!DataEquals(expectedItem, actualItem))
							{
								missMatchingResults++;
								Console.WriteLine($"Mismatch at index {n}: expected value '{expectedItem.Text}', got '{actualItem.Value}'");
							}
						}
						else
						{
							missMatchingResults++;
							Console.WriteLine($"Mismatch at index {n}: expected type {expectedItem.Type}, got {actualItem.InstanceType}");
						}
					}
					if (missMatchingResults != 0)
						RecordResult(engineName, groupName, testName, testData.testDescription, testData.expression, false, $"Some results didn't match expected values ({missMatchingResults} values)");
					else
						RecordResult(engineName, groupName, testName, testData.testDescription, testData.expression, true);
					Assert.AreEqual(0, missMatchingResults, "Some results didn't match expected values");
				}
				else
				{
					if (results.Any())
						RecordResult(engineName, groupName, testName, testData.testDescription, testData.expression, false, "Expected no results, but got some.");
					else
						RecordResult(engineName, groupName, testName, testData.testDescription, testData.expression, true);
					Assert.IsFalse(results.Any(), "Expected no results, but got some.");
				}
			}
			catch (FhirOperationException ex)
			{
				var errMessage = ex.Message;
				if (ex.Outcome != null)
				{
					errMessage = string.Join("\n", ex.Outcome.Issue?.Select(i => $"{i.Severity} ({i.Code}) {i.Details?.Text}{(!string.IsNullOrEmpty(i.Diagnostics) ? $"\n   - {i.Diagnostics}" : "")}"));
				}
				if (testData.expressionValid == "semantic" || testData.expressionValid == "execution" || testData.expressionValid == "syntax")
					RecordResult(engineName, groupName, testName, testData.testDescription, testData.expression, true);
				else
					RecordResult(engineName, groupName, testName, testData.testDescription, testData.expression, false, errMessage);
				Assert.IsTrue(testData.expressionValid == "semantic" || testData.expressionValid == "execution", errMessage);
			}
			catch (InvalidCastException ex)
			{
				RecordResult(engineName, groupName, testName, testData.testDescription, testData.expression, false, ex.Message);
				Assert.Fail(ex.Message);
			}
		}

	}
}