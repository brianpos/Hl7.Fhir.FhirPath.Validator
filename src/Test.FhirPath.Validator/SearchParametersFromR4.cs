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
                var knownBadSearchParams = new[] {
                    "http://hl7.org/fhir/SearchParameter/clinical-date",
                    "http://hl7.org/fhir/SearchParameter/CarePlan-activity-date",
                    "http://hl7.org/fhir/SearchParameter/MedicinalProductDefinition-characteristic",
                    "http://hl7.org/fhir/SearchParameter/ClinicalUseDefinition-contraindication",
                    "http://hl7.org/fhir/SearchParameter/ClinicalUseDefinition-contraindication-reference",
                    "http://hl7.org/fhir/SearchParameter/ClinicalUseDefinition-effect",
                    "http://hl7.org/fhir/SearchParameter/ClinicalUseDefinition-effect-reference",
                    "http://hl7.org/fhir/SearchParameter/ClinicalUseDefinition-indication",
                    "http://hl7.org/fhir/SearchParameter/ClinicalUseDefinition-indication-reference",
                    "http://hl7.org/fhir/SearchParameter/Consent-source-reference",
                    "http://hl7.org/fhir/SearchParameter/DocumentReference-relationship",
                    "http://hl7.org/fhir/SearchParameter/Ingredient-manufacturer",
                    "http://hl7.org/fhir/SearchParameter/Observation-combo-value-quantity",
                    "http://hl7.org/fhir/SearchParameter/Observation-component-value-quantity",
                    "http://hl7.org/fhir/SearchParameter/Observation-value-quantity",
                    "http://hl7.org/fhir/SearchParameter/RiskAssessment-probability",
                };
                var result = new List<object[]>();
                foreach (var spd in ModelInfo.SearchParameters)
                {
                    if (!string.IsNullOrEmpty(spd.Expression))
                    {
                        bool expectedOutcomeResult = true;
                        bool expectedResult = true;

                        // Workaround for the Appointment search parameter fragments that don't prefix their resource type
                        // which then fails if you try to perform static analysis on other resource types
                        if (spd.Url == "http://hl7.org/fhir/SearchParameter/clinical-date")
                            spd.Expression = "AllergyIntolerance.recordedDate | CarePlan.period | CareTeam.period | ClinicalImpression.date | Composition.date | Consent.dateTime | DiagnosticReport.effective | Encounter.period | EpisodeOfCare.period | FamilyMemberHistory.date | Flag.period | (Immunization.occurrence as dateTime) | List.date | Observation.effective | Procedure.performed | (RiskAssessment.occurrence as dateTime) | SupplyRequest.authoredOn";

                        if (knownBadSearchParams.Contains(spd.Url))
                            expectedResult = false;

                        result.Add(new object[] {
                            spd.Resource,
                            spd.Name,
                            spd.Expression,
                            spd.Type,
                            expectedOutcomeResult,
                            expectedResult,
                            spd.Url,
                            spd
                        });
                    }
                }
                return result;
            }
        }

        [TestMethod]
        [DynamicData(nameof(R4Expressions))]
        public void R4Expr(string type, string key, string expression, SearchParamType searchType, bool expectSuccessOutcome, bool expectValidSearch, string url, ModelInfo.SearchParamDefinition spd)
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
            if (!rt.IsAssignableTo(typeof(Resource)))
                rt = typeof(Resource);
            visitor.RegisterVariable("resource", rt);
            VerifyExpression(rt, expression, searchType, expectSuccessOutcome, expectValidSearch, spd, visitor);
        }

        private void VerifyExpression(Type resourceType, string expression, SearchParamType searchType, bool expectSuccessOutcome, bool expectValidSearch, ModelInfo.SearchParamDefinition spd, FhirPathExpressionVisitor visitor)
        {
            var pe = _compiler.Parse(expression);
            var r = pe.Accept(visitor);
            Console.WriteLine($"Result: {r}");
            Console.WriteLine("---------");

            Console.WriteLine(visitor.ToString());
            Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
            Assert.IsTrue(visitor.Outcome.Success == expectSuccessOutcome);

            if (expectValidSearch)
            {
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
                            // Need to feed this back into itself to verify
                            foreach (var cp in spd.Component)
                            {
                                // resolve the composite canonical to work out what type it should be
                                var componentSearchParameterType = ModelInfo.SearchParameters.Where(sp => sp.Url == cp.Definition).FirstOrDefault()?.Type;
                                Assert.IsNotNull(componentSearchParameterType, $"Failed to resolve component URL: {cp.Definition}");
                                foreach (var type in r.Types)
                                {
                                    var visitorComponent = new FhirPathExpressionVisitor();
                                    visitorComponent.RegisterVariable("resource", resourceType);
                                    visitorComponent.RegisterVariable("context", type.ClassMapping);
                                    visitorComponent.AddInputType(type.ClassMapping);
                                    VerifyExpression(
                                        resourceType,
                                        cp.Expression,
                                        componentSearchParameterType.Value,
                                        expectSuccessOutcome,
                                        expectValidSearch,
                                        null,
                                        visitorComponent);
                                }
                            }
                            break;
                        case SearchParamType.Special:
                            // No real way to verify this special type
                            // Assert.Inconclusive($"Need to verify search {searchType} type on {returnType}");
                            break;
                    }
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
            "canonical",
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