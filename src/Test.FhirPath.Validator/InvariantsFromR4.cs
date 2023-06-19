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

namespace Test.Fhir.FhirPath.Validator
{
    [TestClass]
    public class InvariantsFromR4
    {
        private readonly ModelInspector _mi = ModelInspector.ForAssembly(typeof(Patient).Assembly);
        FhirPathCompiler _compiler;

        [TestInitialize]
        public void Init()
        {
            Hl7.Fhir.FhirPath.ElementNavFhirExtensions.PrepareFhirSymbolTableFunctions();
            SymbolTable symbolTable = new(FhirPathCompiler.DefaultSymbolTable);
            _compiler = new FhirPathCompiler(symbolTable);
        }

        public static IEnumerable<object[]> R4Expressions
        {
            get
            {
                var result = new List<object[]>();
                foreach (var spd in ModelInfo.SearchParameters)
                {
                    if (!string.IsNullOrEmpty(spd.Expression))
                    result.Add(new object[] { spd.Resource,
                        spd.Name,
                    spd.Expression, true });
                }
                result.Add(new object[] { "Claim", "be-rule-eagreementclaim-2", "Claim.created.toString().length()=25", true });
                return result;
            }
        }

        [TestMethod]
        [DynamicData(nameof(R4Expressions))]
        public void R4Expr(string type, string key, string expression, bool expectSuccess)
        {
            // string expression = "(software.empty() and implementation.empty()) or kind != 'requirements'";
            Console.WriteLine($"Context: {type}");
            Console.WriteLine($"Invariant key: {key}");
            Console.WriteLine($"Expression:\r\n{expression}");
            Console.WriteLine("---------");
            var visitor = new FhirPathExpressionVisitor();
            var t = SelectType(type, out var rt);
            if (t != rt)
            {
            }
            if (t != null)
                visitor.RegisterVariable("context", t);
            visitor.AddInputType(t);
            if (rt.IsAssignableTo(typeof(Resource)))
                visitor.RegisterVariable("resource", rt);
            else
                visitor.RegisterVariable("resource", typeof(Resource));
            var pe = _compiler.Parse(expression);
            var r = pe.Accept(visitor);
            Console.WriteLine($"Result: {r}");
            Console.WriteLine("---------");

            Console.WriteLine(visitor.ToString());
            Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
            Assert.IsTrue(visitor.Outcome.Success == expectSuccess);
        }

        public Type? SelectType(string path, out Type? rootType)
        {
            string type = path;
            if (path.Contains('.'))
            {
                type = path.Substring(0, path.IndexOf("."));
                path = path.Substring(path.IndexOf(".") + 1);
            }
            else
            {
                path = null;
            }
            rootType = ModelInfo.GetTypeForFhirType(type);
            if (!string.IsNullOrEmpty(path) && rootType != null)
                return NavigateToProp(rootType, path);
            return rootType;
        }

        public Type? NavigateToProp(Type t, string path)
        {
            var nodes = path.Split(".").ToList();
            var cm = _mi.FindOrImportClassMapping(t);
            while (cm != null && nodes.Any())
            {
                var pm = cm.FindMappedElementByName(nodes[0]);
                if (pm == null)
                {
                    pm = cm.FindMappedElementByChoiceName(nodes[0]);
                    cm = _mi.FindOrImportClassMapping(pm.FhirType.First(t => nodes[0].EndsWith(t.Name)));
                }
                else
                {
                    cm = _mi.FindOrImportClassMapping(pm.FhirType.First());
                }
                nodes.RemoveAt(0);
            }
            return cm.NativeType;
        }
    }
}