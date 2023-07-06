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
    public class SearchParametersFromR4
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
                    // use of "as" on a collection
                    "http://hl7.org/fhir/SearchParameter/Medication-ingredient-code",
                    "http://hl7.org/fhir/SearchParameter/MedicationKnowledge-ingredient-code",
                    "http://hl7.org/fhir/SearchParameter/Observation-component-value-concept",
                    "http://hl7.org/fhir/SearchParameter/Observation-component-value-quantity",
                    "http://hl7.org/fhir/SearchParameter/Substance-substance-reference",
                    "http://hl7.org/fhir/SearchParameter/Composition-related-ref",
                    "http://hl7.org/fhir/SearchParameter/Goal-target-date",
                    "http://hl7.org/fhir/SearchParameter/Medication-ingredient",
                    "http://hl7.org/fhir/SearchParameter/Observation-combo-value-concept",
                    "http://hl7.org/fhir/SearchParameter/Observation-combo-value-quantity",
                    "http://hl7.org/fhir/SearchParameter/Group-value",
                    "http://hl7.org/fhir/SearchParameter/Substance-code",
                    "http://hl7.org/fhir/SearchParameter/MedicationKnowledge-ingredient",
                    "http://hl7.org/fhir/SearchParameter/Composition-related-id",
                };
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

                        // Workaround for http://hl7.org/fhir/SearchParameter/conformance-context using "as" on collections
                        if (spd.Url == "http://hl7.org/fhir/SearchParameter/conformance-context")
                            spd.Expression = "(CapabilityStatement.useContext.value.ofType(CodeableConcept)) | (CodeSystem.useContext.value.ofType(CodeableConcept)) | (CompartmentDefinition.useContext.value.ofType(CodeableConcept)) | (ConceptMap.useContext.value.ofType(CodeableConcept)) | (GraphDefinition.useContext.value.ofType(CodeableConcept)) | (ImplementationGuide.useContext.value.ofType(CodeableConcept)) | (MessageDefinition.useContext.value.ofType(CodeableConcept)) | (NamingSystem.useContext.value.ofType(CodeableConcept)) | (OperationDefinition.useContext.value.ofType(CodeableConcept)) | (SearchParameter.useContext.value.ofType(CodeableConcept)) | (StructureDefinition.useContext.value.ofType(CodeableConcept)) | (StructureMap.useContext.value.ofType(CodeableConcept)) | (TerminologyCapabilities.useContext.value.ofType(CodeableConcept)) | (ValueSet.useContext.value.ofType(CodeableConcept))";

                        if (spd.Expression == "(CapabilityStatement.useContext.value as Quantity) | (CapabilityStatement.useContext.value as Range) | (CodeSystem.useContext.value as Quantity) | (CodeSystem.useContext.value as Range) | (CompartmentDefinition.useContext.value as Quantity) | (CompartmentDefinition.useContext.value as Range) | (ConceptMap.useContext.value as Quantity) | (ConceptMap.useContext.value as Range) | (GraphDefinition.useContext.value as Quantity) | (GraphDefinition.useContext.value as Range) | (ImplementationGuide.useContext.value as Quantity) | (ImplementationGuide.useContext.value as Range) | (MessageDefinition.useContext.value as Quantity) | (MessageDefinition.useContext.value as Range) | (NamingSystem.useContext.value as Quantity) | (NamingSystem.useContext.value as Range) | (OperationDefinition.useContext.value as Quantity) | (OperationDefinition.useContext.value as Range) | (SearchParameter.useContext.value as Quantity) | (SearchParameter.useContext.value as Range) | (StructureDefinition.useContext.value as Quantity) | (StructureDefinition.useContext.value as Range) | (StructureMap.useContext.value as Quantity) | (StructureMap.useContext.value as Range) | (TerminologyCapabilities.useContext.value as Quantity) | (TerminologyCapabilities.useContext.value as Range) | (ValueSet.useContext.value as Quantity) | (ValueSet.useContext.value as Range)")
                            spd.Expression = "(CapabilityStatement.useContext.value.ofType(Quantity)) | (CapabilityStatement.useContext.value.ofType(Range)) | (CodeSystem.useContext.value.ofType(Quantity)) | (CodeSystem.useContext.value.ofType(Range)) | (CompartmentDefinition.useContext.value.ofType(Quantity)) | (CompartmentDefinition.useContext.value.ofType(Range)) | (ConceptMap.useContext.value.ofType(Quantity)) | (ConceptMap.useContext.value.ofType(Range)) | (GraphDefinition.useContext.value.ofType(Quantity)) | (GraphDefinition.useContext.value.ofType(Range)) | (ImplementationGuide.useContext.value.ofType(Quantity)) | (ImplementationGuide.useContext.value.ofType(Range)) | (MessageDefinition.useContext.value.ofType(Quantity)) | (MessageDefinition.useContext.value.ofType(Range)) | (NamingSystem.useContext.value.ofType(Quantity)) | (NamingSystem.useContext.value.ofType(Range)) | (OperationDefinition.useContext.value.ofType(Quantity)) | (OperationDefinition.useContext.value.ofType(Range)) | (SearchParameter.useContext.value.ofType(Quantity)) | (SearchParameter.useContext.value.ofType(Range)) | (StructureDefinition.useContext.value.ofType(Quantity)) | (StructureDefinition.useContext.value.ofType(Range)) | (StructureMap.useContext.value.ofType(Quantity)) | (StructureMap.useContext.value.ofType(Range)) | (TerminologyCapabilities.useContext.value.ofType(Quantity)) | (TerminologyCapabilities.useContext.value.ofType(Range)) | (ValueSet.useContext.value.ofType(Quantity)) | (ValueSet.useContext.value.ofType(Range))";

                        if (spd.Expression.Contains("useContext.value as Quantity"))
                            spd.Expression = spd.Expression.Replace(".useContext.value as Quantity", ".useContext.value.ofType(Quantity)");
                        if (spd.Expression.Contains("useContext.value as CodeableConcept"))
                            spd.Expression = spd.Expression.Replace(".useContext.value as CodeableConcept", ".useContext.value.ofType(CodeableConcept)");
                        if (spd.Expression.Contains("useContext.value as Range"))
                            spd.Expression = spd.Expression.Replace(".useContext.value as Range", ".useContext.value.ofType(Range)");

                        if (knownBadSearchParams.Contains(spd.Url))
                            expectedResult = false;

                        if (knownBadOutcomes.Contains(spd.Url))
                            expectedOutcomeResult = false;

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