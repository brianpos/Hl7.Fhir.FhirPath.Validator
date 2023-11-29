using Hl7.Fhir.Introspection;
using Hl7.Fhir.Model;
using Hl7.Fhir.Specification.Source;
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
    public class FhirPathExpressionVisitor : ExtensionResolvingFhirPathExpressionVisitor
    {
        public FhirPathExpressionVisitor()
            : base(new CachedResolver(
				new MultiResolver(
					ZipSource.CreateValidationSource()
				)
			), ModelInspector.ForAssembly(typeof(Patient).Assembly),
                  Hl7.Fhir.Model.ModelInfo.SupportedResources,
                  Hl7.Fhir.Model.ModelInfo.OpenTypes)
        {
        }

		public FhirPathExpressionVisitor(IResourceResolver source)
			: base(source, ModelInspector.ForAssembly(typeof(Patient).Assembly),
                  Hl7.Fhir.Model.ModelInfo.SupportedResources,
                  Hl7.Fhir.Model.ModelInfo.OpenTypes)
        {
        }
    }
}
