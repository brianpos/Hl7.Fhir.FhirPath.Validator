﻿using Hl7.Fhir.Introspection;
using Hl7.Fhir.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using static Hl7.Fhir.FhirPath.Validator.FunctionDefinition;

namespace Hl7.Fhir.FhirPath.Validator
{
	internal class SymbolTable
	{
		private readonly Dictionary<string, FunctionDefinition> _items = new();
		ModelInspector _mi;
		Type[] _openTypes;
		List<string> _supportedResources;

		public SymbolTable(ModelInspector mi, List<string> SupportedResources, Type[] OpenTypes)
		{
			_mi = mi;
			_supportedResources = SupportedResources;
			_openTypes = OpenTypes;
			Add(new FunctionDefinition("lowBoundary").AddContexts(mi, "date-date,instant-instant,decimal-decimal,integer-integer,dateTime-dateTime,time-time,Quantity-Quantity"));
			Add(new FunctionDefinition("highBoundary").AddContexts(mi, "date-date,instant-instant,decimal-decimal,integer-integer,dateTime-dateTime,time-time,Quantity-Quantity"));

			Add(new FunctionDefinition("length").AddContexts(mi, "string-integer")).Validations.Add(ValidateNoArguments);
			Add(new FunctionDefinition("toQuantity").AddContexts(mi, "integer-Quantity,decimal-Quantity,string-Quantity,Quantity-Quantity,boolean-Quantity")).Validations.Add(ValidateNoArguments);
			Add(new FunctionDefinition("toInteger").AddContexts(mi, "integer-integer,decimal-integer,string-integer,boolean-integer")).Validations.Add(ValidateNoArguments);
			// Add(new FunctionItem("false", false));
			// Add(new FunctionItem("null", null));

			Add(new FunctionDefinition("today", false, true) { GetReturnType = ReturnsDate }).Validations.Add(ValidateNoArguments);
			Add(new FunctionDefinition("now", false, true) { GetReturnType = ReturnsDateTime }).Validations.Add(ValidateNoArguments);
			Add(new FunctionDefinition("timeOfDay", false, true) { GetReturnType = ReturnsTime }).Validations.Add(ValidateNoArguments);

			// Add(new FunctionDefinition("allTrue", true, false) { GetReturnType = ReturnsBoolean }).Validations.Add(ValidateNoArguments);
			// Add(new FunctionDefinition("unary.-", false, true).AddContexts(mi, "integer-integer"));
			// Add(new FunctionDefinition("unary.+", false, true).AddContexts(mi, "integer-integer"));
			Add(new FunctionDefinition("descendants", true, false) { GetReturnType = ReturnsFromDescendants, SupportsContext = (props) => true }).Validations.Add(ValidateNoArguments);
		}

		public FunctionDefinition Add(FunctionDefinition item)
		{
			_items.Add(item.Name, item);
			return item;
		}

		public FunctionDefinition Add(FunctionDefinition item,
			GetReturnTypeDelegate getReturnType,
			Action<FunctionDefinition, IEnumerable<FhirPathVisitorProps>, OperationOutcome> validate)
		{
			item.GetReturnType = getReturnType;
			item.Validations.Add(validate);
			_items.Add(item.Name, item);
			return item;
		}
		public FunctionDefinition Add(FunctionDefinition item,
			GetReturnTypeDelegate getReturnType,
			List<Action<FunctionDefinition, IEnumerable<FhirPathVisitorProps>, OperationOutcome>> validate)
		{
			item.GetReturnType = getReturnType;
			item.Validations.AddRange(validate);
			_items.Add(item.Name, item);
			return item;
		}

		public FunctionDefinition Get(string name)
		{
			if (_items.TryGetValue(name, out var result))
				return result;
			return null;
		}

		NodeProps FromType(Type fhirType)
		{
			return new NodeProps(_mi.FindOrImportClassMapping(fhirType));
		}

		List<NodeProps> ReturnsDateTime(FunctionDefinition item, FhirPathVisitorProps focus, IEnumerable<FhirPathVisitorProps> args, OperationOutcome outcome)
		{
			// Result is just an integer
			return FromType(typeof(FhirDateTime)).ToList();
		}

		List<NodeProps> ReturnsDate(FunctionDefinition item, FhirPathVisitorProps focus, IEnumerable<FhirPathVisitorProps> args, OperationOutcome outcome)
		{
			// Result is just an integer
			return FromType(typeof(Date)).ToList();
		}

		List<NodeProps> ReturnsBoolean(FunctionDefinition item, FhirPathVisitorProps focus, IEnumerable<FhirPathVisitorProps> args, OperationOutcome outcome)
		{
			// Result is just an integer
			return FromType(typeof(FhirBoolean)).ToList();
		}

		List<NodeProps> ReturnsTime(FunctionDefinition item, FhirPathVisitorProps focus, IEnumerable<FhirPathVisitorProps> args, OperationOutcome outcome)
		{
			// Result is just an integer
			return FromType(typeof(Time)).ToList();
		}

