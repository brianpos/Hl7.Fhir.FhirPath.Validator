using Hl7.Fhir.Introspection;
using Hl7.Fhir.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;

namespace Hl7.Fhir.FhirPath.Validator
{
    public class FhirPathVisitorProps : IAnnotatable, IAnnotated
    {
        public bool isRoot;
        public readonly Collection<NodeProps> Types = new();

        public void AddType(ModelInspector mi, Type type, bool forceCollection = false, string path = null)
        {
            var cm = mi.FindOrImportClassMapping(type);
            if (cm != null)
            {
                var np = new NodeProps(cm, path: null);
                if (forceCollection)
                    np = np.AsCollection();
                Types.Add(np);
            }
        }

        public string TypeNames()
        {
            string result = ToString();
			if (string.IsNullOrEmpty(result))
				return "???";
			return result;
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

        public string BaseType(string typeName)
        {
            return typeName switch
            {
                "System.Boolean" => "boolean",
                "System.String" => "string",
                "code" => "string",
                "markdown" => "string",
                "id" => "string",
                "uri" => "string",
                "url" => "string",
                "canonical" => "string",
                "uuid" => "string",
                "oid" => "string",
                "base64Binary" => "string",
				"positiveInt" => "integer",
                "unsignedInt" => "integer",
                "date" => "dateTime",
                "instant" => "dateTime",

				_ => typeName
            };
        }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="typeName"></param>
		/// <param name="SingleOnly">If provided, the type must exactly match</param>
		/// <returns></returns>
		public bool CanBeOfType(string typeName, bool SingleOnly = false)
        {
            if (Types.Any(v => v.ClassMapping.Name == typeName && (!SingleOnly || !v.IsCollection)))
                return true;
            // Also check if the base type of this is included
            if (Types.Any(v => BaseType(v.ClassMapping.Name) == BaseType(typeName) && (!SingleOnly || !v.IsCollection)))
                return true;
            return false;
        }

        public IEnumerable<string> CanBeOfTypes(string typeName)
        {
            List<string> result = new List<string>();
            result.AddRange(Types.Where(v => v.ClassMapping.Name == typeName || BaseType(v.ClassMapping.Name) == typeName).Select(v => v.ClassMapping.Name));
            return result;
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
                result.Types.Add(new NodeProps(t.ClassMapping, t.PropertyMapping, path: t.Path) { IsCollection = false });
            }
            return result;
        }

		#region << Annotations >>
		[NonSerialized]
		private AnnotationList _annotations = null;

		private AnnotationList annotations => LazyInitializer.EnsureInitialized(ref _annotations, () => new());

		public IEnumerable<object> Annotations(Type type) => annotations.OfType(type);

		public void AddAnnotation(object annotation) => annotations.AddAnnotation(annotation);

		public void RemoveAnnotations(Type type) => annotations.RemoveAnnotations(type);
		#endregion
	}
}
