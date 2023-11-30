using Hl7.Fhir.FhirPath.Validator;
using Hl7.Fhir.Introspection;
using Hl7.FhirPath.Expressions;
using Hl7.FhirPath;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Model;
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Hl7.Fhir.Specification.Source;
using System.Linq;
using Hl7.Fhir.Utility;
using System.Collections.Generic;
using System.Reflection.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.ComponentModel;
using Hl7.Fhir.Support;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Test.Fhir.FhirPath.Validator
{
	public class QuestionnaireFhirPathExpressionVisitor : FhirPathExpressionVisitor
	{
		public QuestionnaireFhirPathExpressionVisitor()
		{
		}
		public Questionnaire _questionnaireDefinition;
		public Questionnaire QuestionnaireDefinition
		{
			get { return _questionnaireDefinition; }
			set
			{
				_questionnaireDefinition = value;
				itemsByLinkId.Clear();
				IndexItems(_questionnaireDefinition.Item);
			}
		}

		private void IndexItems(List<Questionnaire.ItemComponent> items)
		{
			foreach (var item in items)
			{
				if (!itemsByLinkId.ContainsKey(item.LinkId))
				{
					itemsByLinkId.Add(item.LinkId, item);
				}
				else
				{
					// There are duplicate linkIds in the questionnaire!
				}

				if (item.Item.Any())
					IndexItems(item.Item);
			}
		}

		public Questionnaire.ItemComponent FocusedRootItem { get; set; }

		public Dictionary<string, Questionnaire.ItemComponent> itemsByLinkId = new Dictionary<string, Questionnaire.ItemComponent>();

		protected override void VisitBinaryExpression(FunctionCallExpression expression, FhirPathVisitorProps result, BinaryExpression be)
		{
			base.VisitBinaryExpression(expression, result, be);
			// Check if the expression is on a linkId
			var childExpression = be.Arguments.OfType<ChildExpression>().FirstOrDefault();
			var constExpression = be.Arguments.OfType<ConstantExpression>().FirstOrDefault();
			if (childExpression?.ChildName == "linkId" && constExpression?.Value is string linkIdValue) // need to check focus too
			{
				// Check if this linkId exists in the actual questionnaire
				if (!itemsByLinkId.ContainsKey(linkIdValue)) 
				{
					// Log the error
					var issue = new Hl7.Fhir.Model.OperationOutcome.IssueComponent()
					{
						Severity = Hl7.Fhir.Model.OperationOutcome.IssueSeverity.Error,
						Code = Hl7.Fhir.Model.OperationOutcome.IssueType.Value,
						Details = new Hl7.Fhir.Model.CodeableConcept() { Text = $"LinkId '{linkIdValue}' does not exist in this questionnaire." }
					};
					if (expression.Location != null)
						issue.Location = new[] { $"Line {constExpression.Location.LineNumber}, Position {constExpression.Location.LineNumber}" };
					Outcome.AddIssue(issue);
				}
			}
		}

		protected override void DeduceReturnType(FunctionCallExpression function, FhirPathVisitorProps focus, IEnumerable<FhirPathVisitorProps> props, FhirPathVisitorProps outputProps)
		{
			base.DeduceReturnType(function, focus, props, outputProps);
			if (function.FunctionName == "where" && focus.HasAnnotation<ItemAnnotation>())
			{
				// Check the argument to the where function, explicitly looking for url = 'string'
				if (function.Arguments.Count() == 1 && function.Arguments.First() is BinaryExpression be)
				{
					if (be.Op == "=" && be.Arguments.Count() == 2 && be.Arguments.First() is ChildExpression extPropExpr && be.Arguments.Skip(1).First() is ConstantExpression linkIdValue)
					{
						if (extPropExpr.ChildName == "linkId")
						{
							// This is our case!
							outputProps.SetAnnotation(linkIdValue);
							var parentAnnot = focus.Annotation<ItemAnnotation>();
							outputProps.SetAnnotation(parentAnnot);

							// filter out the items here?
							foreach (var item in parentAnnot.Item.Where(i => i.LinkId != linkIdValue.Value.ToString()).ToArray())
								parentAnnot.Item.Remove(item);
						}
					}
				}
			}
		}

		protected override void VisitChildExpression(FunctionCallExpression expression, FhirPathVisitorProps result, FhirPathVisitorProps rFocus, ChildExpression ce)
		{
			base.VisitChildExpression(expression, result, rFocus, ce);

			// If this is an extension walking into it's value, check if the extension is available, and which value types are applicable.
			if (rFocus.CanBeOfType("QuestionnaireResponse"))
			{
				if (ce.ChildName == "item")
				{
					var itemAnnot = new ItemAnnotation() { ParentIsCollection = rFocus.IsCollection() };
					itemAnnot.Item.AddRange(QuestionnaireDefinition.Item);
					result.SetAnnotation(itemAnnot);
				}
			}

			if (ce.ChildName == "item" && rFocus.HasAnnotation<ItemAnnotation>())
			{
				// We can walk into the child items from here
				var itemAnnot = new ItemAnnotation() { ParentIsCollection = rFocus.IsCollection() };
				result.SetAnnotation(itemAnnot);

				var focusItems = rFocus.Annotation<ItemAnnotation>().Item;
				foreach (var focusItem in focusItems)
				{
					itemAnnot.Item.AddRange(focusItem.Item);
				}
			}

			if (ce.ChildName == "answer" && rFocus.HasAnnotation<ItemAnnotation>())
			{
				result.SetAnnotation(rFocus.Annotation<ItemAnnotation>());
				if (rFocus.HasAnnotation<ConstantExpression>())
					result.SetAnnotation(rFocus.Annotation<ConstantExpression>());
			}

			if (ce.ChildName == "value" && rFocus.HasAnnotation<ItemAnnotation>())
			{
				// Now we need to actually filter those types!
				var itemAnnot = rFocus.Annotation<ItemAnnotation>();
				var constValue = rFocus.Annotation<ConstantExpression>();

				List<NodeProps> newResult = new();
				foreach (var itemDef in itemAnnot.Item)
				{
					var includeTypes = result.Types.Where(t => MapDataType(itemDef.Type).Contains(t.ClassMapping.Name)).ToArray();
					foreach (var it in includeTypes)
					{
						if (itemAnnot.ParentIsCollection || itemDef.Repeats == true)
							newResult.Add(it.AsCollection());
						else
							newResult.Add(it.AsSingle());
					}
				}

				result.Types.Clear();
				foreach (var nt in newResult)
					result.Types.Add(nt);

				AppendLine($"// Items constrained type to : {result.TypeNames()}");
			}
		}

		public static IEnumerable<string> MapDataType(Questionnaire.QuestionnaireItemType? type)
		{
			return type switch
			{
				// non-value type items will constrain to no acceptable types
				Questionnaire.QuestionnaireItemType.Group => new string[] { },
				Questionnaire.QuestionnaireItemType.Display => new string[] { },
				Questionnaire.QuestionnaireItemType.Question => new string[] { },

				// All the regular types can be used here
				Questionnaire.QuestionnaireItemType.Boolean => new[] { "boolean" },
				Questionnaire.QuestionnaireItemType.Decimal => new[] { "decimal" },
				Questionnaire.QuestionnaireItemType.Integer => new[] { "integer" },
				Questionnaire.QuestionnaireItemType.Date => new[] { "date" },
				Questionnaire.QuestionnaireItemType.DateTime => new[] { "dateTime" },
				Questionnaire.QuestionnaireItemType.Time => new[] { "time" },
				Questionnaire.QuestionnaireItemType.String => new[] { "string" },
				Questionnaire.QuestionnaireItemType.Text => new[] { "string" },
				Questionnaire.QuestionnaireItemType.Url => new[] { "string" },
				Questionnaire.QuestionnaireItemType.Choice => new[] { "Coding" },
				Questionnaire.QuestionnaireItemType.OpenChoice => new[] { "Coding", "string" },
				Questionnaire.QuestionnaireItemType.Attachment => new[] { "Attachment" },
				Questionnaire.QuestionnaireItemType.Reference => new[] { "Reference" },
				Questionnaire.QuestionnaireItemType.Quantity => new[] { "Quantity" },
				_ => new string[] { }
			};
		}

		class ItemAnnotation
		{
			public bool ParentIsCollection { get; set; }
			public List<Questionnaire.ItemComponent> Item { get; } = new List<Questionnaire.ItemComponent>();
		}
	}

	[TestClass]
	public class QuestionnaireTests
	{
		FhirPathCompiler _compiler;

		[TestInitialize]
		public void Init()
		{
			Hl7.Fhir.FhirPath.ElementNavFhirExtensions.PrepareFhirSymbolTableFunctions();
			SymbolTable symbolTable = new(FhirPathCompiler.DefaultSymbolTable);
			_compiler = new FhirPathCompiler(symbolTable);
		}

		Questionnaire GetQuestionnaire()
		{
			var result = new Questionnaire()
			{
				Url = "http://example.org/fhir/Questionnaire/LinkID"
			};
			result.Item.Add(new Questionnaire.ItemComponent() { LinkId = "l1", Type = Questionnaire.QuestionnaireItemType.String });
			result.Item.Add(new Questionnaire.ItemComponent() { LinkId = "l2", Type = Questionnaire.QuestionnaireItemType.Integer });
			result.Item.Add(new Questionnaire.ItemComponent() { LinkId = "l3", Type = Questionnaire.QuestionnaireItemType.String, Repeats = true });
			result.Item.Add(new Questionnaire.ItemComponent() { LinkId = "g1", Type = Questionnaire.QuestionnaireItemType.Group });
			result.Item[3].Item.Add(new Questionnaire.ItemComponent() { LinkId = "g1.l1", Type = Questionnaire.QuestionnaireItemType.String });
			result.Item[3].Item.Add(new Questionnaire.ItemComponent() { LinkId = "g1.l2", Type = Questionnaire.QuestionnaireItemType.Integer });
			result.Item[3].Item.Add(new Questionnaire.ItemComponent() { LinkId = "g1.l3", Type = Questionnaire.QuestionnaireItemType.String, Repeats = true });
			return result;
		}

		[TestMethod]
		public void TestExpressionAtRootString()
		{
			string expression = "item.where(linkId = 'l1').answer.value";
			Console.WriteLine(expression);
			var visitor = new QuestionnaireFhirPathExpressionVisitor();
			visitor.QuestionnaireDefinition = GetQuestionnaire();
			visitor.AddInputType(typeof(QuestionnaireResponse));
			var pe = _compiler.Parse(expression);
			Console.WriteLine("---------");
			var r = pe.Accept(visitor);
			Console.WriteLine(visitor.ToString());
			Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
			Assert.AreEqual("string", r.ToString());
			Assert.IsTrue(visitor.Outcome.Success);
		}

		[TestMethod]
		public void TestExpressionAtRootInteger()
		{
			string expression = "item.where(linkId = 'l2').answer.value";
			Console.WriteLine(expression);
			var visitor = new QuestionnaireFhirPathExpressionVisitor();
			visitor.QuestionnaireDefinition = GetQuestionnaire();
			visitor.AddInputType(typeof(QuestionnaireResponse));
			var pe = _compiler.Parse(expression);
			Console.WriteLine("---------");
			var r = pe.Accept(visitor);
			Console.WriteLine(visitor.ToString());
			Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
			Assert.AreEqual("integer", r.ToString());
			Assert.IsTrue(visitor.Outcome.Success);
		}

		[TestMethod]
		public void TestExpressionInvalidLinkId()
		{
			string expression = "item.where('l20' = linkId).answer.value";
			Console.WriteLine(expression);
			var visitor = new QuestionnaireFhirPathExpressionVisitor();
			visitor.QuestionnaireDefinition = GetQuestionnaire();
			visitor.AddInputType(typeof(QuestionnaireResponse));
			var pe = _compiler.Parse(expression);
			Console.WriteLine("---------");
			var r = pe.Accept(visitor);
			Console.WriteLine(visitor.ToString());
			Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
			Assert.AreEqual("", r.ToString());
			Assert.IsFalse(visitor.Outcome.Success);
			Assert.IsTrue(visitor.Outcome.Issue[0].Details.Text.Contains("'l20'"));
		}

		[TestMethod]
		public void TestExpressionAtRootStringArray()
		{
			string expression = "item.where(linkId = 'l3').answer.value";
			Console.WriteLine(expression);
			var visitor = new QuestionnaireFhirPathExpressionVisitor();
			visitor.QuestionnaireDefinition = GetQuestionnaire();
			visitor.AddInputType(typeof(QuestionnaireResponse));
			var pe = _compiler.Parse(expression);
			Console.WriteLine("---------");
			var r = pe.Accept(visitor);
			Console.WriteLine(visitor.ToString());
			Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
			Assert.AreEqual("string[]", r.ToString());
			Assert.IsTrue(visitor.Outcome.Success);
		}

		[TestMethod]
		public void TestExpressionOnItemL1()
		{
			string expression = "answer.value";
			Console.WriteLine(expression);
			var visitor = new QuestionnaireFhirPathExpressionVisitor();
			visitor.QuestionnaireDefinition = GetQuestionnaire();
			visitor.FocusedRootItem = visitor.QuestionnaireDefinition.Item[2];
			var qProp = new FhirPathVisitorProps();
			qProp.SetAnnotation(visitor.QuestionnaireDefinition);
			qProp.Types.Add(new NodeProps(ModelInfo.ModelInspector.FindClassMapping("Quesitonnaire")));
			visitor.RegisterVariable("questionnaire", qProp);

			visitor.AddInputType(typeof(QuestionnaireResponse.ItemComponent)); // need to know WHICH item this is actually starting from
			var pe = _compiler.Parse(expression);
			Console.WriteLine("---------");
			var r = pe.Accept(visitor);
			Console.WriteLine(visitor.ToString());
			Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
			Assert.AreEqual("string[]", r.ToString());
			Assert.IsTrue(visitor.Outcome.Success);
		}

		// TODO: Another test to list of ALL the questions ... would just result in a potentially filtered set of answer types...
		[TestMethod]
		public void TestExpressionAtRootAllItemValues()
		{
			string expression = "item.answer.value";
			Console.WriteLine(expression);
			var visitor = new QuestionnaireFhirPathExpressionVisitor();
			visitor.QuestionnaireDefinition = GetQuestionnaire();
			var qProp = new FhirPathVisitorProps();
			qProp.SetAnnotation(visitor.QuestionnaireDefinition);
			qProp.Types.Add(new NodeProps(ModelInfo.ModelInspector.FindClassMapping("Quesitonnaire")));
			visitor.RegisterVariable("questionnaire", qProp);

			visitor.AddInputType(typeof(QuestionnaireResponse));
			var pe = _compiler.Parse(expression);
			Console.WriteLine("---------");
			var r = pe.Accept(visitor);
			Console.WriteLine(visitor.ToString());
			Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
			Assert.AreEqual("string, integer, string[]", r.ToString());
			Assert.IsTrue(visitor.Outcome.Success);
		}

		[TestMethod]
		public void TestExpressionOnGroupItemInteger()
		{
			string expression = "item.item.where(linkId = 'g1.l2' or linkId='2').answer.value";
			Console.WriteLine(expression);
			var visitor = new QuestionnaireFhirPathExpressionVisitor();
			visitor.QuestionnaireDefinition = GetQuestionnaire();
			visitor.AddInputType(typeof(QuestionnaireResponse));
			var pe = _compiler.Parse(expression);
			Console.WriteLine("---------");
			var r = pe.Accept(visitor);
			Console.WriteLine(visitor.ToString());
			Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
			Assert.AreEqual("integer", r.ToString());
			Assert.IsTrue(visitor.Outcome.Success);
		}

		[TestMethod]
		public void TestExpressionUsingDescendants()
		{
			string expression = "descendants().item.where(linkId = 'g1.l2' or klink='2' or linkId in ('' | 'b' | 'c')).answer.value";
			Console.WriteLine(expression);
			var visitor = new QuestionnaireFhirPathExpressionVisitor();
			visitor.QuestionnaireDefinition = GetQuestionnaire();
			visitor.AddInputType(typeof(QuestionnaireResponse));
			var pe = _compiler.Parse(expression);
			Console.WriteLine("---------");
			var r = pe.Accept(visitor);
			Console.WriteLine(visitor.ToString());
			Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
			Assert.AreEqual("integer", r.ToString());
			Assert.IsTrue(visitor.Outcome.Success);
		}

		[TestMethod]
		public void TestExpressionUsingRepeat()
		{
			string expression = "repeat(item).where(linkId = 'g1.l2' or klink='2' or linkId in ('' | 'b' | 'c')).answer.value";
			Console.WriteLine(expression);
			var visitor = new QuestionnaireFhirPathExpressionVisitor();
			visitor.QuestionnaireDefinition = GetQuestionnaire();
			visitor.AddInputType(typeof(QuestionnaireResponse));
			var pe = _compiler.Parse(expression);
			Console.WriteLine("---------");
			var r = pe.Accept(visitor);
			Console.WriteLine(visitor.ToString());
			Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
			Assert.AreEqual("integer", r.ToString());
			Assert.IsTrue(visitor.Outcome.Success);
		}
	}
}