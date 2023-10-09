using Hl7.Fhir.FhirPath.Validator;
using Hl7.Fhir.Introspection;
using Hl7.FhirPath.Expressions;
using Hl7.FhirPath;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Model;
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.FhirPath;
using System.Linq;

namespace Test.Fhir.FhirPath.Validator
{
    /// <summary>
    /// Testing section 4.1 of the Fhirpath spec
    /// </summary>
    [TestClass]
    public class LiteralTests_4_1
    {
        FhirPathCompiler _compiler;

        [TestInitialize]
        public void Init()
        {
            Hl7.Fhir.FhirPath.ElementNavFhirExtensions.PrepareFhirSymbolTableFunctions();
            SymbolTable symbolTable = new (FhirPathCompiler.DefaultSymbolTable);
            _compiler = new FhirPathCompiler(symbolTable);
        }

        public void VerifyConstant(string expression, string expectedType)
        {
            Console.WriteLine(expression);
            var visitor = new FhirPathExpressionVisitor();
            visitor.AddInputType(typeof(Patient));
            var pe = _compiler.Parse(expression);
            Console.WriteLine("---------");
            var r = pe.Accept(visitor);
            Console.WriteLine(visitor.ToString());
            Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
            Assert.AreEqual(expectedType, r.ToString());
            Assert.IsTrue(visitor.Outcome.Success);
        }

        [TestMethod]
        public void ResolveDemo()
        {
            ElementNavFhirExtensions.PrepareFhirSymbolTableFunctions();
            var sr = new ServiceRequest
            {
                Subject = new ResourceReference
                {
                    Reference = "Patient/1",
                }
            }.ToTypedElement();
            // FhirPathCompiler.DefaultSymbolTable.Add(new CallSignature("where", typeof(IEnumerable<ITypedElement>), typeof(object), typeof(bool)), runWhere);

            var context = new FhirEvaluationContext(sr);
            // Mock a ElementResolver only applicable to this unit test
            context.ElementResolver = (s) =>
            {
                return new Patient
                {
                    Id = "1"
                }.ToTypedElement();
            };
            var values = sr.Select("ServiceRequest.subject.where(resolve() is Patient)", context);
            Assert.AreEqual(1, values.Count());
            var fhirValueProvider = values.First().Annotation<IFhirValueProvider>();
            Assert.IsNotNull(fhirValueProvider);
            Assert.IsNotNull(fhirValueProvider.FhirValue);
            var r = fhirValueProvider.FhirValue as ResourceReference;
            Assert.IsNotNull(r);
            Assert.AreEqual("Patient/1", r.Reference);
        }

        [TestMethod]
        public void VerifyBooleanConstant()
        {
            VerifyConstant("true | false", "boolean[]");
        }

        [TestMethod]
        public void VerifyStringConstant()
        {
            VerifyConstant("'test string' | 'urn:oid:3.4.5.6.7.8'", "string[]");
        }


        [TestMethod]
        public void VerifyIntegerConstant()
        {
            VerifyConstant("0 | 45", "integer[]");
        }

        [TestMethod]
        public void VerifyDecimalConstant()
        {
            VerifyConstant("0.0 | 3.14159265", "decimal[]");
        }

        [TestMethod]
        public void VerifyDateConstant()
        {
            VerifyConstant("@2015-02-04", "date");
        }

        [TestMethod]
        public void VerifyDateTimeConstant()
        {
            VerifyConstant("@2015-02-04T14:34:28+09:00", "dateTime");
        }

        [TestMethod]
        public void VerifyTimeConstant()
        {
            VerifyConstant("@T14:34:28", "time");
        }

        [TestMethod]
        public void VerifyQuantityConstant()
        {
            VerifyConstant("10 'mg' | 4 days", "Quantity[]");
        }

        [TestMethod]
        public void VerifyTypeConstant()
        {
            VerifyConstant("deceased.as(boolean)", "boolean");
        }

        [TestMethod]
        public void VerifyTypeConstant2()
        {
            VerifyConstant("deceased.ofType(boolean)", "boolean");
        }
    }
}