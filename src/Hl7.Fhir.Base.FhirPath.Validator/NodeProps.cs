﻿using Hl7.Fhir.Introspection;
using Hl7.Fhir.Rest;
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
    public struct NodeProps
    {
        public NodeProps(ClassMapping classMapping, PropertyMapping propMap = null)
        {
            ClassMapping = classMapping;
            IsCollection = propMap?.IsCollection == true;
            PropertyMapping = propMap;
        }

        public ClassMapping ClassMapping { get; private set; }
        public PropertyMapping PropertyMapping { get; private set; }
        public bool IsCollection { get; set; }

        public NodeProps AsCollection()
        {
            // The property mapping isn't brought forward
            return new NodeProps(ClassMapping) { IsCollection = true };
        }
    }
}
