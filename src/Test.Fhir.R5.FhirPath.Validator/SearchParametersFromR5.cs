using Hl7.Fhir.FhirPath.Validator;
using Hl7.Fhir.Introspection;
using Hl7.FhirPath.Expressions;
using Hl7.FhirPath;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Model;
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Collections.Generic;

namespace Test.Fhir.FhirPath.Validator
{
    [TestClass]
    public class SearchParametersFromR5
    {
        private readonly ModelInspector _mi = ModelInspector.ForAssembly(typeof(Patient).Assembly);
        FhirPathCompiler _compiler;

        [TestInitialize]
        public void Init()
        {
            // include all the conformance types
            _mi.Import(typeof(StructureDefinition).Assembly);

            Hl7.Fhir.FhirPath.ElementNavFhirExtensions.PrepareFhirSymbolTableFunctions();
            SymbolTable symbolTable = new(FhirPathCompiler.DefaultSymbolTable);
            _compiler = new FhirPathCompiler(symbolTable);
        }

        public static IEnumerable<object[]> R4Expressions
        {
            get
            {
                var knownBadOutcomes = new[] {
                    "http://hl7.org/fhir/SearchParameter/Observation-component-value-canonical",
                    "http://hl7.org/fhir/SearchParameter/Observation-value-canonical",
                    "http://hl7.org/fhir/SearchParameter/Observation-value-markdown",
                };
                var knownBadSearchParams = new[] {
                    "http://hl7.org/fhir/SearchParameter/Observation-value-quantity",
                    "http://hl7.org/fhir/SearchParameter/Observation-value-canonical",
                    "http://hl7.org/fhir/SearchParameter/Observation-component-value-quantity",
                    "http://hl7.org/fhir/SearchParameter/Observation-value-markdown",
                    "http://hl7.org/fhir/SearchParameter/Observation-component-value-canonical",
                    "http://hl7.org/fhir/SearchParameter/MedicinalProductDefinition-characteristic",
                    "http://hl7.org/fhir/SearchParameter/Observation-combo-value-quantity",

                    // Bad composites
                    "http://hl7.org/fhir/SearchParameter/Device-code-value-concept",
                    "http://hl7.org/fhir/SearchParameter/DeviceDefinition-specification-version",
                    "http://hl7.org/fhir/SearchParameter/DocumentReference-relationship",
                    "http://hl7.org/fhir/SearchParameter/Encounter-location-period",
                    "http://hl7.org/fhir/SearchParameter/Ingredient-strength-concentration-ratio",
                    "http://hl7.org/fhir/SearchParameter/Ingredient-strength-presentation-ratio",
                    "http://hl7.org/fhir/SearchParameter/Observation-code-value-string",
                    "http://hl7.org/fhir/SearchParameter/TestScript-scope-artifact-conformance",
                    "http://hl7.org/fhir/SearchParameter/TestScript-scope-artifact-phase",

                    // this one isn't really bad, we don't resolve the extension to discover if the value is restricted
                    "http://hl7.org/fhir/SearchParameter/CareTeam-name", 
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
                            spd.Expression = "AdverseEvent.occurrence.ofType(dateTime) | AdverseEvent.occurrence.ofType(Period) | AdverseEvent.occurrence.ofType(Timing) | AllergyIntolerance.recordedDate | (Appointment.start | Appointment.requestedPeriod.start).first() | AuditEvent.recorded | CarePlan.period | ClinicalImpression.date | Composition.date | Consent.date | DiagnosticReport.effective.ofType(dateTime) | DiagnosticReport.effective.ofType(Period) | DocumentReference.date | Encounter.actualPeriod | EpisodeOfCare.period | FamilyMemberHistory.date | Flag.period | (Immunization.occurrence.ofType(dateTime)) | ImmunizationEvaluation.date | ImmunizationRecommendation.date | Invoice.date | List.date | MeasureReport.date | NutritionIntake.occurrence.ofType(dateTime) | NutritionIntake.occurrence.ofType(Period) | Observation.effective.ofType(dateTime) | Observation.effective.ofType(Period) | Observation.effective.ofType(Timing) | Observation.effective.ofType(instant) | Procedure.occurrence.ofType(dateTime) | Procedure.occurrence.ofType(Period) | Procedure.occurrence.ofType(Timing) | ResearchSubject.period | (RiskAssessment.occurrence.ofType(dateTime)) | SupplyRequest.authoredOn";
                        // Spec is wrong with the intended search type here
                        if (spd.Url == "http://hl7.org/fhir/SearchParameter/Device-serial-number")
                            spd.Type = SearchParamType.Token;
                        if (knownBadOutcomes.Contains(spd.Url))
                            expectedOutcomeResult = false;
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
            var t = _mi.GetTypeForFhirType(type);
            if (t != null)
            {
                visitor.RegisterVariable("context", t);
            visitor.AddInputType(t);
                visitor.RegisterVariable("resource", t);
        }
            VerifyExpression(t, expression, searchType, expectSuccessOutcome, expectValidSearch, spd, visitor);
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
    }
}