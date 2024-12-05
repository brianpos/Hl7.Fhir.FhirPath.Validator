using Hl7.Fhir.FhirPath.Validator;
using Hl7.FhirPath.Expressions;
using Hl7.Fhir.Model;
using Hl7.Fhir.Specification.Source;
using System.Linq;
using Hl7.Fhir.Utility;
using System.Collections.Generic;
using Hl7.Fhir.Introspection;
using System;
using Hl7.Fhir.Support;

namespace Hl7.Fhir.FhirPath.Validator
{
	public class ExtensionResolvingFhirPathExpressionVisitor : BaseFhirPathExpressionVisitor
	{
		/// <summary>
		/// When validating invariants in an Extension profile, use this method
		/// to set the internal context details for the extension.
		/// This is only applicable for complex extensions.
		/// </summary>
		/// <param name="sd"></param>
		public void SetContextExtension(StructureDefinition sd)
		{
			if (sd.Type != "Extension")
				throw new ArgumentException("The structure defintion is not an extension to use as the context", nameof(sd));

			RootContext.AddAnnotation(new ExtensionAnnotation() { CanonicalUrl = sd.Url, StructureDefinition = sd });
		}

		public ExtensionResolvingFhirPathExpressionVisitor(IResourceResolver source, ModelInspector mi, List<string> SupportedResources, Type[] OpenTypes)
			: base(mi, SupportedResources, OpenTypes)
		{
			_source = source;
		}
		IResourceResolver _source;

