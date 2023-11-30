using Hl7.Fhir.Model;
using System;
using System.Diagnostics;

namespace Hl7.Fhir.FhirPath.Validator
{
	/// <summary>
	/// Version Agnostic implementation of the SearchParameter class from the various versions of FHIR (although isn't FIHR and hasn't changed)
	/// </summary>
	[System.Diagnostics.DebuggerDisplay(@"\{{DebuggerDisplay,nq}}")] // http://blogs.msdn.com/b/jaredpar/archive/2011/03/18/debuggerdisplay-attribute-best-practices.aspx
    public class VersionAgnosticSearchParameter
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string DebuggerDisplay
        {
            get
            {
                return String.Format("{0} {1} {2} ({3})", Resource, Name, Type, Expression);
            }
        }

        public string Resource { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public string Url { get; set; }
        public SearchParamType Type { get; set; }

        /// <summary>
        /// The FHIR Path expresssion that can be used to extract the data
        /// for this search parameter
        /// </summary>
        public string Expression { get; set; }

        /// <summary>
        /// If this is a reference, the possible types of resources that the
        /// parameters references to
        /// </summary>
        public string[] Target { get; set; }

        /// <summary>
        /// Used to define the parts of a composite search parameter.
        /// </summary>
        public SearchParamComponent[] Component { get; set; }
    }
}
