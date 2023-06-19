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
    public class SearchParametersFromR4
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
                        result.Add(new object[] {
                            spd.Resource,
                            spd.Name,
                            spd.Expression,
                            spd.Type,
                            true,
                            spd.Url
                        });
                }
                return result;
            }
        }

        [TestMethod]
        [DynamicData(nameof(R4Expressions))]
        public void R4Expr(string type, string key, string expression, SearchParamType searchType, bool expectSuccess, string url)
        {
            // string expression = "(software.empty() and implementation.empty()) or kind != 'requirements'";
            Console.WriteLine($"Context: {type}");
            Console.WriteLine($"Search Param Name: {key}");
            Console.WriteLine($"Search Param Type: {searchType}");
            Console.WriteLine($"Expression:\r\n{expression}");
            Console.WriteLine($"Canonical:\r\n{url}");
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
            Assert.IsTrue(r.ToString().Length > 0);
            foreach (var returnType in r.ToString().Replace("[]", "").Split(", "))
            {
                switch (searchType)
                {
                    case SearchParamType.Number:
                        Assert.IsTrue(NumberTypes.Contains(returnType), $"Search Type mismatch {searchType} type on {returnType}");
                        break;
                    case SearchParamType.Date:
                        Assert.IsTrue(DateTypes.Contains(returnType), $"Search Type mismatch {searchType} type on {returnType}");
                        break;
                    case SearchParamType.String:
                        Assert.IsTrue(StringTypes.Contains(returnType), $"Search Type mismatch {searchType} type on {returnType}");
                        break;
                    case SearchParamType.Token:
                        Assert.IsTrue(TokenTypes.Contains(returnType), $"Search Type mismatch {searchType} type on {returnType}");
                        break;
                    case SearchParamType.Reference:
                        Assert.IsTrue(ReferenceTypes.Contains(returnType) || _mi.IsKnownResource(returnType), $"Search Type mismatch {searchType} type on {returnType}");
                        break;
                    case SearchParamType.Quantity:
                        Assert.IsTrue(QuantityTypes.Contains(returnType), $"Search Type mismatch {searchType} type on {returnType}");
                        break;
                    case SearchParamType.Uri:
                        Assert.IsTrue(UriTypes.Contains(returnType), $"Search Type mismatch {searchType} type on {returnType}");
                        break;
                    case SearchParamType.Composite:
                        // Assert.Inconclusive($"Need to verify search {searchType} type on {returnType}");
                        break;
                    case SearchParamType.Special:
                        // No real way to verify this special type
                        // Assert.Inconclusive($"Need to verify search {searchType} type on {returnType}");
                        break;
                }
            }
        }

        readonly string[] QuantityTypes = {
            "Quantity",
            "Money",
            "Range",
            "Duration",
            "Age",
        };

        readonly string[] TokenTypes = {
            "Identifier",
            "code",
            "CodeableConcept",
            "Coding",
            "string",
            "boolean",
            "id",
            "ContactPoint",
            "uri",
        };

        readonly string[] StringTypes = {
            "markdown",
            "string",
            "Address",
            "HumanName",
        };

        readonly string[] NumberTypes = {
            "decimal",
            "integer",
        };

        readonly string[] ReferenceTypes = {
            "Reference",
            "canonical",
            "uri",
        };

        readonly string[] UriTypes = {
            "uri",
            "url",
            "canonical",
        };

        readonly string[] DateTypes = {
            "dateTime",
            "date",
            "Period",
            "instant",
            "Timing",
        };

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