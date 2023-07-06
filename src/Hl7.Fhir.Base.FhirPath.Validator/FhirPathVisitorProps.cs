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

        public void AddType(ModelInspector mi, Type type, bool forceCollection = false)
        {
            var cm = mi.FindOrImportClassMapping(type);
            if (cm != null)
            {
                var np = new NodeProps(cm);
                if (forceCollection)
                    np = np.AsCollection();
                Types.Add(np);
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

        public bool IsCollection()
        {
            if (Types.Any(v => v.IsCollection == true))
                return true;
            return false;
        }

        internal FhirPathVisitorProps AsSingle()
        {
            FhirPathVisitorProps result = new();
            foreach (var t in this.Types)
            {
                result.Types.Add(new NodeProps(t.ClassMapping, t.PropertyMapping) { IsCollection = false });
            }
            return result;
        }
    }
}
