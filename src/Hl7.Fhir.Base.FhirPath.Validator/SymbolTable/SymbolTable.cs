using Hl7.Fhir.Introspection;
using Hl7.Fhir.Model;
using Hl7.Fhir.Support;
using Hl7.FhirPath.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;

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

			Add(new FunctionDefinition("toBoolean").AddContexts(mi, "boolean-boolean,integer-boolean,decimal-boolean,string-boolean")).Validations.Add(ValidateNoArguments);
			Add(new FunctionDefinition("toInteger").AddContexts(mi, "integer-integer,string-integer,boolean-integer")).Validations.Add(ValidateNoArguments);
			// TODO: What about the `Instant` type here?
			Add(new FunctionDefinition("toDate").AddContexts(mi, "string-date,date-date,dateTime-date")).Validations.Add(ValidateNoArguments);
			Add(new FunctionDefinition("toDateTime").AddContexts(mi, "string-dateTime,date-dateTime,dateTime-dateTime")).Validations.Add(ValidateNoArguments);
			Add(new FunctionDefinition("toDecimal").AddContexts(mi, "decimal-decimal,integer-decimal,string-decimal,boolean-decimal")).Validations.Add(ValidateNoArguments);
			Add(new FunctionDefinition("toQuantity").AddContexts(mi, "integer-Quantity,decimal-Quantity,string-Quantity,Quantity-Quantity,boolean-Quantity")); // can have an optional string argument for units
			Add(new FunctionDefinition("toString").AddContexts(mi, "string-string,integer-string,decimal-string,date-string,dateTime-string,Time-string,Quantity-string")).Validations.Add(ValidateNoArguments);
			Add(new FunctionDefinition("toTime").AddContexts(mi, "time-time,string-time")).Validations.Add(ValidateNoArguments);

			// Add(new FunctionItem("false", false));
			// Add(new FunctionItem("null", null));

			Add(new FunctionDefinition("today", false, true) { GetReturnType = ReturnsDate }).Validations.Add(ValidateNoArguments);
			Add(new FunctionDefinition("now", false, true) { GetReturnType = Returns<FhirDateTime> }).Validations.Add(ValidateNoArguments);
			Add(new FunctionDefinition("timeOfDay", false, true) { GetReturnType = ReturnsTime }).Validations.Add(ValidateNoArguments);
			Add(new FunctionDefinition("iif", false, true) { GetReturnType = IifReturnsType, SupportsContext = IifSupportsContext }).Validations.Add(ValidateRequiredBooleanFirstArgument);

			// Add(new FunctionDefinition("allTrue", true, false) { GetReturnType = ReturnsBoolean }).Validations.Add(ValidateNoArguments);
			Add(new FunctionDefinition("unary.-", false, true).AddContexts(mi, "integer-integer,decimal-decimal"));
			Add(new FunctionDefinition("unary.+", false, true).AddContexts(mi, "integer-integer,decimal-decimal"));
			Add(new FunctionDefinition("descendants", true, false) { GetReturnType = ReturnsFromDescendants, SupportsContext = (props) => true }).Validations.Add(ValidateNoArguments);

			// Math functions
			Add(new FunctionDefinition("abs").AddContexts(mi, "integer-integer,decimal-decimal,Quantity-Quantity")).Validations.Add(ValidateNoArguments);
			Add(new FunctionDefinition("ceiling").AddContexts(mi, "integer-integer,decimal-integer")).Validations.Add(ValidateNoArguments);
			Add(new FunctionDefinition("exp").AddContexts(mi, "integer-decimal,decimal-decimal")).Validations.Add(ValidateNoArguments);
			Add(new FunctionDefinition("floor").AddContexts(mi, "integer-integer,decimal-integer")).Validations.Add(ValidateNoArguments);
			Add(new FunctionDefinition("ln").AddContexts(mi, "integer-decimal,decimal-decimal")).Validations.Add(ValidateNoArguments);
			Add(new FunctionDefinition("log").AddContexts(mi, "integer-decimal,decimal-decimal")); // requires a "base":decimal argument
			Add(new FunctionDefinition("power").AddContexts(mi, "integer-integer,decimal-decimal")); // requires an exponent:integer|decimal argument
			Add(new FunctionDefinition("round").AddContexts(mi, "decimal-decimal,integer-decimal")); // requires a precision:integer argument
			Add(new FunctionDefinition("sqrt").AddContexts(mi, "integer-decimal,decimal-decimal")).Validations.Add(ValidateNoArguments);
			Add(new FunctionDefinition("truncate").AddContexts(mi, "integer-integer,decimal-integer")).Validations.Add(ValidateNoArguments);

			// SDC additional functions
			Add(new FunctionDefinition("answers", false, true) { GetReturnType = ReturnsAnswers }).Validations.Add(ValidateNoArguments);
		}

		public FunctionDefinition Add(FunctionDefinition item)
		{
			_items.Add(item.Name, item);
			return item;
		}

		public FunctionDefinition Add(FunctionDefinition item,
            FunctionDefinition.GetReturnTypeDelegate getReturnType,
			Action<FunctionDefinition, IEnumerable<FhirPathVisitorProps>, OperationOutcome> validate)
		{
			item.GetReturnType = getReturnType;
			item.Validations.Add(validate);
			_items.Add(item.Name, item);
			return item;
		}
		public FunctionDefinition Add(FunctionDefinition item,
			FunctionDefinition.GetReturnTypeDelegate getReturnType,
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

		List<NodeProps> Returns<T>(FunctionDefinition item, FhirPathVisitorProps focus, IEnumerable<FhirPathVisitorProps> args, OperationOutcome outcome)
		{
			// Result is just an integer
			return FromType(typeof(T)).ToList();
		}

		List<NodeProps> ReturnsAnswers(FunctionDefinition item, FhirPathVisitorProps focus, IEnumerable<FhirPathVisitorProps> args, OperationOutcome outcome)
		{
			var t = _mi.GetTypeForFhirType("QuestionnaireResponse#Answer");
			// Result is just an integer
			return FromType(t).AsCollection().ToList();
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

		bool IifSupportsContext(FhirPathVisitorProps focus)
		{
			if (focus.Types.Any(t => t.IsCollection))
				return false;
			return true;
		}
		List<NodeProps> IifReturnsType(FunctionDefinition item, FhirPathVisitorProps focus, IEnumerable<FhirPathVisitorProps> args, OperationOutcome outcome)
		{
			// Result type will be the union of the types of the args
			var result = new List<NodeProps>();
			if (args.Count() > 1)
			{
				foreach (var arg in args.Skip(1))
				{
					foreach (var t2 in arg.Types)
					{
						// This is different to other places, as these types don't need to mesh into eachother
						if (!result.Any(t => t.ToString() == t2.ToString()))
						{
							result.Add(t2);
						}
					}
				}
			}
			return result;
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
		void ValidateRequiredBooleanFirstArgument(FunctionDefinition item, IEnumerable<FhirPathVisitorProps> args, OperationOutcome outcome)
		{
			if (args?.Any() == false)
			{
				var issue = new Hl7.Fhir.Model.OperationOutcome.IssueComponent()
				{
					Severity = Hl7.Fhir.Model.OperationOutcome.IssueSeverity.Error,
					Code = Hl7.Fhir.Model.OperationOutcome.IssueType.Invalid,
					Details = new Hl7.Fhir.Model.CodeableConcept() { Text = $"Function '{item.Name}' requires a boolean first parameter" }
				};
				//if (item.Location != null)
				//	issue.Location = new[] { $"Line {function.Location.LineNumber}, Position {function.Location.LineNumber}" };
				outcome.Issue.Add(issue);
				return;
			}

			// We have some parameters, so lets check the first one for a boolean result!
			if (!args.First().CanBeOfType("boolean", true))
			{
				var issue = new Hl7.Fhir.Model.OperationOutcome.IssueComponent()
				{
					Severity = Hl7.Fhir.Model.OperationOutcome.IssueSeverity.Error,
					Code = Hl7.Fhir.Model.OperationOutcome.IssueType.Invalid,
					Details = new Hl7.Fhir.Model.CodeableConcept() { Text = $"Function '{item.Name}' requires a boolean first parameter" }
				};
				//if (item.Location != null)
				//	issue.Location = new[] { $"Line {function.Location.LineNumber}, Position {function.Location.LineNumber}" };
				outcome.Issue.Add(issue);
				return;
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

		internal static bool IsSupportedContext(this FunctionDefinition me, FhirPathVisitorProps focus, FunctionCallExpression function, OperationOutcome outcome)
		{
			if (me.SupportedAtRoot && focus.isRoot)
				return true;

			// Give the delegate first choice to override success (it can handle collections internally)
			if (me.SupportsContext != null && me.SupportsContext(focus))
				return true;

			if (!me.SupportedContexts.Any(sc => focus.CanBeOfType(sc.Type)))
			{
				var issue = new Hl7.Fhir.Model.OperationOutcome.IssueComponent()
				{
					Severity = Hl7.Fhir.Model.OperationOutcome.IssueSeverity.Error,
					Code = Hl7.Fhir.Model.OperationOutcome.IssueType.NotSupported,
					Details = new Hl7.Fhir.Model.CodeableConcept() { Text = $"Function '{function.FunctionName}' is not supported on context type '{focus}'" }
				};
				if (function.Location != null)
					issue.Location = new[] { $"Line {function.Location.LineNumber}, Position {function.Location.LineNumber}" };
				outcome.AddIssue(issue);
				return false;
			}

			if (!me.SupportsCollections && focus.IsCollection())
			{
				var issueCol = new Hl7.Fhir.Model.OperationOutcome.IssueComponent()
				{
					Severity = Hl7.Fhir.Model.OperationOutcome.IssueSeverity.Warning,
					Code = Hl7.Fhir.Model.OperationOutcome.IssueType.MultipleMatches,
					Details = new Hl7.Fhir.Model.CodeableConcept() { Text = $"Function '{function.FunctionName}' can experience unexpected runtime errors when used with a collection" },
				};
				if (function.Location != null)
					issueCol.Location = new[] { $"Line {function.Location.LineNumber}, Position {function.Location.LineNumber}" };
				outcome.AddIssue(issueCol);
				return true;
			}

			return true;
		}

		internal static List<NodeProps> ToList(this NodeProps me)
		{
			List<NodeProps> result = new List<NodeProps>();
			result.Add(me);
			return result;
		}
	}
}
