using Hl7.Fhir.Introspection;
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
}
