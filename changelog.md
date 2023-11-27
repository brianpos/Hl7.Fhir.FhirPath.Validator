| FhirPath Static Analysis |
|---|

## Change log ##

### November 24 2023: 5.3.0-beta6
* update support for `answers()` to properly check the context that its valid on from the SDC specification
* include support for `ordinal()` from the SDC specification
* when no type is detected while processing output a `???` instead of a blank in the debug logs/error messages<br/>
   e.g. error display: `prop 'value' not found on ???` and a log entry: `.value : ???`
* When a property is encountered that doesn't exist in the given context and that property name exists as a variable, 
   provide a message in the error that indicates that a variable of that name exists (help common typo issue)
* Added SDC functions sum, min, max, avg
* Added support for positiveInt/unsignedInt/base64Binary/date/instant type conversions for context processing to fhir primitive types
* Refined debug output to indent content more consistently

### November 20 2023: 5.3.0-beta5
* Validate the `iif` function to be valid across any context (but not a collection)

### November 15 2023: 5.3.0-beta4
* `round` function now handles integer as a valid context to run (and returns decimal)
* Error message when reporting issue on `ToString` on collections demoted to a warning, consistent with the other occurrences of this - even though it is actually an error
* `Iif` statement validation added
* Improve error reporting for `length` - only on strings, otherwise errors (and a warning on string collections)
