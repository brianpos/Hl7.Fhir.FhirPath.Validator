using Hl7.Fhir.FhirPath.Validator;
using Hl7.FhirPath.Expressions;
using Hl7.Fhir.Model;
using Hl7.Fhir.Specification.Source;
using System.Linq;
using Hl7.Fhir.Utility;
using System.Collections.Generic;
using Hl7.Fhir.Introspection;
using System;

namespace Hl7.Fhir.FhirPath.Validator
{
	public class ExtensionResolvingFhirPathExpressionVisitor : BaseFhirPathExpressionVisitor
	{
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
					outputProps.SetAnnotation(extUrl);

					var extensionDefinition = _source?.FindExtensionDefinition(extUrl.Value.ToString());
					if (extensionDefinition != null)
					{
						// This will be used later on to filter out types if the value is walked into
						outputProps.SetAnnotation(extensionDefinition);

						// Filter down the extension array to singular if that's what the extension is defined as
						if (!focus.IsCollection() && outputProps.Types.Count() == 1 && extensionDefinition.Differential?.Element.FirstOrDefault()?.Max == "1")
						{
							var singleProp = outputProps.Types.First().AsSingle();
							outputProps.Types.Clear();
							outputProps.Types.Add(singleProp);
						}
					}
				}
			}
			if (function.FunctionName == "where" && focus.CanBeOfType("Extension"))
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
							// This is our case!
							outputProps.SetAnnotation(extUrl);

							var extensionDefinition = _source?.FindExtensionDefinition(extUrl.Value.ToString());
							if (extensionDefinition != null)
							{
								// This will be used later on to filter out types if the value is walked into
								outputProps.SetAnnotation(extensionDefinition);

								// Filter down the extension array to singular if that's what the extension is defined as
								// TODO: This needs to check the parent of the collection, not the parent of the where!
								if (!focus.Annotation<ExtensionAnnotation>().ParentIsCollection && outputProps.Types.Count() == 1 && extensionDefinition.Differential?.Element.FirstOrDefault()?.Max == "1")
								{
									var singleProp = outputProps.Types.First().AsSingle();
									outputProps.Types.Clear();
									outputProps.Types.Add(singleProp);
								}
							}
						}
					}
				}
			}
		}

		protected override void VisitChildExpression(FunctionCallExpression expression, FhirPathVisitorProps result, FhirPathVisitorProps rFocus, ChildExpression ce)
		{
			base.VisitChildExpression(expression, result, rFocus, ce);
			
			// If this is an extension walking into it's value, check if the extension is available, and which value types are applicable.
			if (rFocus.CanBeOfType("Extension") && ce.ChildName == "value")
			{
				if (rFocus.HasAnnotation<StructureDefinition>())
				{
					// Get the extension definition
					var extensionDefinition = rFocus.Annotation<StructureDefinition>();
					if (extensionDefinition != null)
					{
						// Get the value type
						var edValue = extensionDefinition.Differential.Element.FirstOrDefault(ed => ed.Path == "Extension.value[x]");
						if (edValue != null)
						{
							// Find the types that have been constrained out
							var excludedTypes = result.Types.Where(t => !edValue.Type.Any(extType => extType.Code == t.ClassMapping.Name)).ToArray();

							// Remove them from the result
							foreach (var typeToRemove in excludedTypes)
								result.Types.Remove(typeToRemove);

							AppendLine($"// Extension {extensionDefinition.Title ?? extensionDefinition.Name} constrained type to : {result.TypeNames()}");
						}
					}
				}
			}
			if (ce.ChildName == "extension")
			{
				result.SetAnnotation(new ExtensionAnnotation() { ParentIsCollection = rFocus.IsCollection() });
			}
		}

		class ExtensionAnnotation
		{
			public bool ParentIsCollection { get;set; }
		}
	}
}