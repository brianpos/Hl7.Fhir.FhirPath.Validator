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
		public void TestMethodOrdinalCoding()
		{
			string expression = "contact.relationship.coding.ordinal()";
			Console.WriteLine(expression);
			var visitor = new FhirPathExpressionVisitor();
			visitor.AddInputType(typeof(Patient));
			var pe = _compiler.Parse(expression);
			Console.WriteLine("---------");
			var r = pe.Accept(visitor);
			Console.WriteLine(visitor.ToString());
			Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
			Assert.AreEqual("decimal", r.ToString());
			Assert.IsTrue(visitor.Outcome.Success);
		}

		[TestMethod]
		public void TestMethodOrdinalCodeableConcept()
		{
			string expression = "contact.relationship.ordinal()";
			Console.WriteLine(expression);
			var visitor = new FhirPathExpressionVisitor();
			visitor.AddInputType(typeof(Patient));
			var pe = _compiler.Parse(expression);
			Console.WriteLine("---------");
			var r = pe.Accept(visitor);
			Console.WriteLine(visitor.ToString());
			Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
			Assert.AreEqual("", r.ToString());
			Assert.IsFalse(visitor.Outcome.Success);
		}

		[TestMethod]
		public void TestMethodOrdinalCode()
		{
			string expression = "contact.relationship.coding.code.ordinal()";
			Console.WriteLine(expression);
			var visitor = new FhirPathExpressionVisitor();
			visitor.AddInputType(typeof(Patient));
			var pe = _compiler.Parse(expression);
			Console.WriteLine("---------");
			var r = pe.Accept(visitor);
			Console.WriteLine(visitor.ToString());
			Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
			Assert.AreEqual("decimal", r.ToString());
			Assert.IsTrue(visitor.Outcome.Success);
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
		public void TestMethodAnswers()
		{
			string expression = "answers().value.ofType(string).first()";
			Console.WriteLine(expression);
			var visitor = new FhirPathExpressionVisitor();
			visitor.AddInputType(typeof(QuestionnaireResponse));
			var pe = _compiler.Parse(expression);
			Console.WriteLine("---------");
			var r = pe.Accept(visitor);
			Console.WriteLine(visitor.ToString());
			Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
			Assert.AreEqual("string", r.ToString());
			Assert.IsTrue(visitor.Outcome.Success);
		}

		[TestMethod]
		public void TestMethodAnswersItemContext()
		{
			string expression = "item.first().answers().value.ofType(string).first()";
			Console.WriteLine(expression);
			var visitor = new FhirPathExpressionVisitor();
			visitor.AddInputType(typeof(QuestionnaireResponse));
			var pe = _compiler.Parse(expression);
			Console.WriteLine("---------");
			var r = pe.Accept(visitor);
			Console.WriteLine(visitor.ToString());
			Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
			Assert.AreEqual("string", r.ToString());
			Assert.IsTrue(visitor.Outcome.Success);
        }

        [TestMethod]
		public void TestMethodAnswersItemCollection()
		{
			string expression = "item.answers().value.ofType(string)";
			Console.WriteLine(expression);
			var visitor = new FhirPathExpressionVisitor();
			visitor.AddInputType(typeof(QuestionnaireResponse));
			var pe = _compiler.Parse(expression);
			Console.WriteLine("---------");
			var r = pe.Accept(visitor);
			Console.WriteLine(visitor.ToString());
			Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
			Assert.AreEqual("string[]", r.ToString());
			Assert.IsTrue(visitor.Outcome.Success);
		}

		[TestMethod]
		public void TestMethodAnswersNegativeType()
		{
			string expression = "item.first().answers().value.ofType(string).first()";
			Console.WriteLine(expression);
			var visitor = new FhirPathExpressionVisitor();
			visitor.AddInputType(typeof(Questionnaire));
			var pe = _compiler.Parse(expression);
			Console.WriteLine("---------");
			var r = pe.Accept(visitor);
			Console.WriteLine(visitor.ToString());
			Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
			Assert.AreEqual("", r.ToString());
			Assert.IsFalse(visitor.Outcome.Success);
		}

		[TestMethod]
		public void TestMethodAnswersNegativeProp()
		{
			string expression = "author.answers().value.ofType(string).first()";
			Console.WriteLine(expression);
			var visitor = new FhirPathExpressionVisitor();
			visitor.AddInputType(typeof(QuestionnaireResponse));
			var pe = _compiler.Parse(expression);
			Console.WriteLine("---------");
			var r = pe.Accept(visitor);
			Console.WriteLine(visitor.ToString());
			Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
			Assert.AreEqual("", r.ToString());
			Assert.IsFalse(visitor.Outcome.Success);
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
            Assert.AreEqual("", r.ToString());
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
            Assert.IsTrue(visitor.Outcome.Success);
			Assert.AreEqual(0, visitor.Outcome.Errors);
			Assert.AreEqual(1, visitor.Outcome.Warnings);
			Assert.AreEqual("integer", r.ToString());
        }

		[TestMethod]
		public void TestLengthContextNonStringCollection()
		{
			string expression = "contact.length()";
			Console.WriteLine(expression);
			var visitor = new FhirPathExpressionVisitor();
			visitor.AddInputType(typeof(Patient));
			var pe = _compiler.Parse(expression);
			Console.WriteLine("---------");
			var r = pe.Accept(visitor);
			Console.WriteLine(visitor.ToString());
			Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
            Assert.IsFalse(visitor.Outcome.Success, "Expected failure");
			Assert.AreEqual("", r.ToString());
            Assert.AreEqual(1, visitor.Outcome.Errors);
			Assert.AreEqual(0, visitor.Outcome.Warnings);
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

		[TestMethod]
		public void TestMethodDescendants()
		{
			string expression = "Patient.deceased.descendants()";
			Console.WriteLine(expression);
			var visitor = new FhirPathExpressionVisitor();
			visitor.AddInputType(typeof(Patient));
			var pe = _compiler.Parse(expression);
			Console.WriteLine("---------");
			var r = pe.Accept(visitor);
			Console.WriteLine(visitor.ToString());
			Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
			Assert.AreEqual("System.String, Extension, Address, code, string, Period, dateTime, System.DateTime, Age, decimal, System.Decimal, uri, Annotation, Reference, Identifier, CodeableConcept, Coding, boolean, System.Boolean, markdown, Attachment, base64Binary, url, unsignedInt, System.Integer, canonical, ContactDetail, ContactPoint, positiveInt, Contributor, Count, DataRequirement, DataRequirement#CodeFilter, DataRequirement#DateFilter, Duration, DataRequirement#Sort, date, System.Date, Distance, Dosage, integer, Timing, Timing#Repeat, Range, Quantity, time, System.Time, Dosage#DoseAndRate, Ratio, Expression, id, HumanName, instant, Meta, Money, oid, ParameterDefinition, RelatedArtifact, SampledData, Signature, TriggerDefinition, UsageContext, uuid", r.ToString());
			Assert.IsTrue(visitor.Outcome.Success);
		}

		[TestMethod]
		public void TestMethodDescendantsValue()
		{
			string expression = "item.descendants()";
			Console.WriteLine(expression);
			var visitor = new FhirPathExpressionVisitor();
			visitor.AddInputType(typeof(QuestionnaireResponse));
			var pe = _compiler.Parse(expression);
			Console.WriteLine("---------");
			var r = pe.Accept(visitor);
			Console.WriteLine(visitor.ToString());
			Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
			Assert.AreEqual("System.String, Extension, Address, code, string, Period, dateTime, System.DateTime, Age, decimal, System.Decimal, uri, Annotation, Reference, Identifier, CodeableConcept, Coding, boolean, System.Boolean, markdown, Attachment, base64Binary, url, unsignedInt, System.Integer, canonical, ContactDetail, ContactPoint, positiveInt, Contributor, Count, DataRequirement, DataRequirement#CodeFilter, DataRequirement#DateFilter, Duration, DataRequirement#Sort, date, System.Date, Distance, Dosage, integer, Timing, Timing#Repeat, Range, Quantity, time, System.Time, Dosage#DoseAndRate, Ratio, Expression, id, HumanName, instant, Meta, Money, oid, ParameterDefinition, RelatedArtifact, SampledData, Signature, TriggerDefinition, UsageContext, uuid, QuestionnaireResponse#Answer, QuestionnaireResponse#Item", r.ToString());
			Assert.IsTrue(visitor.Outcome.Success);
		}

		[TestMethod]
		public void TestMethodDescendantsWithResourceTypeChild_Parameters()
		{
			string expression = "descendants()";
			Console.WriteLine(expression);
			var visitor = new FhirPathExpressionVisitor();
			visitor.AddInputType(typeof(Parameters));
			var pe = _compiler.Parse(expression);
			Console.WriteLine("---------");
			var r = pe.Accept(visitor);
			Console.WriteLine(visitor.ToString());
			Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
			Assert.AreEqual(
                "id, System.String, Extension, Address, code, string, Period, dateTime, System.DateTime, Age, decimal, System.Decimal, uri, Annotation, Reference, Identifier, CodeableConcept, Coding, boolean, System.Boolean, markdown, Attachment, base64Binary, url, unsignedInt, System.Integer, canonical, ContactDetail, ContactPoint, positiveInt, Contributor, Count, DataRequirement, DataRequirement#CodeFilter, DataRequirement#DateFilter, Duration, DataRequirement#Sort, date, System.Date, Distance, Dosage, integer, Timing, Timing#Repeat, Range, Quantity, time, System.Time, Dosage#DoseAndRate, Ratio, Expression, HumanName, instant, Meta, Money, oid, ParameterDefinition, RelatedArtifact, SampledData, Signature, TriggerDefinition, UsageContext, uuid, Parameters#Parameter, Account, Narrative, xhtml, ActivityDefinition, AdministrableProductDefinition, AdverseEvent, AllergyIntolerance, Appointment, AppointmentResponse, AuditEvent, Basic, Binary, BiologicallyDerivedProduct, BodyStructure, Bundle, Bundle#Link, Bundle#Entry, CapabilityStatement, CarePlan, CareTeam, CatalogEntry, ChargeItem, ChargeItemDefinition, Citation, Claim, ClaimResponse, ClinicalImpression, ClinicalUseDefinition, CodeSystem, Communication, CommunicationRequest, CompartmentDefinition, Composition, ConceptMap, Condition, Consent, Contract, Coverage, CoverageEligibilityRequest, CoverageEligibilityResponse, DetectedIssue, Device, DeviceDefinition, DeviceMetric, DeviceRequest, DeviceUseStatement, DiagnosticReport, DocumentManifest, DocumentReference, Encounter, Endpoint, EnrollmentRequest, EnrollmentResponse, EpisodeOfCare, EventDefinition, Evidence, EvidenceReport, EvidenceVariable, ExampleScenario, ExplanationOfBenefit, FamilyMemberHistory, Flag, Goal, GraphDefinition, Group, GuidanceResponse, HealthcareService, ImagingStudy, Immunization, ImmunizationEvaluation, ImmunizationRecommendation, ImplementationGuide, Ingredient, InsurancePlan, Invoice, Library, Linkage, List, Location, ManufacturedItemDefinition, Measure, MeasureReport, Media, Medication, MedicationAdministration, MedicationDispense, MedicationKnowledge, MedicationRequest, MedicationStatement, MedicinalProductDefinition, MessageDefinition, MessageHeader, MolecularSequence, NamingSystem, NutritionOrder, NutritionProduct, Observation, ObservationDefinition, OperationDefinition, OperationOutcome, Organization, OrganizationAffiliation, PackagedProductDefinition, Parameters, Patient, PaymentNotice, PaymentReconciliation, Person, PlanDefinition, Practitioner, PractitionerRole, Procedure, Provenance, Questionnaire, QuestionnaireResponse, RegulatedAuthorization, RelatedPerson, RequestGroup, ResearchDefinition, ResearchElementDefinition, ResearchStudy, ResearchSubject, RiskAssessment, Schedule, SearchParameter, ServiceRequest, Slot, Specimen, SpecimenDefinition, StructureDefinition, StructureMap, Subscription, SubscriptionStatus, SubscriptionTopic, Substance, SubstanceDefinition, SupplyDelivery, SupplyRequest, Task, TerminologyCapabilities, TestReport, TestScript, ValueSet, VerificationResult, VisionPrescription, VisionPrescription#LensSpecification, VisionPrescription#Prism, VerificationResult#PrimarySource, VerificationResult#Attestation, VerificationResult#Validator, ValueSet#Compose, ValueSet#ConceptSet, ValueSet#ConceptReference, ValueSet#Designation, ValueSet#Filter, ValueSet#Expansion, ValueSet#Parameter, ValueSet#Contains, TestScript#Origin, TestScript#Destination, TestScript#Metadata, TestScript#Link, TestScript#Capability, TestScript#Fixture, TestScript#Variable, TestScript#Setup, TestScript#SetupAction, TestScript#Operation, TestScript#RequestHeader, TestScript#Assert, TestScript#Test, TestScript#TestAction, TestScript#Teardown, TestScript#TeardownAction, TestReport#Participant, TestReport#Setup, TestReport#SetupAction, TestReport#Operation, TestReport#Assert, TestReport#Test, TestReport#TestAction, TestReport#Teardown, TestReport#TeardownAction, TerminologyCapabilities#Software, TerminologyCapabilities#Implementation, TerminologyCapabilities#CodeSystem, TerminologyCapabilities#Version, TerminologyCapabilities#Filter, TerminologyCapabilities#Expansion, TerminologyCapabilities#Parameter, TerminologyCapabilities#ValidateCode, TerminologyCapabilities#Translation, TerminologyCapabilities#Closure, Task#Restriction, Task#Parameter, Task#Output, SupplyRequest#Parameter, SupplyDelivery#SuppliedItem, SubstanceDefinition#Moiety, SubstanceDefinition#Property, SubstanceDefinition#MolecularWeight, SubstanceDefinition#Structure, SubstanceDefinition#Representation, SubstanceDefinition#Code, SubstanceDefinition#Name, SubstanceDefinition#Official, SubstanceDefinition#Relationship, SubstanceDefinition#SourceMaterial, Substance#Instance, Substance#Ingredient, SubscriptionTopic#ResourceTrigger, SubscriptionTopic#QueryCriteria, SubscriptionTopic#EventTrigger, SubscriptionTopic#CanFilterBy, SubscriptionTopic#NotificationShape, SubscriptionStatus#NotificationEvent, Subscription#Channel, StructureMap#Structure, StructureMap#Group, StructureMap#Input, StructureMap#Rule, StructureMap#Source, StructureMap#Target, StructureMap#Parameter, StructureMap#Dependent, StructureDefinition#Mapping, StructureDefinition#Context, StructureDefinition#Snapshot, ElementDefinition, ElementDefinition#Slicing, ElementDefinition#Discriminator, ElementDefinition#Base, ElementDefinition#TypeRef, ElementDefinition#Example, integer64, System.Long, ElementDefinition#Constraint, ElementDefinition#ElementDefinitionBinding, ElementDefinition#Mapping, StructureDefinition#Differential, SpecimenDefinition#TypeTested, SpecimenDefinition#Container, SpecimenDefinition#Additive, SpecimenDefinition#Handling, Specimen#Collection, Specimen#Processing, Specimen#Container, SearchParameter#Component, RiskAssessment#Prediction, ResearchStudy#Arm, ResearchStudy#Objective, ResearchElementDefinition#Characteristic, RequestGroup#Action, RequestGroup#Condition, RequestGroup#RelatedAction, RelatedPerson#Communication, CodeableReference, RegulatedAuthorization#Case, QuestionnaireResponse#Item, QuestionnaireResponse#Answer, Questionnaire#Item, Questionnaire#EnableWhen, Questionnaire#AnswerOption, Questionnaire#Initial, Provenance#Agent, Provenance#Entity, Procedure#Performer, Procedure#FocalDevice, PractitionerRole#AvailableTime, PractitionerRole#NotAvailable, Practitioner#Qualification, PlanDefinition#Goal, PlanDefinition#Target, PlanDefinition#Action, PlanDefinition#Condition, PlanDefinition#RelatedAction, PlanDefinition#Participant, PlanDefinition#DynamicValue, Person#Link, PaymentReconciliation#Details, PaymentReconciliation#Notes, Patient#Contact, Patient#Communication, Patient#Link, PackagedProductDefinition#LegalStatusOfSupply, MarketingStatus, PackagedProductDefinition#Package, PackagedProductDefinition#ShelfLifeStorage, PackagedProductDefinition#Property, PackagedProductDefinition#ContainedItem, Organization#Contact, OperationOutcome#Issue, OperationDefinition#Parameter, OperationDefinition#Binding, OperationDefinition#ReferencedFrom, OperationDefinition#Overload, ObservationDefinition#QuantitativeDetails, ObservationDefinition#QualifiedInterval, Observation#ReferenceRange, Observation#Component, NutritionProduct#Nutrient, NutritionProduct#Ingredient, NutritionProduct#ProductCharacteristic, NutritionProduct#Instance, NutritionOrder#OralDiet, NutritionOrder#Nutrient, NutritionOrder#Texture, NutritionOrder#Supplement, NutritionOrder#EnteralFormula, NutritionOrder#Administration, NamingSystem#UniqueId, MolecularSequence#ReferenceSeq, MolecularSequence#Variant, MolecularSequence#Quality, MolecularSequence#Roc, MolecularSequence#Repository, MolecularSequence#StructureVariant, MolecularSequence#Outer, MolecularSequence#Inner, MessageHeader#MessageDestination, MessageHeader#MessageSource, MessageHeader#Response, MessageDefinition#Focus, MessageDefinition#AllowedResponse, MedicinalProductDefinition#Contact, MedicinalProductDefinition#Name, MedicinalProductDefinition#NamePart, MedicinalProductDefinition#CountryLanguage, MedicinalProductDefinition#CrossReference, MedicinalProductDefinition#Operation, MedicinalProductDefinition#Characteristic, MedicationRequest#DispenseRequest, MedicationRequest#InitialFill, MedicationRequest#Substitution, MedicationKnowledge#RelatedMedicationKnowledge, MedicationKnowledge#Monograph, MedicationKnowledge#Ingredient, MedicationKnowledge#Cost, MedicationKnowledge#MonitoringProgram, MedicationKnowledge#AdministrationGuidelines, MedicationKnowledge#Dosage, MedicationKnowledge#PatientCharacteristics, MedicationKnowledge#MedicineClassification, MedicationKnowledge#Packaging, MedicationKnowledge#DrugCharacteristic, MedicationKnowledge#Regulatory, MedicationKnowledge#Substitution, MedicationKnowledge#Schedule, MedicationKnowledge#MaxDispense, MedicationKnowledge#Kinetics, MedicationDispense#Performer, MedicationDispense#Substitution, MedicationAdministration#Performer, MedicationAdministration#Dosage, Medication#Ingredient, Medication#Batch, MeasureReport#Group, MeasureReport#Population, MeasureReport#Stratifier, MeasureReport#StratifierGroup, MeasureReport#Component, MeasureReport#StratifierGroupPopulation, Measure#Group, Measure#Population, Measure#Stratifier, Measure#Component, Measure#SupplementalData, ManufacturedItemDefinition#Property, Location#Position, Location#HoursOfOperation, List#Entry, Linkage#Item, Invoice#Participant, Invoice#LineItem, Invoice#PriceComponent, InsurancePlan#Contact, InsurancePlan#Coverage, InsurancePlan#CoverageBenefit, InsurancePlan#Limit, InsurancePlan#Plan, InsurancePlan#GeneralCost, InsurancePlan#SpecificCost, InsurancePlan#PlanBenefit, InsurancePlan#Cost, Ingredient#Manufacturer, Ingredient#Substance, Ingredient#Strength, RatioRange, Ingredient#ReferenceStrength, ImplementationGuide#DependsOn, ImplementationGuide#Global, ImplementationGuide#Definition, ImplementationGuide#Grouping, ImplementationGuide#Resource, ImplementationGuide#Page, ImplementationGuide#Parameter, ImplementationGuide#Template, ImplementationGuide#Manifest, ImplementationGuide#ManifestResource, ImplementationGuide#ManifestPage, ImmunizationRecommendation#Recommendation, ImmunizationRecommendation#DateCriterion, Immunization#Performer, Immunization#Education, Immunization#Reaction, Immunization#ProtocolApplied, ImagingStudy#Series, ImagingStudy#Performer, ImagingStudy#Instance, HealthcareService#Eligibility, HealthcareService#AvailableTime, HealthcareService#NotAvailable, Group#Characteristic, Group#Member, GraphDefinition#Link, GraphDefinition#Target, GraphDefinition#Compartment, Goal#Target, FamilyMemberHistory#Condition, ExplanationOfBenefit#RelatedClaim, ExplanationOfBenefit#Payee, ExplanationOfBenefit#CareTeam, ExplanationOfBenefit#SupportingInformation, ExplanationOfBenefit#Diagnosis, ExplanationOfBenefit#Procedure, ExplanationOfBenefit#Insurance, ExplanationOfBenefit#Accident, ExplanationOfBenefit#Item, ExplanationOfBenefit#Adjudication, ExplanationOfBenefit#Detail, ExplanationOfBenefit#SubDetail, ExplanationOfBenefit#AddedItem, ExplanationOfBenefit#AddedItemDetail, ExplanationOfBenefit#AddedItemDetailSubDetail, ExplanationOfBenefit#Total, ExplanationOfBenefit#Payment, ExplanationOfBenefit#Note, ExplanationOfBenefit#BenefitBalance, ExplanationOfBenefit#Benefit, ExampleScenario#Actor, ExampleScenario#Instance, ExampleScenario#Version, ExampleScenario#ContainedInstance, ExampleScenario#Process, ExampleScenario#Step, ExampleScenario#Operation, ExampleScenario#Alternative, EvidenceVariable#Characteristic, EvidenceVariable#TimeFromStart, EvidenceVariable#Category, EvidenceReport#Subject, EvidenceReport#Characteristic, EvidenceReport#RelatesTo, EvidenceReport#Section, Evidence#VariableDefinition, Evidence#Statistic, Evidence#SampleSize, Evidence#AttributeEstimate, Evidence#ModelCharacteristic, Evidence#Variable, Evidence#Certainty, EpisodeOfCare#StatusHistory, EpisodeOfCare#Diagnosis, Encounter#StatusHistory, Encounter#ClassHistory, Encounter#Participant, Encounter#Diagnosis, Encounter#Hospitalization, Encounter#Location, DocumentReference#RelatesTo, DocumentReference#Content, DocumentReference#Context, DocumentManifest#Related, DiagnosticReport#Media, DeviceRequest#Parameter, DeviceMetric#Calibration, DeviceDefinition#UdiDeviceIdentifier, DeviceDefinition#DeviceName, DeviceDefinition#Specialization, ProductShelfLife, ProdCharacteristic, DeviceDefinition#Capability, DeviceDefinition#Property, DeviceDefinition#Material, Device#UdiCarrier, Device#DeviceName, Device#Specialization, Device#Version, Device#Property, DetectedIssue#Evidence, DetectedIssue#Mitigation, CoverageEligibilityResponse#Insurance, CoverageEligibilityResponse#Items, CoverageEligibilityResponse#Benefit, CoverageEligibilityResponse#Errors, CoverageEligibilityRequest#SupportingInformation, CoverageEligibilityRequest#Insurance, CoverageEligibilityRequest#Details, CoverageEligibilityRequest#Diagnosis, Coverage#Class, Coverage#CostToBeneficiary, Coverage#Exemption, Contract#ContentDefinition, Contract#Term, Contract#SecurityLabel, Contract#ContractOffer, Contract#ContractParty, Contract#Answer, Contract#ContractAsset, Contract#AssetContext, Contract#ValuedItem, Contract#Action, Contract#ActionSubject, Contract#Signatory, Contract#FriendlyLanguage, Contract#LegalLanguage, Contract#ComputableLanguage, Consent#Policy, Consent#Verification, Consent#provision, Consent#provisionActor, Consent#provisionData, Condition#Stage, Condition#Evidence, ConceptMap#Group, ConceptMap#SourceElement, ConceptMap#TargetElement, ConceptMap#OtherElement, ConceptMap#Unmapped, Composition#Attester, Composition#RelatesTo, Composition#Event, Composition#Section, CompartmentDefinition#Resource, CommunicationRequest#Payload, Communication#Payload, CodeSystem#Filter, CodeSystem#Property, CodeSystem#ConceptDefinition, CodeSystem#Designation, CodeSystem#ConceptProperty, ClinicalUseDefinition#Contraindication, ClinicalUseDefinition#OtherTherapy, ClinicalUseDefinition#Indication, ClinicalUseDefinition#Interaction, ClinicalUseDefinition#Interactant, ClinicalUseDefinition#UndesirableEffect, ClinicalUseDefinition#Warning, ClinicalImpression#Investigation, ClinicalImpression#Finding, ClaimResponse#Item, ClaimResponse#Adjudication, ClaimResponse#ItemDetail, ClaimResponse#SubDetail, ClaimResponse#AddedItem, ClaimResponse#AddedItemDetail, ClaimResponse#AddedItemSubDetail, ClaimResponse#Total, ClaimResponse#Payment, ClaimResponse#Note, ClaimResponse#Insurance, ClaimResponse#Error, Claim#RelatedClaim, Claim#Payee, Claim#CareTeam, Claim#SupportingInformation, Claim#Diagnosis, Claim#Procedure, Claim#Insurance, Claim#Accident, Claim#Item, Claim#Detail, Claim#SubDetail, Citation#Summary, Citation#Classification, Citation#StatusDate, Citation#RelatesTo, Citation#CitedArtifact, Citation#CitedArtifactVersion, Citation#CitedArtifactStatusDate, Citation#CitedArtifactTitle, Citation#CitedArtifactAbstract, Citation#CitedArtifactPart, Citation#CitedArtifactRelatesTo, Citation#CitedArtifactPublicationForm, Citation#CitedArtifactPublicationFormPublishedIn, Citation#CitedArtifactPublicationFormPeriodicRelease, Citation#CitedArtifactPublicationFormPeriodicReleaseDateOfPublication, Citation#CitedArtifactWebLocation, Citation#CitedArtifactClassification, Citation#CitedArtifactClassificationWhoClassified, Citation#CitedArtifactContributorship, Citation#CitedArtifactContributorshipEntry, Citation#CitedArtifactContributorshipEntryAffiliationInfo, Citation#CitedArtifactContributorshipEntryContributionInstance, Citation#CitedArtifactContributorshipSummary, ChargeItemDefinition#Applicability, ChargeItemDefinition#PropertyGroup, ChargeItemDefinition#PriceComponent, ChargeItem#Performer, CatalogEntry#RelatedEntry, CareTeam#Participant, CarePlan#Activity, CarePlan#Detail, CapabilityStatement#Software, CapabilityStatement#Implementation, CapabilityStatement#Rest, CapabilityStatement#Security, CapabilityStatement#Resource, CapabilityStatement#ResourceInteraction, CapabilityStatement#SearchParam, CapabilityStatement#Operation, CapabilityStatement#SystemInteraction, CapabilityStatement#Messaging, CapabilityStatement#Endpoint, CapabilityStatement#SupportedMessage, CapabilityStatement#Document, Bundle#Search, Bundle#Request, Bundle#Response, BiologicallyDerivedProduct#Collection, BiologicallyDerivedProduct#Processing, BiologicallyDerivedProduct#Manipulation, BiologicallyDerivedProduct#Storage, AuditEvent#Agent, AuditEvent#Network, AuditEvent#Source, AuditEvent#Entity, AuditEvent#Detail, Appointment#Participant, AllergyIntolerance#Reaction, AdverseEvent#SuspectEntity, AdverseEvent#Causality, AdministrableProductDefinition#Property, AdministrableProductDefinition#RouteOfAdministration, AdministrableProductDefinition#TargetSpecies, AdministrableProductDefinition#WithdrawalPeriod, ActivityDefinition#Participant, ActivityDefinition#DynamicValue, Account#Coverage, Account#Guarantor",
                r.ToString());
			Assert.IsTrue(visitor.Outcome.Success);
		}

		[TestMethod]
		public void TestIdentifierValue()
		{
			string expression = "value.startsWith('123')";
			Console.WriteLine(expression);
			var visitor = new FhirPathExpressionVisitor();
			visitor.AddInputType(typeof(FhirString));
			var pe = _compiler.Parse(expression);
			Console.WriteLine("---------");
			pe.Accept(visitor);
			Console.WriteLine(visitor.ToString());
			Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
			Assert.IsFalse(visitor.Outcome.Success);
            Assert.AreEqual(2, visitor.Outcome.Errors);
		}

		[TestMethod]
		public void TestCrossResourceExpression()
		{
			string expression = "CodeSystem.id.exists()";
			Console.WriteLine(expression);
			var visitor = new FhirPathExpressionVisitor();
			visitor.AddInputType(typeof(ValueSet));
			var pe = _compiler.Parse(expression);
			Console.WriteLine("---------");
			pe.Accept(visitor);
			Console.WriteLine(visitor.ToString());
			Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
			Assert.IsFalse(visitor.Outcome.Success);
			Assert.AreEqual(2, visitor.Outcome.Errors);
		}

		[TestMethod]
		public void TestMethodToString()
		{
			string expression = "(5|6).toString()";
			Console.WriteLine(expression);
			var visitor = new FhirPathExpressionVisitor();
			visitor.AddInputType(typeof(Patient));
			var pe = _compiler.Parse(expression);
			Console.WriteLine("---------");
			pe.Accept(visitor);
			Console.WriteLine(visitor.ToString());
			Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
			// this should fail as the today function does not require any parameters
			Assert.IsTrue(visitor.Outcome.Success);
            Assert.AreEqual(1, visitor.Outcome.Warnings, "ToString can't be used on collections, but we've downgraded intentionally to a warning");
		}

		[TestMethod]
		public void TestMethodAbs()
		{
			string expression = "(-5).abs() = 5";
			Console.WriteLine(expression);
			var visitor = new FhirPathExpressionVisitor();
			visitor.AddInputType(typeof(Patient));
			var pe = _compiler.Parse(expression);
			Console.WriteLine("---------");
			pe.Accept(visitor);
			Console.WriteLine(visitor.ToString());
			Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
			// this should fail as the today function does not require any parameters
			Assert.IsTrue(visitor.Outcome.Success);
		}

		[TestMethod]
		public void TestMethodCeiling()
		{
			string expression = "(+1.1).ceiling()";
			Console.WriteLine(expression);
			var visitor = new FhirPathExpressionVisitor();
			visitor.AddInputType(typeof(Patient));
			var pe = _compiler.Parse(expression);
			Console.WriteLine("---------");
			pe.Accept(visitor);
			Console.WriteLine(visitor.ToString());
			Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
			// this should fail as the today function does not require any parameters
			Assert.IsTrue(visitor.Outcome.Success);
		}

		[TestMethod]
		public void TestMethodIif()
		{
			string expression = "iif(true, 2, 'false')";
			Console.WriteLine(expression);
			var visitor = new FhirPathExpressionVisitor();
			visitor.AddInputType(typeof(Patient));
			var pe = _compiler.Parse(expression);
			Console.WriteLine("---------");
			var r = pe.Accept(visitor);
			Console.WriteLine(visitor.ToString());
			Console.WriteLine($"Result Type: {r.ToString()}");
			Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
			// this should fail as the today function does not require any parameters
			Assert.IsTrue(visitor.Outcome.Success);
            Assert.AreEqual("integer, string", r.ToString());
		}

		[TestMethod]
		public void TestMethodIifArgs()
		{
			string expression = "iif('4', 2, 'false')";
			Console.WriteLine(expression);
			var visitor = new FhirPathExpressionVisitor();
			visitor.AddInputType(typeof(Patient));
			var pe = _compiler.Parse(expression);
			Console.WriteLine("---------");
			var r = pe.Accept(visitor);
			Console.WriteLine(visitor.ToString());
			Console.WriteLine($"Result Type: {r.ToString()}");
			Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
			// this should fail as the today function does not require any parameters
			Assert.IsFalse(visitor.Outcome.Success);
			Assert.AreEqual("integer, string", r.ToString());
		}

		[TestMethod]
		public void TestMethodIifMoreBadArgs()
		{
			string expression = "iif(true | false, 2, 'false')";
			Console.WriteLine(expression);
			var visitor = new FhirPathExpressionVisitor();
			visitor.AddInputType(typeof(Patient));
			var pe = _compiler.Parse(expression);
			Console.WriteLine("---------");
			var r = pe.Accept(visitor);
			Console.WriteLine(visitor.ToString());
			Console.WriteLine($"Result Type: {r.ToString()}");
			Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
			// this should fail as the today function does not require any parameters
			Assert.IsFalse(visitor.Outcome.Success);
			Assert.AreEqual("integer, string", r.ToString());
		}

		[TestMethod]
		public void TestMethodIifSingleArg()
		{
			string expression = "iif('4', false)";
			Console.WriteLine(expression);
			var visitor = new FhirPathExpressionVisitor();
			visitor.AddInputType(typeof(Patient));
			var pe = _compiler.Parse(expression);
			Console.WriteLine("---------");
			var r = pe.Accept(visitor);
			Console.WriteLine(visitor.ToString());
			Console.WriteLine($"Result Type: {r.ToString()}");
			Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
			// this should fail as the today function does not require any parameters
			Assert.IsFalse(visitor.Outcome.Success);
			Assert.AreEqual("boolean", r.ToString());
		}

		[TestMethod]
		public void TestMethodIifEmptySourceColl()
		{
			string expression = "{}.iif(true, '1')";
			Console.WriteLine(expression);
			var visitor = new FhirPathExpressionVisitor();
			visitor.AddInputType(typeof(Patient));
			var pe = _compiler.Parse(expression);
			Console.WriteLine("---------");
			var r = pe.Accept(visitor);
			Console.WriteLine(visitor.ToString());
			Console.WriteLine($"Result Type: {r.ToString()}");
			Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
			// this should fail as the today function does not require any parameters
			Assert.IsTrue(visitor.Outcome.Success);
			Assert.AreEqual("string", r.ToString());
		}

		[TestMethod]
		public void TestMethodIifSingleSourceColl()
		{
			string expression = "('item').iif(true, '1')";
			Console.WriteLine(expression);
			var visitor = new FhirPathExpressionVisitor();
			visitor.AddInputType(typeof(Patient));
			var pe = _compiler.Parse(expression);
			Console.WriteLine("---------");
			var r = pe.Accept(visitor);
			Console.WriteLine(visitor.ToString());
			Console.WriteLine($"Result Type: {r.ToString()}");
			Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
			// this should fail as the today function does not require any parameters
			Assert.IsTrue(visitor.Outcome.Success);
			Assert.AreEqual("string", r.ToString());
		}

		[TestMethod]
		public void TestMethodIifMultipleSourceColl()
		{
			string expression = "('item1' | 'item2').iif(true, false)";
			Console.WriteLine(expression);
			var visitor = new FhirPathExpressionVisitor();
			visitor.AddInputType(typeof(Patient));
			var pe = _compiler.Parse(expression);
			Console.WriteLine("---------");
			var r = pe.Accept(visitor);
			Console.WriteLine(visitor.ToString());
			Console.WriteLine($"Result Type: {r.ToString()}");
			Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
			// this should fail as the today function does not require any parameters
			Assert.IsFalse(visitor.Outcome.Success);
			Assert.AreEqual("", r.ToString());
		}

		[TestMethod]
		public void TestMethodIif2StringsArgs()
		{
			string expression = "iif(true, 'yes', 'no')";
			Console.WriteLine(expression);
			var visitor = new FhirPathExpressionVisitor();
			visitor.AddInputType(typeof(Patient));
			var pe = _compiler.Parse(expression);
			Console.WriteLine("---------");
			var r = pe.Accept(visitor);
			Console.WriteLine(visitor.ToString());
			Console.WriteLine($"Result Type: {r.ToString()}");
			Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
			// this should fail as the today function does not require any parameters
			Assert.IsTrue(visitor.Outcome.Success);
			Assert.AreEqual("string", r.ToString());
		}

		[TestMethod]
		public void TestMethodIif2StringsArgsAgain()
		{
			string expression = "iif(true, 'yes', 'no' | 'maybe')";
			Console.WriteLine(expression);
			var visitor = new FhirPathExpressionVisitor();
			visitor.AddInputType(typeof(Patient));
			var pe = _compiler.Parse(expression);
			Console.WriteLine("---------");
			var r = pe.Accept(visitor);
			Console.WriteLine(visitor.ToString());
			Console.WriteLine($"Result Type: {r.ToString()}");
			Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
			// this should fail as the today function does not require any parameters
			Assert.IsTrue(visitor.Outcome.Success);
			Assert.AreEqual("string, string[]", r.ToString());
		}
		// Add in a test for how to handle validating an extension definition which leverages the contexts of the extensions.
	}
}