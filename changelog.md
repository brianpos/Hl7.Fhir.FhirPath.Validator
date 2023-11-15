| FhirPath Static Analysis |
|---|

## Change log ##

### November 15 2023: 5.3.0-beta4
* `round` function now handles integer as a valid context to run (and returns decimal)
* Error message when reporting issue on `ToString` on collections demoted to a warning, consistent with the other occurrences of this - even though it is actually an error