		protected override void DeduceReturnType(FunctionCallExpression function, FhirPathVisitorProps focus, IEnumerable<FhirPathVisitorProps> props, FhirPathVisitorProps outputProps)
		{
			base.DeduceReturnType(function, focus, props, outputProps);
			if (function.FunctionName == "extension")
			{
				var extUrl = function.Arguments.FirstOrDefault(a => a is ConstantExpression) as ConstantExpression;
				if (extUrl != null)
				{
					var extAnnot = new ExtensionAnnotation()
					{
						CanonicalUrl = extUrl.Value.ToString(),
						ParentIsCollection = focus.IsCollection()
					};
					outputProps.SetAnnotation(extAnnot);

					// Check to see if this is a complex extension (and the focus also has extension details)
					if (focus.HasAnnotation<ExtensionAnnotation>())
					{
						// Complex extension
						var parentExt = focus.Annotation<ExtensionAnnotation>();
						extAnnot.Path = extAnnot.CanonicalUrl;
						extAnnot.CanonicalUrl = parentExt.CanonicalUrl;
						extAnnot.StructureDefinition = parentExt.StructureDefinition;
						extAnnot.ParentIsCollection = parentExt.ParentIsCollection || parentExt.IsCollection;

						// Check that this is a valid child of the parent extension and filter down the cardinalities
						var eds = extAnnot.StructureDefinition.Differential?.Element.FirstOrDefault(ed => ((ed.Fixed as FhirUri)?.Value == extAnnot.Path));
						if (eds != null)
						{
							var propPaths = extAnnot.StructureDefinition.Differential?.Element.FirstOrDefault(ed => ed.ElementId == eds.ElementId.Substring(0, eds.ElementId.Length - 4));
							var ValuePaths = extAnnot.StructureDefinition.Differential?.Element.FirstOrDefault(ed => ed.ElementId == eds.ElementId.Substring(0, eds.ElementId.Length - 4) + ".value[x]");
							if (propPaths != null && ValuePaths != null)
							{
								extAnnot.Path = propPaths.ElementId;
								extAnnot.IsCollection = !(propPaths?.Max == "1");
								if (!parentExt.ParentIsCollection && !parentExt.IsCollection && outputProps.Types.Count() == 1 && propPaths.Max == "1")
								{
									var singleProp = outputProps.Types.First().AsSingle();
									outputProps.Types.Clear();
									outputProps.Types.Add(singleProp);
								}
							}
						}
						else
						{
							// The property doesn't exist in this complex extension
							var issue = new Hl7.Fhir.Model.OperationOutcome.IssueComponent()
							{
								Severity = Hl7.Fhir.Model.OperationOutcome.IssueSeverity.Error,
								Code = Hl7.Fhir.Model.OperationOutcome.IssueType.NotFound,
								Details = new Hl7.Fhir.Model.CodeableConcept() { Text = $"Property '{extUrl.Value.ToString()}' does not exist in the complex extension {extAnnot.CanonicalUrl}" }
							};
							ReportErrorLocation(function, issue);
							Outcome.AddIssue(issue);
							// outputProps.Types.Clear(); // This would then lead to further issues such as `prop 'value' not found on ???` but don't think that helps anyone
						}
					}
					else
					{
						var extensionDefinition = _source?.FindExtensionDefinition(extAnnot.CanonicalUrl);
						if (extensionDefinition != null)
						{
							// This will be used later on to filter out types if the value is walked into
							extAnnot.StructureDefinition = extensionDefinition;
							extAnnot.Path = extensionDefinition.Differential?.Element.FirstOrDefault()?.ElementId;
							extAnnot.IsCollection = !(extensionDefinition.Differential?.Element.FirstOrDefault()?.Max == "1");

							// Filter down the extension array to singular if that's what the extension is defined as
							if (!focus.IsCollection() && !extAnnot.IsCollection && outputProps.Types.Count() == 1)
							{
								var singleProp = outputProps.Types.First().AsSingle();
								outputProps.Types.Clear();
								outputProps.Types.Add(singleProp with { Path = $"{focus.Types.FirstOrDefault().Path}.extension" });
							}
						}
						else
						{
							// Provide an informational message that the extension was not able to be resolved
							var issue = new Hl7.Fhir.Model.OperationOutcome.IssueComponent()
							{
								Severity = Hl7.Fhir.Model.OperationOutcome.IssueSeverity.Information,
								Code = Hl7.Fhir.Model.OperationOutcome.IssueType.NotFound,
								Details = new Hl7.Fhir.Model.CodeableConcept() { Text = $"Unable to resolve extension profile '{extAnnot.CanonicalUrl}'" }
							};
							ReportErrorLocation(function, issue);
							Outcome.AddIssue(issue);
						}
					}
				}
			}
			else if (function.FunctionName == "where" && focus.CanBeOfType("Extension"))
			{
				// Check the argument to the where function, explicitly looking for url = 'string'
				if (function.Arguments.Count() == 1 && function.Arguments.First() is BinaryExpression be)
				{
					if (be.Op == "=" && be.Arguments.Count() == 2)
					{
						var extPropExpr = be.Arguments.OfType<ChildExpression>().FirstOrDefault();
						var extUrl = be.Arguments.OfType<ConstantExpression>().FirstOrDefault();
						if (extPropExpr.ChildName == "url" && extUrl != null)
						{
							var extAnnot = new ExtensionAnnotation()
							{
								CanonicalUrl = extUrl.Value.ToString(),
								ParentIsCollection = focus.IsCollection()
							};
							outputProps.SetAnnotation(extAnnot);

							ExtensionAnnotation focusExt = null;
							if (focus.HasAnnotation<ExtensionAnnotation>())
								focusExt = focus.Annotation<ExtensionAnnotation>();
							if (focusExt?.StructureDefinition != null)
							{
								// This is the complex extension case
								var parentExt = focus.Annotation<ExtensionAnnotation>();
								extAnnot.Path = extAnnot.CanonicalUrl;
								extAnnot.CanonicalUrl = parentExt.CanonicalUrl;
								extAnnot.StructureDefinition = parentExt.StructureDefinition;
								extAnnot.ParentIsCollection = parentExt.ParentIsCollection || parentExt.IsCollection;

								// Check that this is a valid child of the parent extension and filter down the cardinalities
								var eds = extAnnot.StructureDefinition.Differential?.Element.FirstOrDefault(ed => ((ed.Fixed as FhirUri)?.Value == extAnnot.Path));
								if (eds != null)
								{
									var propPaths = extAnnot.StructureDefinition.Differential?.Element.FirstOrDefault(ed => ed.ElementId == eds.ElementId.Substring(0, eds.ElementId.Length - 4));
									var ValuePaths = extAnnot.StructureDefinition.Differential?.Element.FirstOrDefault(ed => ed.ElementId == eds.ElementId.Substring(0, eds.ElementId.Length - 4) + ".value[x]");
									if (propPaths != null && ValuePaths != null)
									{
										extAnnot.Path = propPaths.ElementId;
										extAnnot.IsCollection = !(propPaths?.Max == "1");
										if (!parentExt.ParentIsCollection && !parentExt.IsCollection && outputProps.Types.Count() == 1 && propPaths.Max == "1")
										{
											var singleProp = outputProps.Types.First().AsSingle();
											outputProps.Types.Clear();
											outputProps.Types.Add(singleProp);
										}
									}
								}
								else
								{
									// The property doesn't exist in this complex extension
									var issue = new Hl7.Fhir.Model.OperationOutcome.IssueComponent()
									{
										Severity = Hl7.Fhir.Model.OperationOutcome.IssueSeverity.Error,
										Code = Hl7.Fhir.Model.OperationOutcome.IssueType.NotFound,
										Details = new Hl7.Fhir.Model.CodeableConcept() { Text = $"Property '{extUrl.Value.ToString()}' does not exist in the complex extension {extAnnot.CanonicalUrl}" }
									};
									ReportErrorLocation(function, issue);
									Outcome.AddIssue(issue);
									// outputProps.Types.Clear(); // This would then lead to further issues such as `prop 'value' not found on ???` but don't think that helps anyone
								}

							}
							else
							{
								// This is our case!
								if (focusExt != null)
								{
									extAnnot.ParentIsCollection = focusExt.ParentIsCollection || focusExt.IsCollection;
								}

								var extensionDefinition = _source?.FindExtensionDefinition(extAnnot.CanonicalUrl);
								if (extensionDefinition != null)
								{
									// This will be used later on to filter out types if the value is walked into
									extAnnot.StructureDefinition = extensionDefinition;
									extAnnot.Path = extensionDefinition.Differential?.Element.FirstOrDefault()?.ElementId;
									extAnnot.IsCollection = !(extensionDefinition.Differential?.Element.FirstOrDefault()?.Max == "1");

									// Filter down the extension array to singular if that's what the extension is defined as
									// TODO: This needs to check the parent of the collection, not the parent of the where!
									if (!focus.Annotation<ExtensionAnnotation>().ParentIsCollection && outputProps.Types.Count() == 1 && extensionDefinition.Differential?.Element.FirstOrDefault()?.Max == "1")
									{
										var singleProp = outputProps.Types.First().AsSingle();
										outputProps.Types.Clear();
										outputProps.Types.Add(singleProp);
									}
								}
								else
								{
									// Provide an informational message that the extension was not able to be resolved
									var issue = new Hl7.Fhir.Model.OperationOutcome.IssueComponent()
									{
										Severity = Hl7.Fhir.Model.OperationOutcome.IssueSeverity.Information,
										Code = Hl7.Fhir.Model.OperationOutcome.IssueType.NotFound,
										Details = new Hl7.Fhir.Model.CodeableConcept() { Text = $"Unable to resolve extension profile '{extAnnot.CanonicalUrl}'" }
									};
									ReportErrorLocation(function, issue);
									Outcome.AddIssue(issue);
								}
							}
						}
					}
				}
			}
			else if (passthroughFuncs.Contains(function.FunctionName))
			{
				// passthrough our extension annotation
				if (focus.HasAnnotation<ExtensionAnnotation>())
				{
					outputProps.SetAnnotation(focus.Annotation<ExtensionAnnotation>());
				}
			}
		}

