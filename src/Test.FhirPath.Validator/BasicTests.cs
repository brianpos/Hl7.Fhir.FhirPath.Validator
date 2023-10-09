using Hl7.Fhir.FhirPath.Validator;
using Hl7.Fhir.Introspection;
using Hl7.FhirPath.Expressions;
using Hl7.FhirPath;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Model;
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test.Fhir.FhirPath.Validator
{
    [TestClass]
    public class BasicTests
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
        public void TestMethodWhere()
        {
            string expression = "contact.telecom.where(use='phone').system";
            Console.WriteLine(expression);
            var visitor = new FhirPathExpressionVisitor();
            visitor.AddInputType(typeof(Patient));
            var pe = _compiler.Parse(expression);
            Console.WriteLine("---------");
            pe.Accept(visitor);
            Console.WriteLine(visitor.ToString());
            Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
            Assert.IsTrue(visitor.Outcome.Success);
        }

        [TestMethod]
        public void TestMethodWhereNonBooleanArguments()
        {
            string expression = "contact.telecom.where('phone').system";
            Console.WriteLine(expression);
            var visitor = new FhirPathExpressionVisitor();
            visitor.AddInputType(typeof(Patient));
            var pe = _compiler.Parse(expression);
            Console.WriteLine("---------");
            pe.Accept(visitor);
            Console.WriteLine(visitor.ToString());
            Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
            Assert.IsFalse(visitor.Outcome.Success);
        }

        [TestMethod]
        public void TestMethodWhereNonBooleanCollectionArguments()
        {
            string expression = "identifier.where(type.coding.code='MR').value.first()";
            Console.WriteLine(expression);
            var visitor = new FhirPathExpressionVisitor();
            visitor.AddInputType(typeof(Patient));
            var pe = _compiler.Parse(expression);
            Console.WriteLine("---------");
            pe.Accept(visitor);
            Console.WriteLine(visitor.ToString());
            Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
            Assert.AreEqual(1, visitor.Outcome.Warnings);
        }

        [TestMethod]
        public void TestMethodCombine()
        {
            string expression = "name.select(given.join(' ').combine(family).join(', '))";
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
        public void TestMethodUnion()
        {
            string expression = "name.select(given.join(' ').union(family).join(', '))";
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
        public void TestMethodRepeat()
        {
            string expression = "QuestionnaireResponse.repeat(item | answer)";
            Console.WriteLine(expression);
            var visitor = new FhirPathExpressionVisitor();
            visitor.AddInputType(typeof(QuestionnaireResponse));
            var pe = _compiler.Parse(expression);
            Console.WriteLine("---------");
            var r = pe.Accept(visitor);
            Console.WriteLine(visitor.ToString());
            Console.WriteLine($"Result Type: {r}");
            Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
            Assert.AreEqual("QuestionnaireResponse#Item[], QuestionnaireResponse#Answer[]", r.ToString());
            Assert.IsTrue(visitor.Outcome.Success);
        }

        [TestMethod]
        public void TestMethodRepeatFailure()
        {
            string expression = "QuestionnaireResponse.repeat(item | answers)";
            Console.WriteLine(expression);
            var visitor = new FhirPathExpressionVisitor();
            visitor.AddInputType(typeof(QuestionnaireResponse));
            var pe = _compiler.Parse(expression);
            Console.WriteLine("---------");
            var r = pe.Accept(visitor);
            Console.WriteLine(visitor.ToString());
            Console.WriteLine($"Result Type: {r}");
            Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
            Assert.AreEqual("QuestionnaireResponse#Item[]", r.ToString());
            Assert.IsFalse(visitor.Outcome.Success);
            Assert.AreEqual("prop 'answers' not found on QuestionnaireResponse", visitor.Outcome.Issue[0].Details.Text);
        }

        [TestMethod]
        public void TestVerticalPipeOperator()
        {
            string expression = "gender | birthDate";
            Console.WriteLine(expression);
            var visitor = new FhirPathExpressionVisitor();
            visitor.AddInputType(typeof(Patient));
            var pe = _compiler.Parse(expression);
            Console.WriteLine("---------");
            var r = pe.Accept(visitor);
            Console.WriteLine(visitor.ToString());
            Console.WriteLine($"Result Type: {r}");
            Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
            Assert.AreEqual("code, date", r.ToString());
            Assert.IsTrue(visitor.Outcome.Success);
        }

        [TestMethod]
        public void TestVerticalPipeOperator2()
        {
            string expression = "generalPractitioner.first() | managingOrganization";
            Console.WriteLine(expression);
            var visitor = new FhirPathExpressionVisitor();
            visitor.AddInputType(typeof(Patient));
            var pe = _compiler.Parse(expression);
            Console.WriteLine("---------");
            var r = pe.Accept(visitor);
            Console.WriteLine(visitor.ToString());
            Console.WriteLine($"Result Type: {r}");
            Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
            Assert.AreEqual("Reference[]", r.ToString());
            Assert.IsTrue(visitor.Outcome.Success);
        }

        [TestMethod]
        public void TestMethodInvalidProperty()
        {
            string expression = "meta.extension[0].value.line[0] | deceased | contact.name[2].select(family & '' & given.join(' ') & chicken) | name.given.first()";
            Console.WriteLine(expression);
            var visitor = new FhirPathExpressionVisitor();
            visitor.AddInputType(typeof(Patient));
            var pe = _compiler.Parse(expression);
            Console.WriteLine("---------");
            pe.Accept(visitor);
            Console.WriteLine(visitor.ToString());
            Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
            Assert.IsFalse(visitor.Outcome.Success, "Expected failure");
        }

        [TestMethod]
        public void TestMethodInvalidFunction()
        {
            string expression = "contact.name[2].garbageFunc(family & '' & given.join(' ') & chicken) | name.given.first()";
            Console.WriteLine(expression);
            var visitor = new FhirPathExpressionVisitor();
            visitor.AddInputType(typeof(Patient));
            var pe = _compiler.Parse(expression);
            Console.WriteLine("---------");
            pe.Accept(visitor);
            Console.WriteLine(visitor.ToString());
            Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
            Assert.IsFalse(visitor.Outcome.Success, "Expected failure");
        }

        [TestMethod]
        public void TestAsFunction()
        {
            string expression = "deceased as boolean";
            Console.WriteLine(expression);
            var visitor = new FhirPathExpressionVisitor();
            visitor.AddInputType(typeof(Patient));
            var pe = _compiler.Parse(expression);
            Console.WriteLine("---------");
            var r = pe.Accept(visitor);
            Console.WriteLine(visitor.ToString());
            Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
            Assert.IsTrue(visitor.Outcome.Success, "Expected success");
            Assert.AreEqual("boolean", r.ToString());
        }

        [TestMethod]
        public void TestStringFuncOnDate()
        {
            string expression = "birthDate.length()";
            Console.WriteLine(expression);
            var visitor = new FhirPathExpressionVisitor();
            visitor.AddInputType(typeof(Patient));
            var pe = _compiler.Parse(expression);
            Console.WriteLine("---------");
            var r = pe.Accept(visitor);
            Console.WriteLine(visitor.ToString());
            Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
            Assert.IsFalse(visitor.Outcome.Success, "Expected failure");
            Assert.AreEqual("integer", r.ToString());
        }

        [TestMethod]
        public void TestHighBoundarySymbolTable()
        {
            string expression = "gender.highBoundary()";
            Console.WriteLine(expression);
            var visitor = new FhirPathExpressionVisitor();
            visitor.AddInputType(typeof(Patient));
            var pe = _compiler.Parse(expression);
            Console.WriteLine("---------");
            var r = pe.Accept(visitor);
            Console.WriteLine(visitor.ToString());
            Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
            Assert.IsFalse(visitor.Outcome.Success, "Expected failure");
            Assert.AreEqual("code", r.ToString());
        }

        [TestMethod]
        public void TestLengthContext()
        {
            string expression = "id.length()";
            Console.WriteLine(expression);
            var visitor = new FhirPathExpressionVisitor();
            visitor.AddInputType(typeof(Patient));
            var pe = _compiler.Parse(expression);
            Console.WriteLine("---------");
            var r = pe.Accept(visitor);
            Console.WriteLine(visitor.ToString());
            Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
            Assert.IsTrue(visitor.Outcome.Success, "Expected success");
            Assert.AreEqual("integer", r.ToString());
        }

        [TestMethod]
        public void TestLengthContextCollection()
        {
            string expression = "name.family.length()";
            Console.WriteLine(expression);
            var visitor = new FhirPathExpressionVisitor();
            visitor.AddInputType(typeof(Patient));
            var pe = _compiler.Parse(expression);
            Console.WriteLine("---------");
            var r = pe.Accept(visitor);
            Console.WriteLine(visitor.ToString());
            Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
            Assert.IsFalse(visitor.Outcome.Success, "Expected failure");
            Assert.AreEqual("integer", r.ToString());
        }

        [TestMethod]
        public void TestLengthContext2()
        {
            string expression = "name.family.select(length())";
            Console.WriteLine(expression);
            var visitor = new FhirPathExpressionVisitor();
            visitor.AddInputType(typeof(Patient));
            var pe = _compiler.Parse(expression);
            Console.WriteLine("---------");
            var r = pe.Accept(visitor);
            Console.WriteLine(visitor.ToString());
            Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
            Assert.IsTrue(visitor.Outcome.Success, "Expected success");
            Assert.AreEqual("integer[]", r.ToString());
        }

        [TestMethod]
        public void TestStringFuncOnCanonical()
        {
            string expression = "questionnaire.length()";
            Console.WriteLine(expression);
            var visitor = new FhirPathExpressionVisitor();
            visitor.AddInputType(typeof(QuestionnaireResponse));
            var pe = _compiler.Parse(expression);
            Console.WriteLine("---------");
            var r = pe.Accept(visitor);
            Console.WriteLine(visitor.ToString());
            Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
            Assert.IsTrue(visitor.Outcome.Success, "Expected success");
            Assert.AreEqual("integer", r.ToString());
        }

        [TestMethod]
        public void TestAsOperator()
        {
            string expression = "Condition.abatement.as(Age)";
            Console.WriteLine(expression);
            var visitor = new FhirPathExpressionVisitor();
            visitor.AddInputType(typeof(Condition));
            var pe = _compiler.Parse(expression);
            Console.WriteLine("---------");
            var r = pe.Accept(visitor);
            Console.WriteLine(visitor.ToString());
            Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
            Assert.IsTrue(visitor.Outcome.Success, "Expected failure");
            Assert.AreEqual("Age", r.ToString());
        }

        [TestMethod]
        public void TestInOperator()
        {
            string expression = "gender in ('male' | 'female')";
            Console.WriteLine(expression);
            var visitor = new FhirPathExpressionVisitor();
            visitor.AddInputType(typeof(Patient));
            var pe = _compiler.Parse(expression);
            Console.WriteLine("---------");
            var r = pe.Accept(visitor);
            Console.WriteLine(visitor.ToString());
            Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
            Assert.IsTrue(visitor.Outcome.Success, "Expected failure");
            Assert.AreEqual("boolean", r.ToString());
        }

        [TestMethod]
        public void TestAsOperatorMultiple()
        {
            string expression = "(value as Quantity) | (value as SampledData)";
            Console.WriteLine(expression);
            var visitor = new FhirPathExpressionVisitor();
            visitor.AddInputType(typeof(Observation));
            var pe = _compiler.Parse(expression);
            Console.WriteLine("---------");
            var r = pe.Accept(visitor);
            Console.WriteLine(visitor.ToString());
            Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
            Assert.IsTrue(visitor.Outcome.Success, "Expected failure");
            Assert.AreEqual("Quantity, SampledData", r.ToString());
        }

        [TestMethod]
        public void TestMethodSelect()
        {
            string expression = "contact.name[2].select(family & '' & $this.given.join(' ')).substring(0,10)";
            Console.WriteLine(expression);
            var visitor = new FhirPathExpressionVisitor();
            visitor.AddInputType(typeof(Patient));
            var pe = _compiler.Parse(expression);
            Console.WriteLine("---------");
            pe.Accept(visitor);
            Console.WriteLine(visitor.ToString());
            Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
            Assert.IsTrue(visitor.Outcome.Success);
        }

        [TestMethod]
        public void TestMethod2()
        {
            string expression = "contact.name[2] | item.item.select(item[0].item.where(linkId='i45')).text";
            Console.WriteLine(expression);
            FhirPathExpressionVisitor visitor = new ();
            visitor.AddInputType(typeof(Questionnaire));
            var pe = _compiler.Parse(expression);
            Console.WriteLine("---------");
            pe.Accept(visitor);
            Console.WriteLine(visitor.ToString());
            Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
            Assert.IsTrue(visitor.Outcome.Success);
        }

        [TestMethod]
        public void TestMethodThis()
        {
            string expression = "name.select($this)";
            Console.WriteLine(expression);
            var visitor = new FhirPathExpressionVisitor();
            visitor.AddInputType(typeof(Questionnaire));
            var pe = _compiler.Parse(expression);
            Console.WriteLine("---------");
            pe.Accept(visitor);
            Console.WriteLine(visitor.ToString());
            Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
            Assert.IsTrue(visitor.Outcome.Success);
        }

        [TestMethod]
        public void TestMethodVariable()
        {
            string expression = "%surprise.family";
            Console.WriteLine(expression);
            var visitor = new FhirPathExpressionVisitor();
            visitor.AddInputType(typeof(Questionnaire));
            visitor.RegisterVariable("surprise", typeof(Hl7.Fhir.Model.HumanName));
            var pe = _compiler.Parse(expression);
            Console.WriteLine("---------");
            pe.Accept(visitor);
            Console.WriteLine(visitor.ToString());
            Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
            Assert.IsTrue(visitor.Outcome.Success);
        }

        [TestMethod]
        public void TestMethod3()
        {
            string expression = "name.skip(1).first().convertsToString().select($this)";
            Console.WriteLine(expression);
            var visitor = new FhirPathExpressionVisitor();
            visitor.AddInputType(typeof(Patient));
            var pe = _compiler.Parse(expression);
            Console.WriteLine("---------");
            pe.Accept(visitor);
            Console.WriteLine(visitor.ToString());
            Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
            Assert.IsTrue(visitor.Outcome.Success);
        }

        [TestMethod]
        public void TestMethodAge()
        {
            string expression = "(software.empty() and implementation.empty()) or kind != 'requirements'";
            Console.WriteLine(expression);
            var visitor = new FhirPathExpressionVisitor();
            visitor.AddInputType(typeof(CapabilityStatement));
            var pe = _compiler.Parse(expression);
            Console.WriteLine("---------");
            pe.Accept(visitor);
            Console.WriteLine(visitor.ToString());
            Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
            Assert.IsTrue(visitor.Outcome.Success);
        }

		[TestMethod]
		public void TestMethodNow()
		{
			string expression = "now() > Patient.birthDate";
			Console.WriteLine(expression);
			var visitor = new FhirPathExpressionVisitor();
			visitor.AddInputType(typeof(Patient));
			var pe = _compiler.Parse(expression);
			Console.WriteLine("---------");
			pe.Accept(visitor);
			Console.WriteLine(visitor.ToString());
			Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
			Assert.IsTrue(visitor.Outcome.Success);
		}

		[TestMethod]
		public void TestMethodToday()
		{
			string expression = "today(birthDate, deceased) > Patient.birthDate";
			Console.WriteLine(expression);
			var visitor = new FhirPathExpressionVisitor();
			visitor.AddInputType(typeof(Patient));
			var pe = _compiler.Parse(expression);
			Console.WriteLine("---------");
			pe.Accept(visitor);
			Console.WriteLine(visitor.ToString());
			Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
            // this should fail as the today function does not require any parameters
			Assert.IsFalse(visitor.Outcome.Success);
		}

		[TestMethod]
		public void TestMethodTimeOfDay()
		{
			string expression = "timeOfDay() > Patient.birthDate";
			Console.WriteLine(expression);
			var visitor = new FhirPathExpressionVisitor();
			visitor.AddInputType(typeof(Patient));
			var pe = _compiler.Parse(expression);
			Console.WriteLine("---------");
			pe.Accept(visitor);
			Console.WriteLine(visitor.ToString());
			Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
			Assert.IsTrue(visitor.Outcome.Success);
		}
	}
}