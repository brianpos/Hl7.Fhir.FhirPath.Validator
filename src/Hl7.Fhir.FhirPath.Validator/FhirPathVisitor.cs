﻿using Hl7.Fhir.Introspection;
using Hl7.Fhir.Support;
using Hl7.Fhir.Utility;
using Hl7.FhirPath.Expressions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Engine.R4B.Helpers
{
    public struct NodeProps
    {
        public NodeProps(ClassMapping classMapping, PropertyMapping propMap = null)
        {
            ClassMapping = classMapping;
            IsCollection = propMap?.IsCollection == true;
            PropertyMapping = propMap;
        }

        public ClassMapping ClassMapping { get; set; }
        public PropertyMapping PropertyMapping { get; set; }
        public bool IsCollection { get; set; }
    }

    public class FhirPathVisitorProps
    {
        public bool isRoot;
        public readonly Collection<NodeProps> Types = new();

        public void AddType(ModelInspector mi, Type type)
        {
            var cm = mi.FindOrImportClassMapping(type);
            if (cm != null)
            {
                Types.Add(new NodeProps(cm));
            }
        }

        public override string ToString()
        {
            return String.Join(", ", Types.Select(v =>
            {
                if (v.IsCollection)
                    return $"{v.ClassMapping.Name}[]";
                return v.ClassMapping.Name;
            }).Distinct());
        }
    }

    public class FhirPathExpressionVisitor : ExpressionVisitor<FhirPathVisitorProps>
    {
        public Hl7.Fhir.Model.OperationOutcome Outcome { get; } = new Hl7.Fhir.Model.OperationOutcome();

        private Stack<FhirPathVisitorProps> _stack = new();
        private readonly StringBuilder _result = new StringBuilder();
        private int _indent = 0;
        public FhirPathExpressionVisitor(ModelInspector mi)
        {
            _mi = mi;
            RegisterVariable("ucum", typeof(Hl7.Fhir.Model.FhirString));
            RegisterVariable("sctt", typeof(Hl7.Fhir.Model.FhirString));
        }

        public void RegisterVariable(string name, Type type)
        {
            var cm = _mi.FindOrImportClassMapping(type);
            if (cm != null && !variables.ContainsKey(name))
            {
                variables.Add(name, cm);
            }
        }
        private Dictionary<string, ClassMapping> variables = new();

        public override string ToString()
        {
            return _result.ToString();
        }

        private Collection<ClassMapping> _inputTypes = new();
        public void AddInputType(Type t)
        {
            var cm = _mi.FindOrImportClassMapping(t);
            if (cm != null)
            {
                _inputTypes.Add(cm);
            }
        }
        ModelInspector _mi;

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

        private string[] boolFuncs = new[]
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
            "ofType",
            "conformsTo",
            "memberOf",
            "subsumes",
            "subsumedBy",
            "htmlChecks",
            "comparable",
        }.ToArray();

        private string[] stringFuncs = new[]
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

        private string[] passthroughFuncs = new[]
        {
            "where",
            "trace",
            "first",
            "skip",
            "take",
            "last",
            "tail"
        }.ToArray();

        private string[] mathFuncs = new[]
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
                    outputProps.Types.Add(t);
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
                    System.Diagnostics.Trace.WriteLine($"select params: {t.ToString()}");
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
                        // unkn
                        System.Diagnostics.Trace.WriteLine($"No types specified");
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
                else
                {
                    FhirPathVisitorProps first = null;
                    foreach (var arg in expression.Arguments)
                    {
                        if (first != null)
                            _result.Append($" {be.Op} ");
                        first = arg.Accept(this);
                    }
                    if (be.Op == "=" || be.Op == "!=")
                    {
                        r.AddType(_mi, typeof(Hl7.Fhir.Model.FhirBoolean));
                    }
                    else
                    {
                        foreach (var t in first.Types)
                            r.Types.Add(t);
                    }
                }

                _result.AppendLine($" : {r.ToString()} (op: {be.Op})");
                return r;
            }
            var rFocus = expression.Focus.Accept(this);
            _stack.Push(rFocus);

            if (expression is IndexerExpression)
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

            if (expression is ChildExpression ce)
            {
                // _stack.Push(rFocus);
                if (!rFocus.isRoot)
                    _result.Append($".");
                else
                {
                    if (rFocus.Types.FirstOrDefault().ClassMapping?.Name == ce.ChildName)
                    {
                        r.Types.Add(rFocus.Types.FirstOrDefault());
                        return r;
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
                            Outcome.AddIssue(issue);
                        }
                    }
                    if (childProp != null)
                    {
                        // _stack.Push()
                        System.Diagnostics.Trace.WriteLine($"read {childProp.Name} {String.Join(",", childProp.FhirType.Select(v => v.Name).ToArray())}");

                        if (childProp.Choice == ChoiceType.ResourceChoice)
                        {
                            foreach (var rt in Hl7.Fhir.Model.ModelInfo.SupportedResources)
                            {
                                if (!Hl7.Fhir.Model.ModelInfo.IsKnownResource(rt))
                                    continue;
                                var cm = _mi.FindClassMapping(rt);
                                if (cm != null)
                                {
                                    // System.Diagnostics.Trace.WriteLine($"read {childProp.Name} {rt}");
                                    r.Types.Add(new NodeProps(cm, childProp));
                                    propFound = true;
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
                                        foreach (var rt in Hl7.Fhir.Model.ModelInfo.OpenTypes)
                                        {
                                            var cme = _mi.FindOrImportClassMapping(rt);
                                            if (cme != null)
                                            {
                                                // System.Diagnostics.Trace.WriteLine($"read {childProp.Name} {rt}");
                                                r.Types.Add(new NodeProps(cme, childProp));
                                                propFound = true;
                                            }
                                        }
                                        break;
                                    }
                                    r.Types.Add(new NodeProps(cm, childProp));
                                    propFound = true;
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
                                }
                            }
                            //var cm = _mi.FindOrImportClassMapping(childProp.ImplementingType);
                            //if (cm != null)
                            //{
                            //    r.Types.Add(new NodeProps(cm, childProp));
                            //    propFound = true;
                            //}
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
                        Details = new Hl7.Fhir.Model.CodeableConcept() { Text = $"prop '{ce.ChildName}' not found on {rFocus.ToString()}" }
                    };
                    if (expression.Location != null)
                        issue.Location = new[] { $"Line {expression.Location.LineNumber}, Position {expression.Location.LineNumber}" };
                    Outcome.AddIssue(issue);
                }
                _result.Append($"{ce.ChildName}");
                _result.AppendLine($" : {r.ToString()}");
                _stack.Pop();
                return r;
            }

            if (!rFocus.isRoot)
                _result.Append($".");
            _result.Append($"{expression.FunctionName}(");

            if (expression.FunctionName == "select"
                || passthroughFuncs.Contains(expression.FunctionName))
            {
                _stack.Push(rFocus);
            }


            incr();

            List<FhirPathVisitorProps> argTypes = new();
            foreach (var arg in expression.Arguments)
            {
                if (argTypes.Count > 0)
                    _result.Append(", ");
                argTypes.Add(arg.Accept(this));
            }

            decr();
            _result.Append($")");

            DeduceReturnType(expression, rFocus, argTypes, r);

            if (expression.FunctionName == "select"
                || passthroughFuncs.Contains(expression.FunctionName)
                )
            {
                _stack.Pop();
            }
            _result.AppendLine($" : {r.ToString()}");

            _stack.Pop();
            return r;
        }

        public override FhirPathVisitorProps VisitNewNodeListInit(NewNodeListInitExpression expression)
        {
            var r = new FhirPathVisitorProps();
            append("new NodeSet");
            // appendType(expression);

            incr();
            foreach (var element in expression.Contents)
                element.Accept(this);
            decr();

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
                _result.Append("$this");

                _result.AppendLine($" : {r.ToString()}");
                return r;
            }
            if (variables.ContainsKey(expression.Name))
            {
                _result.Append($"%{expression.Name}");
                r.Types.Add(new NodeProps(variables[expression.Name]));
                _result.AppendLine($" : {r.ToString()}");
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

        private void append(string text, bool newLine = true)
        {
            if (newLine)
            {
                _result.AppendLine();
                _result.Append(new String(' ', _indent * 4));
            }

            _result.Append(text);
        }

        private void incr()
        {
            _indent += 1;
        }

        private void decr()
        {
            _indent -= 1;
        }
    }
}
