﻿using Hl7.Fhir.FhirPath.Validator;
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
        static ZipSource _source = ZipSource.CreateValidationSource();

        [TestInitialize]
        public void Init()
        {
            Hl7.Fhir.FhirPath.ElementNavFhirExtensions.PrepareFhirSymbolTableFunctions();
            SymbolTable symbolTable = new(FhirPathCompiler.DefaultSymbolTable);
            _compiler = new FhirPathCompiler(symbolTable);
        }

        [TestMethod]
        public void ReadAllInvariants()
        {
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
                                        result.Add(new object[] { ed.Path, c.Key, c.Expression, true });
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

            if (expression.Contains("descendants()"))
                Assert.Fail("Checking does not support descendants()");

            Console.WriteLine("---------");
            var visitor = new FhirPathExpressionVisitor();
            var t = SelectType(path, out var rt);
            if (t != null)
                visitor.RegisterVariable("context", t);
            visitor.AddInputType(t);
            if (!rt.IsAssignableTo(typeof(Resource)))
                rt = typeof(Resource);
            visitor.RegisterVariable("resource", rt);
            visitor.RegisterVariable("rootResource", rt);
            var pe = _compiler.Parse(expression);
            var r = pe.Accept(visitor);
            Console.WriteLine($"Result: {r}");
            Console.WriteLine("---------");

            Console.WriteLine(visitor.ToString());
            Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
            Assert.IsTrue(visitor.Outcome.Success == expectSuccess);
        }

        static public Type? SelectType(string path, out Type? rootType)
        {
            path = path.Replace("[x]", "");
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

        static public Type? NavigateToProp(Type t, string path)
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