		List<NodeProps> ReturnsInteger(FunctionDefinition item, FhirPathVisitorProps focus, IEnumerable<FhirPathVisitorProps> args, OperationOutcome outcome)
		{
			// Result is just an integer
			return FromType(typeof(Integer)).ToList();
		}

		List<NodeProps> ReturnsFromDescendants(FunctionDefinition item, FhirPathVisitorProps focus, IEnumerable<FhirPathVisitorProps> args, OperationOutcome outcome)
		{
			// This is the most complex part, here we need to scan the type(s) in the focus, then for every property in that type, add in it's type, then recurse over this collection till none are left.
			List<ClassMapping> allMaps = new();
			foreach (var focusType in focus.Types)
			{
				ScanTypeForNewChildPropTypes(allMaps, focusType.ClassMapping);
			}

			return allMaps.Select(m => new NodeProps(m)).ToList();
		}

		private void ScanTypeForNewChildPropTypes(List<ClassMapping> allMaps, ClassMapping focusType)
		{
			// System.Diagnostics.Trace.WriteLine($"Scanning: {focusType.Name}");
			foreach (var focusChildProperty in focusType.PropertyMappings)
			{
				if (focusChildProperty.Choice == ChoiceType.DatatypeChoice && focusChildProperty.FhirType.Length == 1 && focusChildProperty.FhirType[0].Name == "DataType")
				{
					var cmt = _openTypes.Select(ot => _mi.FindOrImportClassMapping(ot));
					foreach (var pm in cmt)
					{
						if (pm != null && !allMaps.Contains(pm))
						{
							allMaps.Add(pm);
							ScanTypeForNewChildPropTypes(allMaps, pm);
						}
					}
				}
				else if (focusChildProperty.FhirType.Length > 1)
				{
					// Scan for each of these fhir types
					foreach (var pm in focusChildProperty.FhirType.Select(ft => _mi.FindOrImportClassMapping(ft)))
					{
						if (pm != null && !allMaps.Contains(pm))
						{
							allMaps.Add(pm);
							ScanTypeForNewChildPropTypes(allMaps, pm);
						}
					}
				}
				else if (focusChildProperty.Choice == ChoiceType.None)
				{
					if (!allMaps.Contains(focusChildProperty.PropertyTypeMapping))
					{
						allMaps.Add(focusChildProperty.PropertyTypeMapping);
						ScanTypeForNewChildPropTypes(allMaps, focusChildProperty.PropertyTypeMapping);
					}
				}
				else
				{
					var cmt = _supportedResources.Select(sr => _mi.FindClassMapping(sr));
					foreach (var pm in cmt)
					{
						if (pm != null && !allMaps.Contains(pm))
						{
							allMaps.Add(pm);
							ScanTypeForNewChildPropTypes(allMaps, pm);
						}
					}
				}
			}
		}

		void ValidateNoArguments(FunctionDefinition item, IEnumerable<FhirPathVisitorProps> args, OperationOutcome outcome)
		{
			if (args?.Any() == true)
			{
				var issue = new Hl7.Fhir.Model.OperationOutcome.IssueComponent()
				{
					Severity = Hl7.Fhir.Model.OperationOutcome.IssueSeverity.Error,
					Code = Hl7.Fhir.Model.OperationOutcome.IssueType.Invalid,
					Details = new Hl7.Fhir.Model.CodeableConcept() { Text = $"Function '{item.Name}' does not require any parameters" }
				};
				//if (item.Location != null)
				//	issue.Location = new[] { $"Line {function.Location.LineNumber}, Position {function.Location.LineNumber}" };
				outcome.Issue.Add(issue);
			}
		}
	}


	internal static class ExtFuncs
	{
		internal static FunctionDefinition AddContexts(this FunctionDefinition me, ModelInspector mi, string contexts)
		{
			me.SupportedContexts.AddRange(contexts.Split(',').Select(v =>
			{
				var t = v.Split('-');
				var cm = mi.FindClassMapping(t[1]);
				if (cm == null)
					throw new ArgumentException($"{t[1]} is not a registered FHIR type (for the symbol table)");
				return new FunctionContext(t[0], cm);
			}));
			return me;
		}

		internal static bool IsSupportedContext(this FunctionDefinition me, FhirPathVisitorProps focus)
		{
			if (me.SupportedAtRoot && focus.isRoot)
				return true;

			if (!me.SupportsCollections && focus.IsCollection())
				return false;

			if (me.SupportsContext != null && me.SupportsContext(focus))
				return true;

			if (me.SupportedContexts.Any(sc => focus.CanBeOfType(sc.Type)))
				return true;
			return false;
		}

		internal static List<NodeProps> ToList(this NodeProps me)
		{
			List<NodeProps> result = new List<NodeProps>();
			result.Add(me);
			return result;
		}
	}
}
