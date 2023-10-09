| FhirPath Static Analysis |
|---|

## Introduction ##

Many [FHIR][fhir-spec] resources contain [FhirPath][fhirpath-spec] expressions as string values that are to 
be use in a specific context, such as SearchParameters, StructureDefinitions, and Quesitonnaires.

The Firely SDK provides a FHIRPath engine for evaluating these expressions at runtime along with a parser/compiler.

This project provides a static analysis tool that can help ensure that a valid fhirpath expression (returned
by the Firely parser) is valid for the context in which it is to be used.

For example it could check that a specific custom search parameter was valid against the Patient resource.

The library contains:

* A visitor of the Firely Expression class returned by the FhirPath parser that can be used to verify the validity of the FhirPath expression.
* Unit test verifying all the R4B/R5 search expressions provided by the Firely SDK
* Unit test verifying all the R4B/R5 invariant expressions provided by the Hl7 SDK

Coming Soon:

* Unit test verifying all expressions in a FHIR npm package (this is now possible using the [UploadFIG](https://github.com/brianpos/uploadfig) dotnet tool)
* Support to validate expressions in FHIR Questionnaires (R4B only to start)

Known Issues/incomplete funcitonality:
* missing functions: intersect, exclude, single, iif
* type conversions http://hl7.org/fhirpath/N1/#conversion
	* toBoolean
	* toInteger
	* toDate
	* toDateTime
	* toDecimal
	* toQuantity
	* toTime
	* indexOf
* length() doesn't check that context is a string
* toChars() returns a string not string[]
* Math functions
	* abs
	* ceiling
	* exp
	* floor
	* ln
	* log
	* power
	* round
	* sqrt
	* truncate
* Utility functions
	* now()
	* timeOfDay()
	* today()
* Comparisons don't check for type conversions, or that the types are compatible/same
	* though does identify that the resulting type is boolean for downstream processing
* Boolean logic operators should check that both sides are boolean type parameters
* Math operators
* Reflection
* Checking types of parameters to functions (not just return types and object mdel prop names)

> **Note:** Only reviewed up to section 6 in the specification


The library depends on several NuGet packages (notably):

* `Hl7.Fhir.Conformance` - contains the FhirPath Engine, Introspection, and base models
* *The version specific assemblies also leverage the `Hl7.Fhir.*` packages*
	* [R4][r4-spec], [R4B][r4b-spec], [R5][r5-spec]

## Getting Started ##

TODO: But best place to start is to look at the unit tests.

To date they are covering verifying the R4B and R5 Search Parameters.

## Support ##
None officially.
For questions and broader discussions, we use the .NET FHIR Implementers chat on [Zulip][netapi-zulip].

## Contributing ##

I am welcoming any contributors!

If you want to participate in this project, we're using [Git Flow][nvie] for our branch management, so please submit your commits using pull requests no on the develop branches mentioned above!

### GIT branching strategy ###

- [NVIE](http://nvie.com/posts/a-successful-git-branching-model/)
- Or see: [Git workflow](https://www.atlassian.com/git/workflows#!workflow-gitflow)

[netapi-zulip]: https://chat.fhir.org/#narrow/stream/dotnet
[fhir-spec]: http://www.hl7.org/fhir
[r4-spec]: http://www.hl7.org/fhir/r4
[r4b-spec]: http://www.hl7.org/fhir/r4b
[r5-spec]: http://www.hl7.org/fhir/r5
[fhirpath-spec]: http://hl7.org/fhirpath/

### History ###
This project was created to help verify the validity of the fhirpath expressions
throughout the core HL7 specifications, however once working discovered that this
could also be relevant for others to perform the same style of checks in running systems,
such as servers wanting to check their own fhirpath expressions.
