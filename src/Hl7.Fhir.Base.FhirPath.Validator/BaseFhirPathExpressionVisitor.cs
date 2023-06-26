﻿using Hl7.Fhir.Introspection;
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
        }

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
                    cm = pm.SelectMany(pm2 => {
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

        private readonly Stack<FhirPathVisitorProps> _stack = new();
        private readonly Stack<FhirPathVisitorProps> _stackThis = new();
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
            if (expression.ExpressionType.Name == "String")
                _result.Append($"'{expression.Value}'");
            else
                _result.Append($"{expression.Value}");
            // appendType(expression);
            return r;
        }

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
            "in",
            "contains",
            "or",
            "xor",
            "implies",
            "and",
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
            // FHIR extensions to fhirpath
            "hasValue",
            "conformsTo",
            "memberOf",
            "subsumes",
            "subsumedBy",
            "htmlChecks",
            "comparable",
        }.ToArray();

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

        private readonly string[] passthroughFuncs = new[]
        {
            "where",
            "trace",
            "first",
            "skip",
            "take",
            "last",
            "tail" 
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
            if (stringFuncs.Contains(function.FunctionName))
                outputProps.AddType(_mi, typeof(Hl7.Fhir.Model.FhirString));
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
                FhirPathVisitorProps isType = isTypeArg.Accept(this);
                // Check if the type possibly COULD be evaluated as true
                if (isTypeArg is ConstantExpression ceTa)
                {
                    // ceTa.Value
                    var isTypeToCheck = _mi.GetTypeForFhirType(ceTa.Value as string);
                    var possibleTypeNames = focus.Types.Select(t => t.ClassMapping.Name);
                    var validResultTypes = focus.Types.Where(t => t.ClassMapping.NativeType.IsAssignableFrom(isTypeToCheck));
                    if (!validResultTypes.Any())
                    {
                        System.Diagnostics.Trace.WriteLine($"Expression included an 'as' test for {ceTa.Value} where possible types are {string.Join(", ", possibleTypeNames)}");
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
                // Test that the first parameter is always a boolean
                // then return the union of the other 2 parameters
            }
            else if (function.FunctionName == "select")
            {
                foreach (var t in props)
                {
                    System.Diagnostics.Trace.WriteLine($"select params: {t}");
                }
                if (props.Count() == 1)
                {
                    foreach (var t in props.First().Types)
                        outputProps.Types.Add(t);
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
            else
            {
                System.Diagnostics.Trace.WriteLine($"Unhandled function {function.FunctionName}");
                var issue = new Hl7.Fhir.Model.OperationOutcome.IssueComponent()
                {
                    Severity = Hl7.Fhir.Model.OperationOutcome.IssueSeverity.Warning,
                    Code = Hl7.Fhir.Model.OperationOutcome.IssueType.NotSupported,
                    Details = new Hl7.Fhir.Model.CodeableConcept() { Text = $"Unhandled function {function.FunctionName}" }
                };
                if (function.Location != null)
                    issue.Location = new[] { $"Line {function.Location.LineNumber}, Position {function.Location.LineNumber}" };
                Outcome.AddIssue(issue);
            }
        }

        public override FhirPathVisitorProps VisitFunctionCall(FunctionCallExpression expression)
        {
            var r = new FhirPathVisitorProps();
            if (expression is BinaryExpression be)
            {
                return VisitBinaryExpression(expression, r, be);
            }
            var rFocus = expression.Focus.Accept(this);
            _stack.Push(rFocus);

            if (expression is IndexerExpression)
            {
                return VisitIndexerExpression(expression, r, rFocus);
            }

            if (expression is ChildExpression ce)
            {
                return VisitChildExpression(expression, r, rFocus, ce);
            }

            if (!rFocus.isRoot)
                _result.Append('.');
            _result.Append($"{expression.FunctionName}(");

            if (expression.FunctionName == "select"
                || passthroughFuncs.Contains(expression.FunctionName))
            {
                _stack.Push(rFocus);
            }
            
            if (expressionFuncs.Contains(expression.FunctionName))
            {
                _stackThis.Push(rFocus);
            }

            if (expression.FunctionName == "repeat")
            {
                return VisitRepeatFunction(expression, r);
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

            DeduceReturnType(expression, rFocus, argTypes, r);

            if (expression.FunctionName == "select"
                || passthroughFuncs.Contains(expression.FunctionName)
                )
            {
                _stack.Pop();
            }
            if (expressionFuncs.Contains(expression.FunctionName))
            {
                _stackThis.Pop();
            }

            _result.AppendLine($" : {r}");

            _stack.Pop();
            return r;
        }

        private FhirPathVisitorProps VisitRepeatFunction(FunctionCallExpression expression, FhirPathVisitorProps r)
        {
            _repeatChildren = new Dictionary<ChildExpression, OperationOutcome.IssueComponent?>();

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
                        if (!r.Types.Contains(t2))
                            r.Types.Add(t2);
                }
            }

            // Now iterate in with these result types 
            _stack.Push(r);
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
                            if (!r.Types.Any(t => t.ClassMapping == t2.ClassMapping))
                            {
                                r.Types.Add(t2);
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
            _stack.Pop();
            if (_repeatChildren != null)
            {
                foreach (var iss in _repeatChildren.Values.Where(v => v != null))
                {
                    Outcome.Issue.Add(iss);
                }
                _repeatChildren = null;
            }

            return r;
        }

        private FhirPathVisitorProps VisitChildExpression(FunctionCallExpression expression, FhirPathVisitorProps r, FhirPathVisitorProps rFocus, ChildExpression ce)
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
                    return r;
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
                            r.AddType(_mi, rt);
                            if (_repeatChildren?.ContainsKey(ce) == true)
                                _repeatChildren[ce] = null;
                            return r;
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
                    System.Diagnostics.Trace.WriteLine($"read {childProp.Name} {String.Join(",", childProp.FhirType.Select(v => v.Name).ToArray())}");

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
                                r.Types.Add(new NodeProps(cm, childProp));
                                propFound = true;
                                if (_repeatChildren?.ContainsKey(ce) == true)
                                    _repeatChildren[ce] = null;
                            }
                            else
                            {
                                System.Diagnostics.Trace.WriteLine($"class {childProp.ImplementingType} not found");
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
                                            r.Types.Add(new NodeProps(cme, childProp));
                                            propFound = true;
                                            if (_repeatChildren?.ContainsKey(ce) == true)
                                                _repeatChildren[ce] = null;
                                        }
                                    }
                                    break;
                                }
                                r.Types.Add(new NodeProps(cm, childProp));
                                propFound = true;
                                if (_repeatChildren?.ContainsKey(ce) == true)
                                    _repeatChildren[ce] = null;
                            }
                            else
                            {
                                System.Diagnostics.Trace.WriteLine($"class {childProp.ImplementingType} not found");
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
                                r.Types.Add(new NodeProps(cm, childProp));
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
                System.Diagnostics.Trace.WriteLine($"prop '{ce.ChildName}' not found");
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
            _stack.Pop();
            return r;
        }

        private FhirPathVisitorProps VisitIndexerExpression(FunctionCallExpression expression, FhirPathVisitorProps r, FhirPathVisitorProps rFocus)
        {
            _result.Append('[');
            foreach (var arg in expression.Arguments)
                arg.Accept(this);
            _result.Append(']');
            foreach (var t in rFocus.Types)
                r.Types.Add(t);
            // _result.AppendLine($" : {String.Join(", ", r.Types.Select(v => v.Name))}");
            _stack.Pop();
            return r;
        }

        private FhirPathVisitorProps VisitBinaryExpression(FunctionCallExpression expression, FhirPathVisitorProps r, BinaryExpression be)
        {
            if (be.Op == "is")
            {
                FhirPathVisitorProps focus = expression.Arguments.First().Accept(this);
                var isTypeArg = expression.Arguments.Skip(1).First();
                FhirPathVisitorProps isType = isTypeArg.Accept(this);
                // Check if the type possibly COULD be evaluated as true
                if (isTypeArg is ConstantExpression ceTa)
                {
                    // ceTa.Value
                    var isTypeToCheck = _mi.GetTypeForFhirType(ceTa.Value as string);
                    var possibleTypeNames = focus.Types.Select(t => t.ClassMapping.Name);
                    if (!focus.Types.Any(t => t.ClassMapping.NativeType.IsAssignableFrom(isTypeToCheck)))
                    {
                        System.Diagnostics.Trace.WriteLine($"Expression included an 'is' test for {ceTa.Value} where possible types are {string.Join(", ", possibleTypeNames)}");
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
                r.AddType(_mi, typeof(Hl7.Fhir.Model.FhirBoolean));
            }
            else if (be.Op == "as")
            {
                FhirPathVisitorProps focus = expression.Arguments.First().Accept(this);
                var isTypeArg = expression.Arguments.Skip(1).First();
                FhirPathVisitorProps isType = isTypeArg.Accept(this);
                // Check if the type possibly COULD be evaluated as true
                if (isTypeArg is ConstantExpression ceTa)
                {
                    // ceTa.Value
                    var isTypeToCheck = _mi.GetTypeForFhirType(ceTa.Value as string);
                    var possibleTypeNames = focus.Types.Select(t => t.ClassMapping.Name);
                    var validResultTypes = focus.Types.Where(t => t.ClassMapping.NativeType.IsAssignableFrom(isTypeToCheck));
                    if (!validResultTypes.Any())
                    {
                        System.Diagnostics.Trace.WriteLine($"Expression included an 'as' test for {ceTa.Value} where possible types are {string.Join(", ", possibleTypeNames)}");
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
                            r.Types.Add(rt);
                        }
                    }
                }
            }
            else if (be.Op == "|")
            {
                IncrementTab();
                FhirPathVisitorProps first = null;
                foreach (var arg in expression.Arguments)
                {
                    if (first != null)
                        _result.Append($" {be.Op} ");
                    first = arg.Accept(this);
                    foreach (var t in first.Types)
                        r.Types.Add(t);
                }
                _result.AppendLine($" : {r} (op: {be.Op})");
                DecrementTab();
                return r;
            }
            else
            {
                FhirPathVisitorProps first = null;
                foreach (var arg in expression.Arguments)
                {
                    if (first != null)
                        _result.Append($" {be.Op} ");
                    first = arg.Accept(this);
                }
                if (boolOperators.Contains(be.Op))
                {
                    r.AddType(_mi, typeof(Hl7.Fhir.Model.FhirBoolean));
                }
                else
                {
                    foreach (var t in first.Types)
                        r.Types.Add(t);
                }
            }

            _result.AppendLine($" : {r} (op: {be.Op})");
            return r;
        }

        public override FhirPathVisitorProps VisitNewNodeListInit(NewNodeListInitExpression expression)
        {
            var r = new FhirPathVisitorProps();
            Append("new NodeSet");
            // appendType(expression);

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
                if (_stack.Any())
                {
                    foreach (var t in _stack.Peek().Types)
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
                if (_stackThis.Any())
                {
                    foreach (var t in _stackThis.Peek().Types)
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
