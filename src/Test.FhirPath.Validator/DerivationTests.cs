using Hl7.Fhir.Introspection;
using Hl7.FhirPath.Expressions;
using Hl7.FhirPath;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Model;
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection.Metadata;
using Hl7.Fhir.FhirPath.Validator;

namespace Test.Fhir.FhirPath.Validator
{

	[TestClass]
    public class DerivationTests
    {
        FhirPathCompiler _compiler;

        [TestInitialize]
        public void Init()
        {
            Hl7.Fhir.FhirPath.ElementNavFhirExtensions.PrepareFhirSymbolTableFunctions();
            SymbolTable symbolTable = new (FhirPathCompiler.DefaultSymbolTable);
            _compiler = new FhirPathCompiler(symbolTable);
        }

		[TestMethod]
		public void TestExtensionShortcut()
		{
			string expression = "extension('http://hl7.org/fhir/StructureDefinition/patient-mothersMaidenName').value";
			Console.WriteLine(expression);
			var visitor = new FhirPathExpressionVisitor();
			visitor.AddInputType(typeof(Patient));
			var pe = _compiler.Parse(expression);
			Console.WriteLine("---------");
			var r = pe.Accept(visitor);
			Console.WriteLine(visitor.ToString());
			Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
			Assert.AreEqual("string", r.ToString());
			Assert.IsTrue(visitor.Outcome.Success);
		}

		[TestMethod]
		public void TestExtensionShortcutOnCollection()
		{
			string expression = "name.extension('http://hl7.org/fhir/StructureDefinition/patient-mothersMaidenName').value";
			Console.WriteLine(expression);
			var visitor = new FhirPathExpressionVisitor();
			visitor.AddInputType(typeof(Patient));
			var pe = _compiler.Parse(expression);
			Console.WriteLine("---------");
			var r = pe.Accept(visitor);
			Console.WriteLine(visitor.ToString());
			Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
			Assert.AreEqual("string[]", r.ToString());
			Assert.IsTrue(visitor.Outcome.Success);
		}

		[TestMethod]
		public void TestExtensionViaWhere()
		{
			string expression = "extension.where(url='http://hl7.org/fhir/StructureDefinition/patient-mothersMaidenName').value";
			Console.WriteLine(expression);
			var visitor = new FhirPathExpressionVisitor();
			visitor.AddInputType(typeof(Patient));
			var pe = _compiler.Parse(expression);
			Console.WriteLine("---------");
			var r = pe.Accept(visitor);
			Console.WriteLine(visitor.ToString());
			Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
			Assert.AreEqual("string", r.ToString());
			Assert.IsTrue(visitor.Outcome.Success);
		}

		[TestMethod]
		public void TestExtensionViaWhereReverse()
		{
			string expression = "extension.where('http://hl7.org/fhir/StructureDefinition/patient-mothersMaidenName'=url).value";
			Console.WriteLine(expression);
			var visitor = new FhirPathExpressionVisitor();
			visitor.AddInputType(typeof(Patient));
			var pe = _compiler.Parse(expression);
			Console.WriteLine("---------");
			var r = pe.Accept(visitor);
			Console.WriteLine(visitor.ToString());
			Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
			Assert.AreEqual("string", r.ToString());
			Assert.IsTrue(visitor.Outcome.Success);
		}

		[TestMethod]
		public void TestExtensionViaWhereOnCollection()
		{
			string expression = "name.extension.where(url='http://hl7.org/fhir/StructureDefinition/patient-mothersMaidenName').value";
			Console.WriteLine(expression);
			var visitor = new FhirPathExpressionVisitor();
			visitor.AddInputType(typeof(Patient));
			var pe = _compiler.Parse(expression);
			Console.WriteLine("---------");
			var r = pe.Accept(visitor);
			Console.WriteLine(visitor.ToString());
			Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
			Assert.AreEqual("string[]", r.ToString());
			Assert.IsTrue(visitor.Outcome.Success);
		}

		[TestMethod]
		public void TestExtensionViaWhereOnCollectionWithFirst()
		{
			string expression = "name.extension.where(url='http://hl7.org/fhir/StructureDefinition/patient-mothersMaidenName').first().value";
			Console.WriteLine(expression);
			var visitor = new FhirPathExpressionVisitor();
			visitor.AddInputType(typeof(Patient));
			var pe = _compiler.Parse(expression);
			Console.WriteLine("---------");
			var r = pe.Accept(visitor);
			Console.WriteLine(visitor.ToString());
			Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
			Assert.AreEqual("string", r.ToString());
			Assert.IsTrue(visitor.Outcome.Success);
		}

		[TestMethod]
		public void TestExtensionComplexChild_Constraint()
		{
			string expression = "extension('http://hl7.org/fhir/StructureDefinition/questionnaire-constraint').extension('expression').value";
			Console.WriteLine(expression);
			var visitor = new FhirPathExpressionVisitor();
			visitor.AddInputType(typeof(Questionnaire));
			var pe = _compiler.Parse(expression);
			Console.WriteLine("---------");
			var r = pe.Accept(visitor);
			Console.WriteLine(visitor.ToString());
			Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
			Assert.AreEqual("string[]", r.ToString());
			Assert.IsTrue(visitor.Outcome.Success);
		}

		[TestMethod]
		public void TestExtensionComplexChild_Constraint2()
		{
			string expression = "extension.where(url='http://hl7.org/fhir/StructureDefinition/questionnaire-constraint').extension('expression').value";
			Console.WriteLine(expression);
			var visitor = new FhirPathExpressionVisitor();
			visitor.AddInputType(typeof(Questionnaire));
			var pe = _compiler.Parse(expression);
			Console.WriteLine("---------");
			var r = pe.Accept(visitor);
			Console.WriteLine(visitor.ToString());
			Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
			Assert.AreEqual("string[]", r.ToString());
			Assert.IsTrue(visitor.Outcome.Success);
		}