		protected override void VisitChildExpression(FunctionCallExpression expression, FhirPathVisitorProps result, FhirPathVisitorProps rFocus, ChildExpression ce)
		{
			base.VisitChildExpression(expression, result, rFocus, ce);

			// walking into the extension property, lets kick things off with the known focus collection state
			if (ce.ChildName == "extension" || ce.ChildName == "modifierExtension")
			{
				var childAnnotation = new ExtensionAnnotation() { ParentIsCollection = rFocus.IsCollection() };
				result.SetAnnotation(childAnnotation);
				if (rFocus.HasAnnotation<ExtensionAnnotation>())
				{
					var focusAnnotation = rFocus.Annotation<ExtensionAnnotation>();
					childAnnotation.StructureDefinition = focusAnnotation.StructureDefinition;
					childAnnotation.CanonicalUrl = focusAnnotation.CanonicalUrl;
					childAnnotation.ParentIsCollection = rFocus.IsCollection() || focusAnnotation.IsCollection;
				}
			}

			// If this is an extension walking into it's value, check if the extension is available, and which value types are applicable.
			if (rFocus.HasAnnotation<ExtensionAnnotation>() && ce.ChildName == "value")
			{
				// Get the extension definition
				var extensionDefinition = rFocus.Annotation<ExtensionAnnotation>();
				if (extensionDefinition.StructureDefinition != null)
				{
					// Get the value type
					var edValue = extensionDefinition.StructureDefinition.Differential?.Element.FirstOrDefault(ed => ed.ElementId == $"{extensionDefinition.Path}.value[x]");
					if (edValue != null)
					{
						// Find the types that have been constrained out
						var excludedTypes = result.Types.Where(t => !edValue.Type.Any(extType => extType.Code == t.ClassMapping.Name)).ToArray();

						// Remove them from the result
						foreach (var typeToRemove in excludedTypes)
							result.Types.Remove(typeToRemove);

						var edSlice = extensionDefinition.StructureDefinition.Differential?.Element.FirstOrDefault(ed => ed.ElementId == $"{extensionDefinition.Path}");
						if (edSlice != null)
							AppendLine($"// Extension '{edSlice.Label ?? edSlice.Short ?? edSlice.Definition ?? extensionDefinition.StructureDefinition.Title ?? extensionDefinition.StructureDefinition.Name}' constrained type to : {result.TypeNames()}");
						else
							AppendLine($"// Extension {extensionDefinition.StructureDefinition.Title ?? extensionDefinition.StructureDefinition.Name} constrained type to : {result.TypeNames()}");
					}
				}
			}
		}

		class ExtensionAnnotation
		{
			public bool ParentIsCollection { get; set; }
			public bool IsCollection { get; set; }
			public string CanonicalUrl { get; set; }
			public string Path { get; set; }
			public StructureDefinition StructureDefinition { get; set; }
		}
	}
}