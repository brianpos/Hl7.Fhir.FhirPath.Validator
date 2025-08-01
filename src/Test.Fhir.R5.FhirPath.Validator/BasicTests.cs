﻿using Hl7.Fhir.FhirPath.Validator;
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
            pe.Accept(visitor);
            Console.WriteLine(visitor.ToString());
            Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
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
            pe.Accept(visitor);
            Console.WriteLine(visitor.ToString());
            Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
            Assert.IsFalse(visitor.Outcome.Success, "Expected failure");
        }

        [TestMethod]
        public void TestAsFunction()
        {
            string expression = "(deceased as boolean)";
            Console.WriteLine(expression);
            var visitor = new FhirPathExpressionVisitor();
            visitor.AddInputType(typeof(Patient));
            var pe = _compiler.Parse(expression);
            var r = pe.Accept(visitor);
            Console.WriteLine(visitor.ToString());
            Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
            Assert.IsTrue(visitor.Outcome.Success, "Expected failure");
            Assert.AreEqual("boolean", r.ToString());
        }

        [TestMethod]
        public void TestAsOperator()
        {
            string expression = "Condition.abatement.as(Age)";
            Console.WriteLine(expression);
            var visitor = new FhirPathExpressionVisitor();
            visitor.AddInputType(typeof(Condition));
            var pe = _compiler.Parse(expression);
            var r = pe.Accept(visitor);
            Console.WriteLine(visitor.ToString());
            Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
            Assert.IsTrue(visitor.Outcome.Success, "Expected failure");
            Assert.AreEqual("Age", r.ToString());
        }

        [TestMethod]
        public void TestAsOperatorMultiple()
        {
            string expression = "(value as Quantity) | (value as SampledData)";
            Console.WriteLine(expression);
            var visitor = new FhirPathExpressionVisitor();
            visitor.AddInputType(typeof(Observation));
            var pe = _compiler.Parse(expression);
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
            pe.Accept(visitor);
            Console.WriteLine(visitor.ToString());
            Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
            Assert.IsTrue(visitor.Outcome.Success);
        }

        [TestMethod]
        public void TestMethodArguments()
        {
            string expression = "name.given.substring(name.count())";
            Console.WriteLine(expression);
            var visitor = new FhirPathExpressionVisitor();
            visitor.AddInputType(typeof(Patient));
            var pe = _compiler.Parse(expression);
            pe.Accept(visitor);
            Console.WriteLine(visitor.ToString());
            Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
            Assert.IsTrue(visitor.Outcome.Success);
        }

        [TestMethod]
        public void TestMethodArgumentsInvalid()
        {
            string expression = "Patient.name.family.first().substring(2, length()-5)";
            Console.WriteLine(expression);
            var visitor = new FhirPathExpressionVisitor();
            visitor.AddInputType(typeof(Patient));
            var pe = _compiler.Parse(expression);
            pe.Accept(visitor);
            Console.WriteLine(visitor.ToString());
            Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
            Assert.IsFalse(visitor.Outcome.Success);
        }

        [TestMethod]
        public void TestMethodArgumentsInvalid2()
        {
            string expression = "Patient.name.select(family.first().substring(2, length()-5))";
            Console.WriteLine(expression);
            var visitor = new FhirPathExpressionVisitor();
            visitor.AddInputType(typeof(Patient));
            var pe = _compiler.Parse(expression);
            pe.Accept(visitor);
            Console.WriteLine(visitor.ToString());
            Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
            Assert.IsFalse(visitor.Outcome.Success);
        }

        [TestMethod]
        public void TestMethodArgumentsValid2()
        {
            string expression = "Patient.name.family.first().select(substring(2, length()-5))";
            Console.WriteLine(expression);
            var visitor = new FhirPathExpressionVisitor();
            visitor.AddInputType(typeof(Patient));
            var pe = _compiler.Parse(expression);
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
            pe.Accept(visitor);
            Console.WriteLine(visitor.ToString());
            Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
            Assert.IsTrue(visitor.Outcome.Success);
        }
    }
}