		[TestMethod]
		public void TestExtensionComplexChild_Constraint3()
		{
			string expression = "extension('http://hl7.org/fhir/StructureDefinition/questionnaire-constraint').extension.where(url='expression').value";
			Console.WriteLine(expression);
			var visitor = new FhirPathExpressionVisitor();
			visitor.AddInputType(typeof(Questionnaire));
			var pe = _compiler.Parse(expression);
			Console.WriteLine("---------");
			var r = pe.Accept(visitor);
			Console.WriteLine(visitor.ToString());
			Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
			Assert.AreEqual("string[]", r.ToString());
			Assert.IsTrue(visitor.Outcome.Success);
		}

		[TestMethod]
		public void TestExtensionComplexChild_Constraint4()
		{
			string expression = "extension.where(url='http://hl7.org/fhir/StructureDefinition/questionnaire-constraint').extension.where(url='expression').value";
			Console.WriteLine(expression);
			var visitor = new FhirPathExpressionVisitor();
			visitor.AddInputType(typeof(Questionnaire));
			var pe = _compiler.Parse(expression);
			Console.WriteLine("---------");
			var r = pe.Accept(visitor);
			Console.WriteLine(visitor.ToString());
			Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
			Assert.AreEqual("string[]", r.ToString());
			Assert.IsTrue(visitor.Outcome.Success);
		}

		[TestMethod]
		public void TestExtensionComplexChild_Single()
		{
			string expression = "extension('http://hl7.org/fhir/StructureDefinition/geolocation').extension('latitude').value";
			Console.WriteLine(expression);
			var visitor = new FhirPathExpressionVisitor();
			visitor.AddInputType(typeof(Location));
			var pe = _compiler.Parse(expression);
			Console.WriteLine("---------");
			var r = pe.Accept(visitor);
			Console.WriteLine(visitor.ToString());
			Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
			Assert.AreEqual("decimal", r.ToString());
			Assert.IsTrue(visitor.Outcome.Success);
		}

		[TestMethod]
		public void TestExtensionComplexChild_Single2()
		{
			string expression = "extension.where(url='http://hl7.org/fhir/StructureDefinition/geolocation').extension('latitude').value";
			Console.WriteLine(expression);
			var visitor = new FhirPathExpressionVisitor();
			visitor.AddInputType(typeof(Location));
			var pe = _compiler.Parse(expression);
			Console.WriteLine("---------");
			var r = pe.Accept(visitor);
			Console.WriteLine(visitor.ToString());
			Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
			Assert.AreEqual("decimal", r.ToString());
			Assert.IsTrue(visitor.Outcome.Success);
		}

		[TestMethod]
		public void TestExtensionComplexChild_Single3()
		{
			string expression = "extension('http://hl7.org/fhir/StructureDefinition/geolocation').extension.where(url='latitude').value";
			Console.WriteLine(expression);
			var visitor = new FhirPathExpressionVisitor();
			visitor.AddInputType(typeof(Location));
			var pe = _compiler.Parse(expression);
			Console.WriteLine("---------");
			var r = pe.Accept(visitor);
			Console.WriteLine(visitor.ToString());
			Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
			Assert.AreEqual("decimal", r.ToString());
			Assert.IsTrue(visitor.Outcome.Success);
		}

		[TestMethod]
		public void TestExtensionComplexChild_Single4()
		{
			string expression = "extension.where(url='http://hl7.org/fhir/StructureDefinition/geolocation').extension.where(url='latitude').value";
			Console.WriteLine(expression);
			var visitor = new FhirPathExpressionVisitor();
			visitor.AddInputType(typeof(Location));
			var pe = _compiler.Parse(expression);
			Console.WriteLine("---------");
			var r = pe.Accept(visitor);
			Console.WriteLine(visitor.ToString());
			Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
			Assert.AreEqual("decimal", r.ToString());
			Assert.IsTrue(visitor.Outcome.Success);
		}

		[TestMethod]
		public void TestExtensionUnknown()
		{
			string expression = "extension('http://hl7.org/fhir/StructureDefinition/this-doesnt-exist').value";
			Console.WriteLine(expression);
			var visitor = new FhirPathExpressionVisitor();
			visitor.AddInputType(typeof(Location));
			var pe = _compiler.Parse(expression);
			Console.WriteLine("---------");
			var r = pe.Accept(visitor);
			Console.WriteLine(visitor.ToString());
			Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
			Assert.AreEqual("Address[], Age[], Annotation[], Attachment[], base64Binary[], boolean[], canonical[], code[], CodeableConcept[], Coding[], ContactDetail[], ContactPoint[], Contributor[], Count[], DataRequirement[], date[], dateTime[], decimal[], Distance[], Dosage[], Duration[], Expression[], HumanName[], id[], Identifier[], instant[], integer[], markdown[], Meta[], Money[], oid[], ParameterDefinition[], Period[], positiveInt[], Quantity[], Range[], Ratio[], Reference[], RelatedArtifact[], SampledData[], Signature[], string[], time[], Timing[], TriggerDefinition[], unsignedInt[], uri[], url[], UsageContext[], uuid[]", r.ToString());
			Assert.IsTrue(visitor.Outcome.Success);
			Assert.AreEqual(1, visitor.Outcome.Issue.Count, "expected to find the informational message on the failed resolve");
			Assert.AreEqual(OperationOutcome.IssueSeverity.Information, visitor.Outcome.Issue[0].Severity);
			Assert.AreEqual(OperationOutcome.IssueType.NotFound, visitor.Outcome.Issue[0].Code);
		}
	}
}