using Hl7.Fhir.Introspection;
using Hl7.Fhir.Model;
using Hl7.FhirPath;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Hl7.Fhir.FhirPath.Validator
{
    /// <summary>
    /// Version Agnostic implementation of the SearchParameter class from the various versions of FHIR (although isn't FIHR and hasn't changed)
    /// </summary>
    [System.Diagnostics.DebuggerDisplay(@"\{{DebuggerDisplay,nq}}")] // http://blogs.msdn.com/b/jaredpar/archive/2011/03/18/debuggerdisplay-attribute-best-practices.aspx
    public class VersionAgnosticSearchParameter
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string DebuggerDisplay
        {
            get
            {
                return String.Format("{0} {1} {2} ({3})", Resource, Name, Type, Expression);
            }
        }

        public string Resource { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public string Url { get; set; }
        public SearchParamType Type { get; set; }

        /// <summary>
        /// The FHIR Path expresssion that can be used to extract the data
        /// for this search parameter
        /// </summary>
        public string Expression { get; set; }

        /// <summary>
        /// If this is a reference, the possible types of resources that the
        /// parameters references to
        /// </summary>
        public string[] Target { get; set; }

        /// <summary>
        /// Used to define the parts of a composite search parameter.
        /// </summary>
        public SearchParamComponent[] Component { get; set; }
    }

    /// <summary>
    /// Version Agnostic implementation used by the above VersionAgnosticSearchParameter class
    /// </summary>
    public struct SearchParamComponent
    {
        public string Definition { get; set; }
        public string Expression { get; set; }
    }


    public class SearchExpressionValidator
    {
        public SearchExpressionValidator(ModelInspector mi, List<string> SupportedResources, Type[] OpenTypes, Func<string, VersionAgnosticSearchParameter> ResolveSearchParameter)
        {
            _mi = mi;
            _supportedResources = SupportedResources;
            _openTypes = OpenTypes;

            _resolveSearchParameter = ResolveSearchParameter;

            var symbolTable = new Hl7.FhirPath.Expressions.SymbolTable(FhirPathCompiler.DefaultSymbolTable);
            _compiler = new FhirPathCompiler(symbolTable);
        }
        private readonly ModelInspector _mi;
        private readonly List<string> _supportedResources;
        private readonly Type[] _openTypes;
        private Func<string, VersionAgnosticSearchParameter> _resolveSearchParameter;
        FhirPathCompiler _compiler;

        public bool IncludeParseTreeDiagnostics { get; set; } = false;

        public IEnumerable<OperationOutcome.IssueComponent> Validate(string type, string code, string expression, SearchParamType searchType, string url, VersionAgnosticSearchParameter spd)
        {
            //Console.WriteLine($"Context: {type}");
            //Console.WriteLine($"Search Param Name: {code}");
            //Console.WriteLine($"Search Param Type: {searchType}");
            //Console.WriteLine($"Expression:\r\n{expression}");
            //Console.WriteLine($"Canonical:\r\n{url}");
            //Console.WriteLine("---------");
            var visitor = new BaseFhirPathExpressionVisitor(_mi, _supportedResources, _openTypes);
            var t = _mi.GetTypeForFhirType(type);
            if (t != null)
            {
                visitor.RegisterVariable("context", t);
                visitor.AddInputType(t);
                visitor.RegisterVariable("resource", t);
            }
            return VerifyExpression(t, code, expression, searchType, spd, visitor);
        }

        const string ErrorNamespace = "http://fhirpath-lab.com/CodeSystem/search-exp-errors";
        readonly static Coding SearchTypeMismatch = new(ErrorNamespace, "SE0001", "Search type data mismatch");
        readonly static Coding UnknownReturnType = new(ErrorNamespace, "SE0002", "Cannot evaluate return type of expression");
        readonly static Coding CannotResolveCanonical = new(ErrorNamespace, "SE0003", "Cannot resolve canonical for composite expression");

        private IEnumerable<OperationOutcome.IssueComponent> VerifyExpression(Type resourceType, string code, string expression, SearchParamType searchType, VersionAgnosticSearchParameter spd, BaseFhirPathExpressionVisitor visitor)
        {
            List<OperationOutcome.IssueComponent> results = new List<OperationOutcome.IssueComponent>();
            var pe = _compiler.Parse(expression);
            var r = pe.Accept(visitor);
            // Console.WriteLine($"Result: {r}");
            // Console.WriteLine("---------");

            // Console.WriteLine(visitor.ToString());
            // Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
            results.AddRange(visitor.Outcome.Issue);

            string diagnostics = $"Expression: {expression}\r\nReturn type: {r}";
            if (IncludeParseTreeDiagnostics)
                diagnostics += $"\r\nParse Tree:\r\n{visitor.ToString().Replace("\r\n\r\n", "\r\n")}";

            AssertIsTrue(results, UnknownReturnType, r.ToString().Length > 0, "Unable to determine the return type of the expression", diagnostics);
            foreach (var returnType in r.ToString().Replace("[]", "").Replace(" ", "").Split(','))
            {
                switch (searchType)
                {
                    case SearchParamType.Number:
                        AssertIsTrue(results, SearchTypeMismatch, NumberTypes.Contains(returnType), $"Search Type mismatch {searchType} type on {returnType}", diagnostics);
                        break;
                    case SearchParamType.Date:
                        AssertIsTrue(results, SearchTypeMismatch, DateTypes.Contains(returnType), $"Search Type mismatch {searchType} type on {returnType}", diagnostics);
                        break;
                    case SearchParamType.String:
                        AssertIsTrue(results, SearchTypeMismatch, StringTypes.Contains(returnType), $"Search Type mismatch {searchType} type on {returnType}", diagnostics);
                        break;
                    case SearchParamType.Token:
                        AssertIsTrue(results, SearchTypeMismatch, TokenTypes.Contains(returnType), $"Search Type mismatch {searchType} type on {returnType}", diagnostics);
                        break;
                    case SearchParamType.Reference:
                        AssertIsTrue(results, SearchTypeMismatch, ReferenceTypes.Contains(returnType) || _mi.IsKnownResource(returnType), $"Search Type mismatch {searchType} type on {returnType}", diagnostics);
                        break;
                    case SearchParamType.Quantity:
                        AssertIsTrue(results, SearchTypeMismatch, QuantityTypes.Contains(returnType), $"Search Type mismatch {searchType} type on {returnType}", diagnostics);
                        break;
                    case SearchParamType.Uri:
                        AssertIsTrue(results, SearchTypeMismatch, UriTypes.Contains(returnType), $"Search Type mismatch {searchType} type on {returnType}", diagnostics);
                        break;
                    case SearchParamType.Composite:
                        // Need to feed this back into itself to verify
                        foreach (var cp in spd.Component)
                        {
                            // resolve the composite canonical to work out what type it should be
                            var componentSearchParameter = _resolveSearchParameter(cp.Definition);
                            AssertIsTrue(results, CannotResolveCanonical, componentSearchParameter?.Type != null, $"Failed to resolve component URL: {cp.Definition}", diagnostics);
                            foreach (var type in r.Types)
                            {
                                var visitorComponent = new BaseFhirPathExpressionVisitor(_mi, _supportedResources, _openTypes);
                                visitorComponent.RegisterVariable("resource", resourceType);
                                visitorComponent.RegisterVariable("context", type.ClassMapping);
                                visitorComponent.AddInputType(type.ClassMapping);
                                results.AddRange(VerifyExpression(
                                    resourceType,
                                    componentSearchParameter.Code,
                                    cp.Expression,
                                    componentSearchParameter.Type,
                                    null,
                                    visitorComponent));
                            }
                        }
                        break;
                    case SearchParamType.Special:
                        // No real way to verify this special type
                        // Assert.Inconclusive($"Need to verify search {searchType} type on {returnType}");
                        break;
                }
            }
            return results;
        }

        private void AssertIsTrue(List<OperationOutcome.IssueComponent> results, Coding detail, bool testResult, string message, string diagnostics)
        {
            if (!testResult)
            {
                // Console.WriteLine(message);
                var issue = new Hl7.Fhir.Model.OperationOutcome.IssueComponent()
                {
                    Severity = Hl7.Fhir.Model.OperationOutcome.IssueSeverity.Error,
                    Code = Hl7.Fhir.Model.OperationOutcome.IssueType.Invalid,
                    Details = new Hl7.Fhir.Model.CodeableConcept(detail.System, detail.Code, detail.Display, message),
                    Diagnostics = diagnostics
                };
                results.Add(issue);
            }
        }

        // This list has been cross referenced with:
        // https://github.com/GinoCanessa/fhir-candle/blob/dev/src/FhirStore.CommonVersioned/Search/SearchDefinitions.cs

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
            "oid",
            "uri",
            "url",
            "uuid",
        };

        readonly string[] StringTypes = {
            "id",
            "markdown",
            "string",
            "Address",
            "HumanName",
            "xhtml",
        };

        readonly string[] NumberTypes = {
            "decimal",
            "integer",
            "unsignedInt",
            "positiveInt",
            "integer64",
        };

        readonly string[] ReferenceTypes = {
            "Reference",
            "canonical",
            "uri",
            "url",
            "oid",
            "uuid",
        };

        readonly string[] UriTypes = {
            "uri",
            "url",
            "canonical",
            "oid",
            "uuid",
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
