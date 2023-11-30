using Hl7.Fhir.FhirPath.Validator;
using Hl7.Fhir.Introspection;
using Hl7.FhirPath.Expressions;
using Hl7.FhirPath;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Model;
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using Hl7.Fhir.Specification.Source;

namespace Test.Fhir.FhirPath.Validator
{
    [TestClass]
    public class InvariantsFromR4
    {
        static private readonly ModelInspector _mi = ModelInspector.ForAssembly(typeof(Patient).Assembly);
        static FhirPathCompiler _compiler;
        public static ZipSource _source = ZipSource.CreateValidationSource();
		private readonly CachedResolver _cachedSource = new CachedResolver(_source);

		private BaseFhirPathExpressionVisitor CreateFhirPathValidator()
		{
			return new ExtensionResolvingFhirPathExpressionVisitor(_cachedSource, _mi, Hl7.Fhir.Model.ModelInfo.SupportedResources,
				  Hl7.Fhir.Model.ModelInfo.OpenTypes);
		}

		[TestInitialize]
        public void Init()
        {
            // include all the conformance types
            _mi.Import(typeof(StructureDefinition).Assembly);

            Hl7.Fhir.FhirPath.ElementNavFhirExtensions.PrepareFhirSymbolTableFunctions();
            SymbolTable symbolTable = new(FhirPathCompiler.DefaultSymbolTable);
            _compiler = new FhirPathCompiler(symbolTable);
        }

        // [TestMethod]
        public void ReadAllInvariants()
        {
			ZipSource _source = ZipSource.CreateValidationSource();
			_source.Prepare();
			if (_source.ListSummaries().Count() == 0)
			{
				// Need to re-create the set!
				System.IO.Directory.Delete(_source.ExtractPath, true);
				_source = ZipSource.CreateValidationSource();
				_source.Prepare();
			}
            foreach (var item in _source.ListSummaries().Where(s => s.ResourceTypeName == "StructureDefinition")) 
            {
                var sd = _source.ResolveByUri(item.ResourceUri) as StructureDefinition;
                if (sd != null && sd.Kind == StructureDefinition.StructureDefinitionKind.Resource)
                {
                    var elements = sd.Differential.Element.Where(e => e.Constraint.Any()).ToList();
                    if (elements.Any())
                    {
                        // Console.WriteLine($"Resource: {sd.Name}");
                        foreach (var ed in elements)
                        {
                            Console.WriteLine($"{ed.Path}");
                            foreach (var c in ed.Constraint)
                            {
                                Console.WriteLine($"\t{c.Key}:\t {c.Expression}");
                                //var visitor = new FhirPathExpressionVisitor();
                                //var t = SelectType(ed.Path, out var rt);
                                //if (t != rt)
                                //{
                                //}
                                //if (t != null)
                                //    visitor.RegisterVariable("context", t);
                                //visitor.AddInputType(t);
                                //if (rt.IsAssignableTo(typeof(Resource)))
                                //    visitor.RegisterVariable("resource", rt);
                                //else
                                //    visitor.RegisterVariable("resource", typeof(Resource));
                                //var pe = _compiler.Parse(c.Expression);
                                //var r = pe.Accept(visitor);
                                //Console.WriteLine($"Result: {r}");
                                //Console.WriteLine("---------");

                                //Console.WriteLine(visitor.ToString());
                                //Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
                                //Assert.IsTrue(visitor.Outcome.Success == true);
                            }
                        }
                    }
                }

            }
        }

        public static IEnumerable<object[]> R4Invariants
        {
            get
            {
                var result = new List<object[]>();
                var knownBadInvariants = new[] {
                    "Evidence cnl-0",
                    "EvidenceReport cnl-0",
                    "ChargeItemDefinition cid-0",
                    "StructureDefinition.snapshot sdf-24",
                    "StructureDefinition.snapshot sdf-25",
					"Ingredient ing-1",
                };

                ZipSource source = ZipSource.CreateValidationSource();
                foreach (var item in source.ListSummaries().Where(s => s.ResourceTypeName == "StructureDefinition"))
                {
                    var sd = source.ResolveByUri(item.ResourceUri) as StructureDefinition;
                    if (sd != null && sd.Kind == StructureDefinition.StructureDefinitionKind.Resource && sd.Abstract == false)
                    {
                        var elements = sd.Differential.Element.Where(e => e.Constraint.Any()).ToList();
                        if (elements.Any())
                        {
                            foreach (var ed in elements)
                            {
                                foreach (var c in ed.Constraint)
                                {
                                    if (!string.IsNullOrEmpty(c.Expression))
                                        result.Add(new object[] { ed.Path, c.Key, c.Expression, !knownBadInvariants.Contains($"{ed.Path} {c.Key}") });
                                }
                            }
                        }
                    }
                }
                return result;
            }
        }

        [TestMethod]
        [DynamicData(nameof(R4Invariants))]
        public void TestR4Invariants(string path, string key, string expression, bool expectSuccess)
        {
            // string expression = "(software.empty() and implementation.empty()) or kind != 'requirements'";
            Console.WriteLine($"Context: {path}");
            Console.WriteLine($"Invariant key: {key}");
            Console.WriteLine($"Expression:\r\n{expression}");

            Console.WriteLine("---------");
            var visitor = CreateFhirPathValidator();
            visitor.SetContext(path);
            var pe = _compiler.Parse(expression);
            var r = pe.Accept(visitor);
            Console.WriteLine($"Result: {r}");
            Console.WriteLine("---------");

            Console.WriteLine(visitor.ToString());
            Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
            Assert.IsTrue(visitor.Outcome.Success == expectSuccess);
            if (expectSuccess)
                Assert.AreEqual("boolean", r.ToString(), "Invariants must return a boolean");
        }
    }
}