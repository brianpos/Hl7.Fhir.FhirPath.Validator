| FhirPath Static Analysis |
|---|

## Change log ##

### 25 July 2025:
* Update the scope of processing variables to be $this rather than the functions input collection.

### February 11 2025
* Update FirelySDK dependency to 5.11.3

### December 18 2024: 5.11.1-rc4
* Tracks all the properties that an expression uses. (each time it calls the child property, add it to the list)
* Resolved issue with paths visited reported by the visitor for complex extensions

### December 18 2024: 5.11.1-rc3
* Update FirelySDK dependency to 5.11.1
* Include net90 as a target framework

### May 6 2024: 5.8.1-rc2
* Resolve minor issues with defineVariable validation checks

### May 6 2024: 5.8.1-rc1
* Update FirelySDK dependency to 5.8.1 (to include the defineVariable function implementation)
* Update to compatibility with Net80 (and remove the net6 and net7 targets)
* Refactor location in issue setting to a central function (in preparation for location position parsing)

### December 15 2023: 5.3.0-rc1
* Correct the error message shown when incorrectly using `ofType` to which shows `as`
* If the `resolve()` function is performed on a property that resolves to just Resource,
  this really means that can be any type of resource supported by that version of FHIR

### December 15 2023: 5.3.0-beta9
* Revert the inclusion `string` as one of the valid datatypes for the Search Type `Uri`

### December 14 2023: 5.3.0-beta8
* Correct return type of `as()` to boolean
* Add validation check to the `as()` function to check that the type provided could potentially be valid
* Include `string` as one of the valid datatypes for the Search Type `Uri`

### November 30 2023: 5.3.0-beta7
* Support derivations on the BaseFhirPathExpressionvisitor
* Support adding annotations to the FhirPathVisitorProps
* Support for validating extension URLs via a ISourceResolver via a new class `ExtensionResolvingFhirPathExpressionVisitor`
* Support for validating complex extension invariants with their internal "properties"
* updated unit tests to set the Extension resolving
* additional null reference check for search parameter checks encountered when trying to resolve a composite in a composite
* search parameter validator able to swap the visitor (to be able to use derived visitors)

### November 27 2023: 5.3.0-beta6
* update support for `answers()` to properly check the context that its valid on from the SDC specification
* include support for `ordinal()` from the SDC specification
* when no type is detected while processing output a `???` instead of a blank in the debug logs/error messages<br/>
   e.g. error display: `prop 'value' not found on ???` and a log entry: `.value : ???`
* When a property is encountered that doesn't exist in the given context and that property name exists as a variable, 
   provide a message in the error that indicates that a variable of that name exists (help common typo issue)
* Added SDC functions sum, min, max, avg
* Added support for positiveInt/unsignedInt/base64Binary/date/instant type conversions for context processing to fhir primitive types
* Refined debug output to indent content more consistently
* Implementation of aggregate and $total

### November 20 2023: 5.3.0-beta5
* Validate the `iif` function to be valid across any context (but not a collection)

### November 15 2023: 5.3.0-beta4
* `round` function now handles integer as a valid context to run (and returns decimal)
* Error message when reporting issue on `ToString` on collections demoted to a warning, consistent with the other occurrences of this - even though it is actually an error
* `Iif` statement validation added
* Improve error reporting for `length` - only on strings, otherwise errors (and a warning on string collections)
