using Hl7.Fhir.Introspection;
using Hl7.Fhir.Model;
using System;
using System.Collections.Generic;

namespace Hl7.Fhir.FhirPath.Validator
{
    internal class FunctionArgument
    {
        bool _isOptional;
        public FunctionArgument(string name, NodeProps type, bool isOptional = false)
        {
            Name = name;
            Type = type;
            _isOptional = isOptional;
        }

        public FunctionArgument(ModelInspector mi, string name, Type type, bool isOptional = false)
        {
            Name = name;
            Type = new NodeProps(mi.FindClassMapping(type));
            _isOptional = isOptional;
        }

        public string Name { get; }
        public NodeProps Type { get; }
    }

    internal class FunctionDefinition
    {
        public FunctionDefinition(string name, bool supportsCollections = false, bool supportedAtRoot = false)
        {
            Name = name;
            SupportsCollections = supportsCollections;
            SupportedAtRoot = supportedAtRoot;

		}
        public string Name { get; }
        public bool SupportsCollections { get; }
        public bool SupportedAtRoot { get; }
        public List<FunctionContext> SupportedContexts { get; } = new();
        public Func<FunctionDefinition, IEnumerable<FhirPathVisitorProps>, OperationOutcome, List<NodeProps>> GetReturnType { get; set; }
        public List<Action<FunctionDefinition, IEnumerable<FhirPathVisitorProps>, OperationOutcome>> Validations { get; set; } = new List<Action<FunctionDefinition, IEnumerable<FhirPathVisitorProps>, OperationOutcome>>();
    }

    [System.Diagnostics.DebuggerDisplay(@"Context: {Type}, Returns: {ReturnType.Name}")] // http://blogs.msdn.com/b/jaredpar/archive/2011/03/18/debuggerdisplay-attribute-best-practices.aspx
    internal class FunctionContext
    {
        public FunctionContext(string type, ClassMapping returnType)
        {
            Type = type;
            ReturnType = returnType;
        }
        public string Type { get; set; }
        public ClassMapping ReturnType { get; set; }
    }
}
