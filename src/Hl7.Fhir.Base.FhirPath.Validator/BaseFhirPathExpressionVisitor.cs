using Hl7.Fhir.Introspection;
using Hl7.Fhir.Model;
using Hl7.Fhir.Support;
using Hl7.Fhir.Utility;
using Hl7.FhirPath.Expressions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Hl7.Fhir.FhirPath.Validator
{
    public class BaseFhirPathExpressionVisitor : ExpressionVisitor<FhirPathVisitorProps>
    {
        public BaseFhirPathExpressionVisitor(ModelInspector mi, List<string> SupportedResources, Type[] OpenTypes)
        {
            _mi = mi;
            _supportedResources = SupportedResources;
            _openTypes = OpenTypes;

            // Register some FHIR Standard variables (const strings)
            RegisterVariable("ucum", typeof(Hl7.Fhir.Model.FhirString));
            RegisterVariable("sctt", typeof(Hl7.Fhir.Model.FhirString));

            _table = new SymbolTable(mi);
        }

        private SymbolTable _table;

        /// <summary>
        /// Set the Context of the expression to verify 
        /// (and also set the resource, rootResource and context variables)
        /// </summary>
        /// <param name="definitionPath">StructureDefinition style path to the property (not a fhirpath expression)</param>
        public void SetContext(string definitionPath)
        {
            var path = definitionPath.Replace("[x]", "");
            string typeName = path;
            if (path.Contains('.'))
            {
                typeName = path.Substring(0, path.IndexOf("."));
                path = path.Substring(path.IndexOf(".") + 1);
            }
            else
            {
                path = null;
            }
            var rootType = _mi.GetTypeForFhirType(typeName);

            if (rootType != null)
            {
                RegisterVariable("rootResource", rootType);

                if (string.IsNullOrEmpty(path))
                {
                    RegisterVariable("resource", rootType);
                    RegisterVariable("context", rootType);
                    AddInputType(rootType);
                    return;
                }

                var resourceType = rootType; // don't set this till we get to the end, as it could be different
                var nodes = path.Split('.').ToList();
                IEnumerable<ClassMapping> cm = new[] { _mi.FindOrImportClassMapping(rootType) }.Where(v => v != null).ToList();
                while (cm.Any() && nodes.Any())
                {
                    var pm = cm.Select(cm => cm.FindMappedElementByName(nodes[0]) ?? cm.FindMappedElementByChoiceName(nodes[0]))
                        .Where(c => c != null)
                        .ToList();
                    cm = pm.SelectMany(pm2 =>
                    {
                        if (pm2.Choice == ChoiceType.DatatypeChoice && pm2.FhirType.Length == 1 && pm2.FhirType[0].Name == "DataType")
                        {
                            // This is the set of open types
                            return _openTypes.Select(ot => _mi.FindOrImportClassMapping(ot));
                        }
                        if (pm2.Name != nodes[0])
                            return pm2?.FhirType.Where(t => nodes[0].EndsWith(t.Name)).Select(ft => _mi.FindOrImportClassMapping(ft));
                        return pm2?.FhirType.Select(ft => _mi.FindOrImportClassMapping(ft));
                    }).Where(pm => pm != null).ToList();
                    nodes.RemoveAt(0);
                }

                RegisterVariable("resource", resourceType);
                foreach (var tcm in cm)
                {
                    RegisterVariable("context", tcm); // this won't replace things, so not quite right
                    AddInputType(tcm);
                }
            }
            else
            {
                throw new ApplicationException($"Could not result type: {typeName}");
            }
        }

        private readonly ModelInspector _mi;
        private readonly List<string> _supportedResources;
        private readonly Type[] _openTypes;

        // for repeat error checking
        struct RepeatInfo
        {
            public ChildExpression ce;
            public OperationOutcome.IssueComponent issue;
        }
        Dictionary<ChildExpression, OperationOutcome.IssueComponent> _repeatChildren;

        public Hl7.Fhir.Model.OperationOutcome Outcome { get; } = new Hl7.Fhir.Model.OperationOutcome();

        private readonly Stack<FhirPathVisitorProps> _stackPropertyContext = new();
        private readonly Stack<FhirPathVisitorProps> _stackExpressionContext = new();
        private readonly StringBuilder _result = new();
        private int _indent = 0;

        public void RegisterVariable(string name, Type type)
        {
            var cm = _mi.FindOrImportClassMapping(type);
            if (cm != null && !variables.ContainsKey(name))
            {
                variables.Add(name, cm);
            }
        }
        public void RegisterVariable(string name, ClassMapping cm)
        {
            if (cm != null && !variables.ContainsKey(name))
            {
                variables.Add(name, cm);
            }
        }
        private readonly Dictionary<string, ClassMapping> variables = new();

        public override string ToString()
        {
            return _result.ToString();
        }

        private readonly Collection<ClassMapping> _inputTypes = new();
        public void AddInputType(Type t)
        {
            var cm = _mi.FindOrImportClassMapping(t);
            if (cm != null && !_inputTypes.Contains(cm))
            {
                _inputTypes.Add(cm);
            }
        }

        public void AddInputType(ClassMapping cm)
        {
            if (!_inputTypes.Contains(cm))
            {
                _inputTypes.Add(cm);
            }
        }

        public override FhirPathVisitorProps VisitConstant(ConstantExpression expression)
        {
            // ChildExpression ce 
            var r = new FhirPathVisitorProps();
            var t = _mi.GetTypeForFhirType(expression.ExpressionType.Name);
            if (t != null)
            {
                r.AddType(_mi, t);
                var debugValue = expression.ExpressionType.Name.ToLower() switch
                {
                    "boolean" => $"{expression.Value}",
                    "string" => $"'{expression.Value}'",
                    "integer" => $"{expression.Value}",
                    "decimal" => $"{expression.Value}",
                    "date" => $"@{expression.Value}",
                    "datetime" => $"@{expression.Value}",
                    "time" => $"@T{expression.Value}",
                    "quantity" => $"{expression.Value}",
                    _ => ""
                };
                _result.Append(debugValue);
            }
            else
                _result.Append($"{expression.Value}");
            // appendType(expression);
            return r;
        }

        private readonly string[] nonCollectionOperators = new[]
        {
            "=",
            "~",
            "!=",
            "!~",
            "<",
            "<=",
            ">",
            ">=",
            "as",
            "is",
            "or",
            "xor",
            "implies",
            "and", // TODO: check for boolean values each side
        };

        private readonly string[] boolOperators = new[]
        {
            "=",
            "~",
            "!=",
            "!~",
            "<",
            "<=",
            ">",
            ">=",
            "in", // TODO: could check for type overlaps
            "contains", // TODO: could check for type overlaps
            "or",
            "xor",
            "implies",
            "and", // TODO: check for boolean values each side
        };

        private readonly string[] boolFuncs = new[]
        {
            "empty",
            "exists",
            "allTrue",
            "anyTrue",
            "allFalse",
            "anyFalse",
            "binary.contains",
            "binary.in",
            "isDistinct",
            "not",
            "binary.=",
            "binary.!=",
            "binary.~",
            "binary.!~",
            "convertsToBoolean",
            "convertsToInteger",
            "convertsToLong",
            "convertsToDecimal",
            "convertsToQuantity",
            "convertsToString",
            "convertsToDate",
            "convertsToDateTime",
            "convertsToTime",
            "startsWith",
            "endsWith",
            "matches",
            "contains",
            "is",
            "binary.is",
            "binary.and",
            "or",
            "binary.xor",
            "binary.implies",
            "all",
            "any",
            "supersetOf",
            "subsetOf",
            // FHIR extensions to fhirpath
            "hasValue",
            "conformsTo",
            "memberOf",
            "subsumes",
            "subsumedBy",
            "htmlChecks",
            "comparable",
        }.ToArray();

        private readonly string[] stringFocusFuncs = new[] 
        {
            // Section 6.6.7
            "binary.&",
            // Section 5.7 in the spec (CI build)
            "encode",
            "decode",
            "escape",
            "unescape",
            "trim",
            "split",
            "join",
            // Section 5.6 in the spec (normative)
            "indexOf",
            "substring",
            "startsWith",
            "endsWith",
            "contains",
            "upper",
            "lower",
            "replace",
            "matches",
            "replaceMatches",
            "length",
            "toChars",
        }; 

        private readonly string[] stringFuncs = new[]
        {
            "toString",
            "upper",
            "lower",
            "toChars",
            "substring",
            "trim",
            "join",
            "split",
            "encode",
            "decode",
            "escape",
            "unescape",
            "binary.&",
            "replaceMatches",
            "replace",
        }.ToArray();

        private readonly string[] expressionFuncs = new[]
        {
            "exists",
            "all",
            "select",
            "where",
            "repeat",
            "iif",
            "trace",
            "aggregate",
        }.ToArray();

        private readonly string[] booleanArgFuncs = new[]
        {
            "where",
            "all",
            "iif",
        }.ToArray();

        private readonly string[] passthroughFuncs = new[]
        {
            "single",
            "where",
            "trace",
            "first",
            "skip",
            "take",
            "last",
            "tail",
            "intersect", // TODO: could validate that these types have overlap
            "exclude", // TODO: could validate that these types have overlap
            "distinct",

            // New additions in FHIR R5
            "lowBoundary",
            "highBoundary",
        }.ToArray();

        private readonly string[] mathFuncs = new[]
        {
            "+",
            "-",
            "/",
            "*",
        }.ToArray();

        private void DeduceReturnType(FunctionCallExpression function, FhirPathVisitorProps focus, IEnumerable<FhirPathVisitorProps> props, FhirPathVisitorProps outputProps)
        {
            var fd = _table.Get(function.FunctionName);
            if (fd != null)
            {
                // Perform any validations
                foreach (var validation in fd.Validations)
                {
                    validation(fd, props, Outcome);
                }

                // check the context of the function
                if (!fd.IsSupportedContext(focus))
                {
                    var issue = new Hl7.Fhir.Model.OperationOutcome.IssueComponent()
                    {
                        Severity = Hl7.Fhir.Model.OperationOutcome.IssueSeverity.Error,
                        Code = Hl7.Fhir.Model.OperationOutcome.IssueType.NotSupported,
                        Details = new Hl7.Fhir.Model.CodeableConcept() { Text = $"Function '{function.FunctionName}' is not supported on context type '{focus}'" }
                    };
                    if (function.Location != null)
                        issue.Location = new[] { $"Line {function.Location.LineNumber}, Position {function.Location.LineNumber}" };
                    Outcome.AddIssue(issue);
                }
                else
                {
                    // At least this is supported
                    var rts = fd.SupportedContexts.Select(sc => sc.ReturnType).Distinct().ToList();
                    if (!rts.Any() && fd.GetReturnType != null)
                    {
                        foreach (var nprop in fd.GetReturnType(fd, props, Outcome))
                            outputProps.Types.Add(nprop);
					}
                    else
                    {
                        foreach (var rt in rts)
                            outputProps.Types.Add(new NodeProps(rt));
                    }
                }
            }

            if (stringFocusFuncs.Contains(function.FunctionName))
            {
                // these string functions all have to work on an actual type of string too
                if (!focus.CanBeOfType("string"))
                {
                    var issue = new Hl7.Fhir.Model.OperationOutcome.IssueComponent()
                    {
                        Severity = Hl7.Fhir.Model.OperationOutcome.IssueSeverity.Error,
                        Code = Hl7.Fhir.Model.OperationOutcome.IssueType.NotSupported,
                        Details = new Hl7.Fhir.Model.CodeableConcept() { Text = $"String function '{function.FunctionName}' is not supported on {focus}" }
                    };
                    if (function.Location != null)
                        issue.Location = new[] { $"Line {function.Location.LineNumber}, Position {function.Location.LineNumber}" };
                    Outcome.AddIssue(issue);
                }
            }
            
            if (stringFuncs.Contains(function.FunctionName))
            {
                if (function.FunctionName == "toChars")
                    outputProps.AddType(_mi, typeof(Hl7.Fhir.Model.FhirString), true);
                else
                    outputProps.AddType(_mi, typeof(Hl7.Fhir.Model.FhirString));
            }
            else if (function.FunctionName == "is")
            {
                // Check this before the boolfuncs tests
                foreach (var t in focus.Types)
                    outputProps.Types.Add(t);
            }
            else if (function.FunctionName == "as" || function.FunctionName == "ofType")
            {
                // Check this before the boolfuncs tests
                var isTypeArg = function.Arguments.First();
                FhirPathVisitorProps isType = props.FirstOrDefault();
                // Check if the type possibly COULD be evaluated as true
                if (isTypeArg is ConstantExpression ceTa)
                {
                    // ceTa.Value
                    var isTypeToCheck = _mi.GetTypeForFhirType(ceTa.Value as string);
                    var possibleTypeNames = focus.Types.Select(t => t.ClassMapping.Name);
                    var validResultTypes = focus.Types.Where(t => t.ClassMapping.NativeType.IsAssignableFrom(isTypeToCheck));
                    if (!focus.CanBeOfType(ceTa.Value as string))
                    {
                        var issue = new Hl7.Fhir.Model.OperationOutcome.IssueComponent()
                        {
                            Severity = Hl7.Fhir.Model.OperationOutcome.IssueSeverity.Error,
                            Code = Hl7.Fhir.Model.OperationOutcome.IssueType.NotSupported,
                            Details = new Hl7.Fhir.Model.CodeableConcept() { Text = $"Expression included an 'as' test for {ceTa.Value} where possible types are {string.Join(", ", possibleTypeNames)}" }
                        };
                        if (function.Location != null)
                            issue.Location = new[] { $"Line {function.Location.LineNumber}, Position {function.Location.LineNumber}" };
                        Outcome.AddIssue(issue);
                    }
                    else
                    {
                        // filter down to the types listed
                        foreach (var rt in validResultTypes)
                        {
                            outputProps.Types.Add(rt);
                        }
                    }
                }
                if (function.FunctionName == "as")
                {
                    // Check the collection too
                    if (focus.IsCollection())
                    {
                        var issue = new Hl7.Fhir.Model.OperationOutcome.IssueComponent()
                        {
                            Severity = Hl7.Fhir.Model.OperationOutcome.IssueSeverity.Warning,
                            Code = Hl7.Fhir.Model.OperationOutcome.IssueType.MultipleMatches,
                            Details = new Hl7.Fhir.Model.CodeableConcept() { Text = $"Function '{function.FunctionName}' can experience unexpected runtime errors when used with a collection" },
                        };
                        if (function.Location != null)
                            issue.Location = new[] { $"Line {function.Location.LineNumber}, Position {function.Location.LineNumber}" };
                        Outcome.AddIssue(issue);
                    }
                }
            }
            // TODO: Also include ofType special case handling to look for possible warnings
            else if (boolFuncs.Contains(function.FunctionName))
                outputProps.AddType(_mi, typeof(Hl7.Fhir.Model.FhirBoolean));
            else if (function.FunctionName == "length")
            {
                // TODO: Also check that the context coming in here is a string
                outputProps.AddType(_mi, typeof(Hl7.Fhir.Model.Integer));
            }
            else if (function.FunctionName == "count")
                outputProps.AddType(_mi, typeof(Hl7.Fhir.Model.Integer));
            else if (function.FunctionName == "extension")
                outputProps.AddType(_mi, typeof(Hl7.Fhir.Model.Extension));
            else if (mathFuncs.Contains(function.FunctionName))
            {
                foreach (var t in focus.Types)
                    outputProps.Types.Add(t);
            }
            else if (passthroughFuncs.Contains(function.FunctionName))
            {
                foreach (var t in focus.Types)
                {
                    if (function.FunctionName == "first" || function.FunctionName == "last" || function.FunctionName == "tail")
                        outputProps.Types.Add(new NodeProps(t.ClassMapping, t.PropertyMapping) { IsCollection = false });
                    else
                        outputProps.Types.Add(t);
                }
            }
            else if (function.FunctionName == "iif")
            {
                // TODO: Test that the first parameter is always a boolean
                // then return the union of the other 2 parameters
            }
            else if (function.FunctionName == "select")
            {
                // Return types here should also check for Arrays and convert result to an array if source type was a collection
                bool bForceCollections = false;
                foreach (var t in focus.Types)
                {
                    if (t.IsCollection)
                        bForceCollections = true;
                }
                // 
                //foreach (var t in props)
                //{
                //    System.Diagnostics.Trace.WriteLine($"select params: {t}");
                //}
                if (props.Count() == 1)
                {
                    foreach (var t in props.First().Types)
                    {
                        var t2 = bForceCollections ? t.AsCollection() : t;
                        outputProps.Types.Add(t2);
                    }
                }
            }
            else if (function.FunctionName == "resolve")
            {
                // Check the supported reference types for this resource type
                foreach (var t in focus.Types)
                {
                    var v = t.PropertyMapping.NativeProperty.GetCustomAttribute<ReferencesAttribute>();
                    if (v?.Resources?.Any() == true)
                    {
                        // retrieve the classname
                        foreach (var typeName in v.Resources)
                        {
                            var cm = _mi.FindClassMapping(typeName);
                            outputProps.Types.Add(new NodeProps(cm));
                        }
                    }
                    else
                    {
                        // System.Diagnostics.Trace.WriteLine($"No types specified");
                        // Type not listed, so just enumerate ALL resources
                        foreach (var typeName in _supportedResources)
                        {
                            var cm = _mi.FindClassMapping(typeName);
                            outputProps.Types.Add(new NodeProps(cm));
                        }
                    }
                    // outputProps.Types.Add(t);
                }
            }
            else if (function.FunctionName == "children")
            {
                // Check the supported reference types for this resource type
                foreach (var t in focus.Types)
                {
                    // walk through all the child properties
                    foreach (var p in t.ClassMapping.PropertyMappings)
                    {
                        outputProps.Types.Add(new NodeProps(p.PropertyTypeMapping, p));
                    }
                }
            }
            else if (fd == null) // only warn if we didn't have a symbol table entry
            {
                var issue = new Hl7.Fhir.Model.OperationOutcome.IssueComponent()
                {
                    Severity = Hl7.Fhir.Model.OperationOutcome.IssueSeverity.Warning,
                    Code = Hl7.Fhir.Model.OperationOutcome.IssueType.NotSupported,
                    Details = new Hl7.Fhir.Model.CodeableConcept() { Text = $"Unhandled function '{function.FunctionName}'" }
                };
                if (function.Location != null)
                    issue.Location = new[] { $"Line {function.Location.LineNumber}, Position {function.Location.LineNumber}" };
                Outcome.AddIssue(issue);
            }

            if (function.FunctionName == "exists")
            {
                if (props.FirstOrDefault() != null && props.FirstOrDefault()?.ToString() != "boolean")
                {
                    var issue = new Hl7.Fhir.Model.OperationOutcome.IssueComponent()
                    {
                        Severity = Hl7.Fhir.Model.OperationOutcome.IssueSeverity.Error,
                        Code = Hl7.Fhir.Model.OperationOutcome.IssueType.Invalid,
                        Details = new Hl7.Fhir.Model.CodeableConcept() { Text = $"{function.FunctionName} must have a boolean first argument, detected {props.FirstOrDefault()}" }
                    };
                    if (function.Location != null)
                        issue.Location = new[] { $"Line {function.Location.LineNumber}, Position {function.Location.LineNumber}" };
                    Outcome.AddIssue(issue);
                }
            }
            if (booleanArgFuncs.Contains(function.FunctionName))
            {
                if (props.FirstOrDefault()?.ToString() != "boolean")
                {
                    var issue = new Hl7.Fhir.Model.OperationOutcome.IssueComponent()
                    {
                        Severity = Hl7.Fhir.Model.OperationOutcome.IssueSeverity.Error,
                        Code = Hl7.Fhir.Model.OperationOutcome.IssueType.Invalid,
                        Details = new Hl7.Fhir.Model.CodeableConcept() { Text = $"{function.FunctionName} must have a boolean first argument, detected {props.FirstOrDefault()}" }
                    };
                    if (function.Location != null)
                        issue.Location = new[] { $"Line {function.Location.LineNumber}, Position {function.Location.LineNumber}" };
                    Outcome.AddIssue(issue);
                }
            }
        }

        public override FhirPathVisitorProps VisitFunctionCall(FunctionCallExpression expression)
        {
            var result = new FhirPathVisitorProps();
            if (expression is BinaryExpression be)
            {
                VisitBinaryExpression(expression, result, be);
                return result;
            }
            var rFocus = expression.Focus.Accept(this);
            _stackPropertyContext.Push(rFocus);

            if (expression is IndexerExpression)
            {
                VisitIndexerExpression(expression, result, rFocus);
                _stackPropertyContext.Pop();
                return result;
            }

            if (expression is ChildExpression ce)
            {
                VisitChildExpression(expression, result, rFocus, ce);
                _stackPropertyContext.Pop();
                return result;
            }

            if (!rFocus.isRoot)
                _result.Append('.');
            _result.Append($"{expression.FunctionName}(");

            if (expression.FunctionName == "combine" || expression.FunctionName == "union")
            {
                VisitCombineOrUnionFunction(rFocus, expression, result);
                _result.Append(')');
                _result.AppendLine($" : {result}");
                _stackPropertyContext.Pop();
                return result;
            }

			if (expressionFuncs.Contains(expression.FunctionName))
            {
                if (expression.FunctionName == "select"
                    || expression.FunctionName == "where"
                    || expression.FunctionName == "exists"
                    || expression.FunctionName == "all")
                {
                    // Push them onto the stack without the collection as we're processing them individually
                    var rFocusSingle = rFocus.AsSingle();
                    _stackExpressionContext.Push(rFocusSingle);
                }
                else
                {
                    _stackExpressionContext.Push(rFocus);
                }
            }

            if (expression.FunctionName == "repeat")
            {
                VisitRepeatFunction(expression, result);
                _stackPropertyContext.Pop();
                _stackExpressionContext.Pop();
                return result;
            }

            IncrementTab();

            List<FhirPathVisitorProps> argTypes = new();
            foreach (var arg in expression.Arguments)
            {
                if (argTypes.Count > 0)
                    _result.Append(", ");
                argTypes.Add(arg.Accept(this));
            }

            DecrementTab();
            _result.Append(')');

            DeduceReturnType(expression, rFocus, argTypes, result);

            if (expressionFuncs.Contains(expression.FunctionName))
            {
                _stackExpressionContext.Pop();
            }

            _result.AppendLine($" : {result}");

            _stackPropertyContext.Pop();
            return result;
        }

        private void VisitRepeatFunction(FunctionCallExpression expression, FhirPathVisitorProps result)
        {
            _repeatChildren = new Dictionary<ChildExpression, OperationOutcome.IssueComponent>();

            // Special handling for repeat,
            // iteratively select types using the expressions we
            // work out if all the names are actually possible
            List<FhirPathVisitorProps> argTypesR = new();
            foreach (var arg in expression.Arguments)
            {
                if (argTypesR.Count > 0)
                    _result.Append(", ");
                argTypesR.Add(arg.Accept(this));
                foreach (var t in argTypesR)
                {
                    foreach (var t2 in t.Types)
                        if (!result.Types.Contains(t2))
                            result.Types.Add(t2);
                }
            }

            // Now iterate in with these result types 
            _stackPropertyContext.Push(result);
            _stackExpressionContext.Push(result);
            bool bChanged = false;
            int maxIterations = 10;
            do
            {
                bChanged = false;
                maxIterations--;
                foreach (var arg in expression.Arguments)
                {
                    _result.Append(", ");
                    argTypesR.Add(arg.Accept(this));
                    foreach (var t in argTypesR)
                    {
                        foreach (var t2 in t.Types)
                            if (!result.Types.Any(t => t.ClassMapping == t2.ClassMapping))
                            {
                                result.Types.Add(t2);
                                bChanged = true;
                            }
                    }
                }
            }
            while (bChanged && maxIterations > 0);
            if (maxIterations == 0)
            {
                var issue = new Hl7.Fhir.Model.OperationOutcome.IssueComponent()
                {
                    Severity = Hl7.Fhir.Model.OperationOutcome.IssueSeverity.Error,
                    Code = Hl7.Fhir.Model.OperationOutcome.IssueType.NotFound,
                    Details = new Hl7.Fhir.Model.CodeableConcept() { Text = $"repeat() iterations exceeded 10" }
                };
                Outcome.AddIssue(issue);

            }
            _stackPropertyContext.Pop();
            _stackExpressionContext.Pop();
            if (_repeatChildren != null)
            {
                foreach (var iss in _repeatChildren.Values.Where(v => v != null))
                {
                    Outcome.Issue.Add(iss);
                }
                _repeatChildren = null;
            }
        }

        private void VisitCombineOrUnionFunction(FhirPathVisitorProps rFocus, FunctionCallExpression expression, FhirPathVisitorProps result)
        {
            // Start with the focus types
            foreach (var t in rFocus.Types)
                result.Types.Add(t);

            // Combine takes the possible types of the "focus" and appends the possible types of the arguments and returns those
            foreach (var arg in expression.Arguments)
            {
                var t = arg.Accept(this);
                foreach (var t2 in t.Types)
                {
                    if (!result.Types.Any(t => t.ClassMapping.Name == t2.ClassMapping.Name))
                    {
                        result.Types.Add(t2);
                    }
                    else
                    {
                        // If the type was already in the list, then we can set that to be a collection instead
                        var v = result.Types.First(t => t.ClassMapping.Name == t2.ClassMapping.Name);
                        if (!v.IsCollection)
                        {
                            var v2 = v.AsCollection();
                            result.Types.Remove(v);
                            result.Types.Add(v2);
                        }
                    }
                }
            }
        }

        private void VisitChildExpression(FunctionCallExpression expression, FhirPathVisitorProps r, FhirPathVisitorProps rFocus, ChildExpression ce)
        {
            // _stack.Push(rFocus);
            if (!rFocus.isRoot)
                _result.Append('.');
            else
            {
                if (rFocus.Types.FirstOrDefault().ClassMapping?.Name == ce.ChildName)
                {
                    r.Types.Add(rFocus.Types.FirstOrDefault());
                    if (_repeatChildren?.ContainsKey(ce) == true)
                        _repeatChildren[ce] = null;
                    return;
                }
                else
                {
                    // This is a workaround for search parameters
                    // where they merge multiple resource types into
                    // the same expression.
                    if (_mi.IsKnownResource(ce.ChildName))
                    {
                        var rt = _mi.GetTypeForFhirType(ce.ChildName);
                        if (rt.Name == ce.ChildName)
                        {
                            r.AddType(_mi, rt, rFocus.IsCollection());
                            if (_repeatChildren?.ContainsKey(ce) == true)
                                _repeatChildren[ce] = null;
                            return;
                        }
                    }
                }
            }
            bool propFound = false;
            foreach (var t in rFocus.Types)
            {
                var childProp = t.ClassMapping.FindMappedElementByName(ce.ChildName);
                if (childProp == null)
                {
                    // Check if this is a choice type (using the choicename in the type is not valid)
                    var ctCP = t.ClassMapping.FindMappedElementByChoiceName(ce.ChildName);
                    if (ctCP != null)
                    {
                        // report this as an error!!!
                        var issue = new Hl7.Fhir.Model.OperationOutcome.IssueComponent()
                        {
                            Severity = Hl7.Fhir.Model.OperationOutcome.IssueSeverity.Error,
                            Code = Hl7.Fhir.Model.OperationOutcome.IssueType.Value,
                            Details = new Hl7.Fhir.Model.CodeableConcept() { Text = $"prop '{ce.ChildName}' is the choice type, remove the type from the end - {ctCP.Name}" }
                        };
                        if (expression.Location != null)
                            issue.Location = new[] { $"Line {expression.Location.LineNumber}, Position {expression.Location.LineNumber}" };
                        if (_repeatChildren != null)
                        {
                            if (!_repeatChildren.ContainsKey(ce))
                                _repeatChildren.Add(ce, issue);
                        }
                        else
                            Outcome.AddIssue(issue);
                    }
                }
                if (childProp != null)
                {
                    // _stack.Push()
                    // System.Diagnostics.Trace.WriteLine($"read {childProp.Name} {String.Join(",", childProp.FhirType.Select(v => v.Name).ToArray())}");

                    if (childProp.Choice == ChoiceType.ResourceChoice)
                    {
                        foreach (var rt in _supportedResources)
                        {
                            if (!_mi.IsKnownResource(rt))
                                continue;
                            var cm = _mi.FindClassMapping(rt);
                            if (cm != null)
                            {
                                // System.Diagnostics.Trace.WriteLine($"read {childProp.Name} {rt}");
                                r.Types.Add(new NodeProps(cm, childProp, rFocus.IsCollection()));
                                propFound = true;
                                if (_repeatChildren?.ContainsKey(ce) == true)
                                    _repeatChildren[ce] = null;
                            }
                            else
                            {
                                // System.Diagnostics.Trace.WriteLine($"class {childProp.ImplementingType} not found");
                                var issue = new Hl7.Fhir.Model.OperationOutcome.IssueComponent()
                                {
                                    Severity = Hl7.Fhir.Model.OperationOutcome.IssueSeverity.Error,
                                    Code = Hl7.Fhir.Model.OperationOutcome.IssueType.Invalid,
                                    Details = new Hl7.Fhir.Model.CodeableConcept() { Text = $"class {childProp.ImplementingType} not found" }
                                };
                                if (expression.Location != null)
                                    issue.Location = new[] { $"Line {expression.Location.LineNumber}, Position {expression.Location.LineNumber}" };
                                if (_repeatChildren != null)
                                {
                                    if (!_repeatChildren.ContainsKey(ce))
                                        _repeatChildren.Add(ce, issue);
                                }
                                else
                                    Outcome.AddIssue(issue);
                            }
                        }
                    }
                    else if (childProp.Choice == ChoiceType.DatatypeChoice)
                    {
                        foreach (var ft in childProp.FhirType)
                        {
                            var cm = _mi.FindOrImportClassMapping(ft);
                            if (cm != null)
                            {
                                if (ft.FullName == "Hl7.Fhir.Model.DataType" && t.ClassMapping.Name == "Extension")
                                {
                                    // List the actual fhir types valid in extensions
                                    foreach (var rt in _openTypes)
                                    {
                                        var cme = _mi.FindOrImportClassMapping(rt);
                                        if (cme != null)
                                        {
                                            // System.Diagnostics.Trace.WriteLine($"read {childProp.Name} {rt}");
                                            r.Types.Add(new NodeProps(cme, childProp, rFocus.IsCollection()));
                                            propFound = true;
                                            if (_repeatChildren?.ContainsKey(ce) == true)
                                                _repeatChildren[ce] = null;
                                        }
                                    }
                                    break;
                                }
                                r.Types.Add(new NodeProps(cm, childProp, rFocus.IsCollection()));
                                propFound = true;
                                if (_repeatChildren?.ContainsKey(ce) == true)
                                    _repeatChildren[ce] = null;
                            }
                            else
                            {
                                // System.Diagnostics.Trace.WriteLine($"class {childProp.ImplementingType} not found");
                                var issue = new Hl7.Fhir.Model.OperationOutcome.IssueComponent()
                                {
                                    Severity = Hl7.Fhir.Model.OperationOutcome.IssueSeverity.Error,
                                    Code = Hl7.Fhir.Model.OperationOutcome.IssueType.Invalid,
                                    Details = new Hl7.Fhir.Model.CodeableConcept() { Text = $"class {childProp.ImplementingType} not found" }
                                };
                                if (expression.Location != null)
                                    issue.Location = new[] { $"Line {expression.Location.LineNumber}, Position {expression.Location.LineNumber}" };
                                if (_repeatChildren != null)
                                {
                                    if (!_repeatChildren.ContainsKey(ce))
                                        _repeatChildren.Add(ce, issue);
                                }
                                else
                                    Outcome.AddIssue(issue);
                            }

                        }
                    }
                    else
                    {
                        foreach (var ct in childProp.FhirType)
                        {
                            var cm = _mi.FindOrImportClassMapping(ct);
                            if (cm != null)
                            {
                                r.Types.Add(new NodeProps(cm, childProp, rFocus.IsCollection()));
                                propFound = true;
                                if (_repeatChildren?.ContainsKey(ce) == true)
                                    _repeatChildren[ce] = null;
                            }
                        }
                    }
                }
            }
            if (!propFound)
            {
                var issue = new Hl7.Fhir.Model.OperationOutcome.IssueComponent()
                {
                    Severity = Hl7.Fhir.Model.OperationOutcome.IssueSeverity.Error,
                    Code = Hl7.Fhir.Model.OperationOutcome.IssueType.NotFound,
                    Details = new Hl7.Fhir.Model.CodeableConcept() { Text = $"prop '{ce.ChildName}' not found on {rFocus}" }
                };
                if (expression.Location != null)
                    issue.Location = new[] { $"Line {expression.Location.LineNumber}, Position {expression.Location.LineNumber}" };
                if (_repeatChildren != null)
                {
                    if (!_repeatChildren.ContainsKey(ce))
                        _repeatChildren.Add(ce, issue);
                }
                else
                    Outcome.AddIssue(issue);
            }
            _result.Append($"{ce.ChildName}");
            _result.AppendLine($" : {r}");
        }

        private void VisitIndexerExpression(FunctionCallExpression expression, FhirPathVisitorProps result, FhirPathVisitorProps rFocus)
        {
            _result.Append('[');
            foreach (var arg in expression.Arguments)
                arg.Accept(this);
            _result.Append(']');
            foreach (var t in rFocus.Types)
                result.Types.Add(t.AsSingle());
            // _result.AppendLine($" : {String.Join(", ", r.Types.Select(v => v.Name))}");
        }

        private void VisitBinaryExpression(FunctionCallExpression expression, FhirPathVisitorProps result, BinaryExpression be)
        {
            var leftResult = expression.Arguments.First().Accept(this);
            _result.AppendLine($"{be.Op}");
            var rightExpression = expression.Arguments.Skip(1).First();
            var rightResult = rightExpression.Accept(this);

            if (be.Op == "is")
            {
                // Check if the type possibly COULD be evaluated as true
                if (rightExpression is ConstantExpression ceTa)
                {
                    // ceTa.Value
                    var isTypeToCheck = _mi.GetTypeForFhirType(ceTa.Value as string);
                    var possibleTypeNames = leftResult.Types.Select(t => t.ClassMapping.Name);
                    if (!leftResult.Types.Any(t => t.ClassMapping.NativeType.IsAssignableFrom(isTypeToCheck)) && !leftResult.CanBeOfType(ceTa.Value as string))
                    {
                        var issue = new Hl7.Fhir.Model.OperationOutcome.IssueComponent()
                        {
                            Severity = Hl7.Fhir.Model.OperationOutcome.IssueSeverity.Error,
                            Code = Hl7.Fhir.Model.OperationOutcome.IssueType.NotSupported,
                            Details = new Hl7.Fhir.Model.CodeableConcept() { Text = $"Expression included an 'is' test for {ceTa.Value} where possible types are {string.Join(", ", possibleTypeNames)}" }
                        };
                        if (be.Location != null)
                            issue.Location = new[] { $"Line {be.Location.LineNumber}, Position {be.Location.LineNumber}" };
                        Outcome.AddIssue(issue);
                    }
                }
                // TODO:
                result.AddType(_mi, typeof(Hl7.Fhir.Model.FhirBoolean));
            }
            else if (be.Op == "as")
            {
                // Check if the type possibly COULD be evaluated as true
                if (rightExpression is ConstantExpression ceTa)
                {
                    // ceTa.Value
                    var isTypeToCheck = _mi.GetTypeForFhirType(ceTa.Value as string);
                    var possibleTypeNames = leftResult.Types.Select(t => t.ClassMapping.Name);
                    var validResultTypes = leftResult.Types.Where(t => t.ClassMapping.NativeType.IsAssignableFrom(isTypeToCheck));
                    if (!validResultTypes.Any())
                    {
                        var issue = new Hl7.Fhir.Model.OperationOutcome.IssueComponent()
                        {
                            Severity = Hl7.Fhir.Model.OperationOutcome.IssueSeverity.Error,
                            Code = Hl7.Fhir.Model.OperationOutcome.IssueType.NotSupported,
                            Details = new Hl7.Fhir.Model.CodeableConcept() { Text = $"Expression included an 'as' test for {ceTa.Value} where possible types are {string.Join(", ", possibleTypeNames)}" }
                        };
                        if (be.Location != null)
                            issue.Location = new[] { $"Line {be.Location.LineNumber}, Position {be.Location.LineNumber}" };
                        Outcome.AddIssue(issue);
                    }
                    else
                    {
                        // filter down to the types listed
                        foreach (var rt in validResultTypes)
                        {
                            result.Types.Add(rt);
                        }
                    }
                }
            }
            else if (be.Op == "|")
            {
                foreach (var t in leftResult.Types)
                    result.Types.Add(t);
                foreach (var t in rightResult.Types)
                {
                    // Merge in the types (don't duplicate them)
                    // if the same one is already in there, remove it and replace with a collection version.
                    var et = result.Types.Where(tc => tc.ClassMapping.Name == t.ClassMapping.Name);
                    if (et.Any())
                    {
                        var ct = et.First();
                        if (!ct.IsCollection)
                        {
                            result.Types.Add(ct.AsCollection());
                            result.Types.Remove(ct);
                        }
                    }
                    else
                    {
                        result.Types.Add(t);
                    }
                }
            }
            else
            {
                if (boolOperators.Contains(be.Op))
                {
                    result.AddType(_mi, typeof(Hl7.Fhir.Model.FhirBoolean));
                }
                else
                {
                    foreach (var t in leftResult.Types)
                        result.Types.Add(t);
                }
            }

            if (nonCollectionOperators.Contains(be.Op))
            {
                // Validate that neither of the arguments are collections
                if (leftResult.IsCollection() != rightResult.IsCollection())
                {
                    var issue = new Hl7.Fhir.Model.OperationOutcome.IssueComponent()
                    {
                        Severity = Hl7.Fhir.Model.OperationOutcome.IssueSeverity.Warning,
                        Code = Hl7.Fhir.Model.OperationOutcome.IssueType.MultipleMatches,
                        Details = new Hl7.Fhir.Model.CodeableConcept() { Text = $"Operator '{be.Op}' can experience unexpected runtime errors when used with a collection" },
                        Diagnostics = $"{leftResult} {be.Op} {rightResult}"
                    };
                    if (be.Location != null)
                        issue.Location = new[] { $"Line {be.Location.LineNumber}, Position {be.Location.LineNumber}" };
                    Outcome.AddIssue(issue);
                }
            }

            _result.AppendLine($"\r\n : {result} (op: {be.Op})");
        }

        public override FhirPathVisitorProps VisitNewNodeListInit(NewNodeListInitExpression expression)
        {
            var r = new FhirPathVisitorProps();
            Append("{}");
            // There is no type information for this sub-expression

            IncrementTab();
            foreach (var element in expression.Contents)
                element.Accept(this);
            DecrementTab();

            return r;
        }

        public override FhirPathVisitorProps VisitVariableRef(VariableRefExpression expression)
        {
            var r = new FhirPathVisitorProps();
            if (expression.Name == "builtin.that")
            {
                r.isRoot = true;
                if (_stackExpressionContext.Any())
                {
                    foreach (var t in _stackExpressionContext.Peek().Types)
                        r.Types.Add(t);
                }
                else
                {
                    foreach (var t in _inputTypes)
                        r.Types.Add(new NodeProps(t));
                }
                // _result.AppendLine($" : {String.Join(", ", r.Types.Select(v => v.Name))}");
                return r;
            }
            if (expression.Name == "builtin.this")
            {
                if (_stackExpressionContext.Any())
                {
                    foreach (var t in _stackExpressionContext.Peek().Types)
                        r.Types.Add(t);
                }
                else
                {
                    foreach (var t in _inputTypes)
                        r.Types.Add(new NodeProps(t));
                }
                _result.Append("$this");

                _result.AppendLine($" : {r}");
                return r;
            }
            if (variables.ContainsKey(expression.Name))
            {
                _result.Append($"%{expression.Name}");
                r.Types.Add(new NodeProps(variables[expression.Name]));
                _result.AppendLine($" : {r}");
                return r;
            }
            var issue = new Hl7.Fhir.Model.OperationOutcome.IssueComponent()
            {
                Severity = Hl7.Fhir.Model.OperationOutcome.IssueSeverity.Error,
                Code = Hl7.Fhir.Model.OperationOutcome.IssueType.NotFound,
                Details = new Hl7.Fhir.Model.CodeableConcept() { Text = $"variable '{expression.Name}' not found" }
            };
            if (expression.Location != null)
                issue.Location = new[] { $"Line {expression.Location.LineNumber}, Position {expression.Location.LineNumber}" };
            Outcome.AddIssue(issue);

            return r;
        }

        private void Append(string text, bool newLine = true)
        {
            if (newLine)
            {
                _result.AppendLine();
                _result.Append(new String(' ', _indent * 4));
            }

            _result.Append(text);
        }

        private void IncrementTab()
        {
            _indent += 1;
        }

        private void DecrementTab()
        {
            _indent -= 1;
        }
    }
}
