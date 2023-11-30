using Hl7.Fhir.FhirPath.Validator;
using Hl7.Fhir.Introspection;
using Hl7.FhirPath.Expressions;
using Hl7.FhirPath;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Model;
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Hl7.Fhir.Specification.Source;
using System.Collections.Generic;
using System.Linq;
using Hl7.Fhir.Rest;

namespace Test.Fhir.FhirPath.Validator
{
    [TestClass]
    public class ValidateIGs
    {
        private readonly ModelInspector _mi = ModelInspector.ForAssembly(typeof(Patient).Assembly);
        FhirPathCompiler _compiler;
		public static ZipSource _source = ZipSource.CreateValidationSource();

		private ExtensionResolvingFhirPathExpressionVisitor CreateFhirPathValidator(string packageId)
		{
			CachedResolver _cachedSource = new CachedResolver(new MultiResolver(_source, _ig_source[packageId]));
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

        //[TestMethod]
        //public async System.Threading.Tasks.Task PackageCacheUsCareIG()
        //{
        //    var packRef = new Firely.Fhir.Packages.PackageReference("hl7.fhir.us.core", "6.1.0");
        //    var cache = new Firely.Fhir.Packages.DiskPackageCache();
        //    Console.WriteLine(cache.PackageContentFolder(packRef));
        //    var cc = await cache.GetPackageReferences();
        //    //if (!await cache.IsInstalled(packRef))
        //    //{
        //    //    await cache.PackageContentFolder()
        //    //}
        //    //var pc = Firely.Fhir.Packages.PackageClient.Create();
        //    //var package = await pc.GetPackage(new Firely.Fhir.Packages.PackageReference("hl7.fhir.us.core", "6.1.0"));
        //    //string folder = @"c:\temp\uscore";
        //    //await Firely.Fhir.Packages.Packaging.UnpackToFolder(package, folder);

        //    var packRefSdc = new Firely.Fhir.Packages.PackageReference("hl7.fhir.us.sdc", "3.0.0");
        //    Console.WriteLine(cache.PackageContentFolder(packRefSdc));

        //    var content = InvariantsInUsCoreIG;
        //}

        //[TestMethod]
        //public async System.Threading.Tasks.Task DownloadUsCareIG()
        //{
        //    var pc = Firely.Fhir.Packages.PackageClient.Create();
        //    var package = await pc.GetPackage(new Firely.Fhir.Packages.PackageReference("hl7.fhir.us.core", "6.1.0"));
        //    string folder = @"c:\temp\uscore";
        //    await Firely.Fhir.Packages.Packaging.UnpackToFolder(package, folder);
        //}
        private static Dictionary<string, CommonDirectorySource> _ig_source = new Dictionary<string, CommonDirectorySource>();

        public static IEnumerable<object[]> InvariantsInIG(string packageId, string packageVersion, string[] knownBadInvariants)
        {
            var result = new List<object[]>();

            ModelInspector mi = ModelInspector.ForAssembly(typeof(Patient).Assembly);
            mi.Import(typeof(StructureDefinition).Assembly);

            var cache = new Firely.Fhir.Packages.DiskPackageCache();
            var packRef = new Firely.Fhir.Packages.PackageReference(packageId, packageVersion);
            string folder = cache.PackageContentFolder(packRef);
            if (!cache.IsInstalled(packRef).Result)
            {
                var pc = Firely.Fhir.Packages.PackageClient.Create();
                var content = pc.GetPackage(packRef).Result;
                Firely.Fhir.Packages.Packaging.UnpackToFolder(content, folder.Replace("\\package", "")).Wait();
                cache.Install(packRef, content).Wait();
            }

            CommonDirectorySource source = new CommonDirectorySource(mi, folder, new DirectorySourceSettings() { IncludeSubDirectories = true });
            if (!_ig_source.ContainsKey(packageId))
                _ig_source.Add(packageId, source);
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
                                    result.Add(new object[] { ed.Path, c.Key, c.Expression, !knownBadInvariants.Contains($"{sd.Url} {c.Key}"), sd.Url });
                            }
                        }
                    }
                }
            }
            return result;
        }

        public static IEnumerable<object[]> InvariantsInUsCoreIG
        {
            get
            {
                var knownBadInvariants = new[] {
                    "http://hl7.org/fhir/us/core/StructureDefinition/us-core-smokingstatus us-core-3",
                    "http://hl7.org/fhir/us/core/StructureDefinition/us-core-observation-lab us-core-4",
                    "http://hl7.org/fhir/us/core/StructureDefinition/us-core-observation-clinical-result us-core-3",
                    "http://hl7.org/fhir/us/core/StructureDefinition/us-core-practitionerrole us-core-13",
                };

                return InvariantsInIG("hl7.fhir.us.core", "6.1.0", knownBadInvariants);
            }
        }

        [TestMethod]
        [DynamicData(nameof(InvariantsInUsCoreIG))]
        public void VerifyUsCoreExpression(string type, string key, string expression, bool expectSuccess, string canonical)
        {
			if (!_ig_source.ContainsKey("hl7.fhir.us.core"))
			{
				var t = InvariantsInUsCoreIG;
			}
            // string expression = "(software.empty() and implementation.empty()) or kind != 'requirements'";
            Console.WriteLine($"Context: {type}");
            Console.WriteLine($"Canonical: {canonical}");
            Console.WriteLine($"Invariant key: {key}");
            Console.WriteLine($"Expression:\r\n{expression}");
            Console.WriteLine("---------");
            var visitor = CreateFhirPathValidator("hl7.fhir.us.core");
            visitor.SetContext(type);
			var source = _ig_source["hl7.fhir.us.core"];
			var res = source.ResolveByCanonicalUri(canonical);
			if (res is StructureDefinition sd && sd.Type == "Extension")
				visitor.SetContextExtension(sd);
            var pe = _compiler.Parse(expression);
            var r = pe.Accept(visitor);
            Console.WriteLine($"Result: {r}");
            Console.WriteLine("---------");

            Console.WriteLine(visitor.ToString());
            Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
            Assert.IsTrue((visitor.Outcome.Success && visitor.Outcome.Warnings == 0) == expectSuccess);
            Assert.AreEqual("boolean", r.ToString(), "Invariants must return a boolean");
        }

        public static IEnumerable<object[]> InvariantsInSdcIG
        {
            get
            {
                var knownBadInvariants = new[] {
                    "http://hl7.org/fhir/us/core/StructureDefinition/us-core-smokingstatus us-core-3",
                    "http://hl7.org/fhir/uv/sdc/StructureDefinition/sdc-task sdc-t1",
                };

                return InvariantsInIG("hl7.fhir.uv.sdc", "3.0.0", knownBadInvariants);
            }
        }

        [TestMethod]
        [DynamicData(nameof(InvariantsInSdcIG))]
        public void VerifySdcExpression(string type, string key, string expression, bool expectSuccess, string canonical)
        {
			if (!_ig_source.ContainsKey("hl7.fhir.uv.sdc"))
			{
				var t = InvariantsInSdcIG;
			}
            // string expression = "(software.empty() and implementation.empty()) or kind != 'requirements'";
            Console.WriteLine($"Context: {type}");
            Console.WriteLine($"Canonical: {canonical}");
            Console.WriteLine($"Invariant key: {key}");
            Console.WriteLine($"Expression:\r\n{expression}");
            Console.WriteLine("---------");
            var visitor = CreateFhirPathValidator("hl7.fhir.uv.sdc");
            visitor.SetContext(type);
            var source = _ig_source["hl7.fhir.uv.sdc"];
            var res = source.ResolveByCanonicalUri(canonical);
            if (res is StructureDefinition sd && sd.Type == "Extension")
                visitor.SetContextExtension(sd);
            var pe = _compiler.Parse(expression);
            var r = pe.Accept(visitor);
            Console.WriteLine($"Result: {r}");
            Console.WriteLine("---------");

            Console.WriteLine(visitor.ToString());
            Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
            Assert.IsTrue(visitor.Outcome.Success == expectSuccess);
            Assert.AreEqual("boolean", r.ToString(), "Invariants must return a boolean");
        }

        public static IEnumerable<object[]> InvariantsInAuBaseIG
        {
            get
            {
                var knownBadInvariants = new[] {
					"http://hl7.org.au/fhir/StructureDefinition/identifier-routability inv-routability-preference",
                };

                return InvariantsInIG("hl7.fhir.au.base", "4.1.0", knownBadInvariants);
            }
        }

        [TestMethod]
        [DynamicData(nameof(InvariantsInAuBaseIG))]
        public void VerifyAuBaseExpression(string type, string key, string expression, bool expectSuccess, string canonical)
        {
			if (!_ig_source.ContainsKey("hl7.fhir.au.base"))
			{
				var t = InvariantsInAuBaseIG;
			}
            // string expression = "(software.empty() and implementation.empty()) or kind != 'requirements'";
            Console.WriteLine($"Context: {type}");
            Console.WriteLine($"Canonical: {canonical}");
            Console.WriteLine($"Invariant key: {key}");
            Console.WriteLine($"Expression:\r\n{expression}");
            Console.WriteLine("---------");
            var visitor = CreateFhirPathValidator("hl7.fhir.au.base");

            visitor.SetContext(type);
			var source = _ig_source["hl7.fhir.au.base"];
			var res = source.ResolveByCanonicalUri(canonical);
			if (res is StructureDefinition sd && sd.Type == "Extension")
				visitor.SetContextExtension(sd);
            var pe = _compiler.Parse(expression);
            var r = pe.Accept(visitor);
            Console.WriteLine($"Result: {r}");
            Console.WriteLine("---------");

            Console.WriteLine(visitor.ToString());
            Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
            Assert.IsTrue(visitor.Outcome.Success == expectSuccess);
            Assert.AreEqual("boolean", r.ToString(), "Invariants must return a boolean");
        }

        public static IEnumerable<object[]> InvariantsInChCoreIG
        {
            get
            {
                var knownBadInvariants = new[] {
                    "http://fhir.ch/ig/ch-core/StructureDefinition/ch-core-patient ch-pat-3",
                };

                return InvariantsInIG("ch.fhir.ig.ch-core", "4.0.0-ballot", knownBadInvariants);
            }
        }

        [TestMethod]
        [DynamicData(nameof(InvariantsInChCoreIG))]
        public void VerifyChCoreExpression(string type, string key, string expression, bool expectSuccess, string canonical)
        {
			if (!_ig_source.ContainsKey("ch.fhir.ig.ch-core"))
			{
				var t = InvariantsInChCoreIG;
			}
            // string expression = "(software.empty() and implementation.empty()) or kind != 'requirements'";
            Console.WriteLine($"Context: {type}");
            Console.WriteLine($"Canonical: {canonical}");
            Console.WriteLine($"Invariant key: {key}");
            Console.WriteLine($"Expression:\r\n{expression}");
            Console.WriteLine("---------");
            var visitor = CreateFhirPathValidator("ch.fhir.ig.ch-core");
            visitor.SetContext(type);
			var source = _ig_source["ch.fhir.ig.ch-core"];
			var res = source.ResolveByCanonicalUri(canonical);
			if (res is StructureDefinition sd && sd.Type == "Extension")
				visitor.SetContextExtension(sd);
            var pe = _compiler.Parse(expression);
            var r = pe.Accept(visitor);
            Console.WriteLine($"Result: {r}");
            Console.WriteLine("---------");

            Console.WriteLine(visitor.ToString());
            Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
            Assert.IsTrue(visitor.Outcome.Success == expectSuccess);
            Assert.AreEqual("boolean", r.ToString(), "Invariants must return a boolean");
        }

        public static IEnumerable<object[]> InvariantsInSdohClinicalCareIG
        {
            get
            {
                var knownBadInvariants = new[] {
                    "http://hl7.org/fhir/us/core/StructureDefinition/us-core-smokingstatus us-core-3",
                };

                return InvariantsInIG("hl7.fhir.us.sdoh-clinicalcare", "dev", knownBadInvariants);
            }
        }

        [TestMethod]
        [DynamicData(nameof(InvariantsInSdohClinicalCareIG))]
        public void VerifySdohClinicalCareExpression(string type, string key, string expression, bool expectSuccess, string canonical)
        {
            if (!_ig_source.ContainsKey("hl7.fhir.us.sdoh-clinicalcare"))
            {
                var t= InvariantsInSdohClinicalCareIG;
			}
            // string expression = "(software.empty() and implementation.empty()) or kind != 'requirements'";
            Console.WriteLine($"Context: {type}");
            Console.WriteLine($"Canonical: {canonical}");
            Console.WriteLine($"Invariant key: {key}");
            Console.WriteLine($"Expression:\r\n{expression}");
            Console.WriteLine("---------");
            var visitor = CreateFhirPathValidator("hl7.fhir.us.sdoh-clinicalcare");
            visitor.SetContext(type);
			var source = _ig_source["hl7.fhir.us.sdoh-clinicalcare"];
			var res = source.ResolveByCanonicalUri(canonical);
			if (res is StructureDefinition sd && sd.Type == "Extension")
				visitor.SetContextExtension(sd);
            var pe = _compiler.Parse(expression);
            var r = pe.Accept(visitor);
            Console.WriteLine($"Result: {r}");
            Console.WriteLine("---------");

            Console.WriteLine(visitor.ToString());
            Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
            Assert.IsTrue(visitor.Outcome.Success == expectSuccess);
            Assert.AreEqual("boolean", r.ToString(), "Invariants must return a boolean");
        }
    }
}