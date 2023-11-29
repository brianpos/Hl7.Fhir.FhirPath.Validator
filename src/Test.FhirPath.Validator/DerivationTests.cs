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
	}
}