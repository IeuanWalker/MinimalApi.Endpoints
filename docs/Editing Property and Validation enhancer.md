This impoves a number of aspects of the generated open api spec 
* Proper type handling
* Automatic documentation of all validation rules
* Full enum documentation
* Proper nullability documentation 

# Usage
```csharp
builder.Services.AddOpenApi(options =>
{
	config.EnhancePropertiesAndValidation(
		autoDocumentFluentValidation: true,         // Default: true
		autoDocumentDataAnnotationValidation: true, // Default: true
		appendRulesToPropertyDescription: true      // Default: true - This is a global setting, you can also control this on a per request or per property basis - https://github.com/IeuanWalker/MinimalApi.Endpoints/wiki/WithValidationRules#appendrulestopropertydescription
	);
});
```

__________ 

# Features
## In-lines supported types
Automatically in-lines supported types _(int, double, long, string, array, list, IFormFiles, IFormFileCollections, etc)_
```diff
 "intValue": {
-  "$ref": "#/components/schemas/System.Int32"
+  "type": "integer",
+  "format": "int32"
 }

-"System.Int32": {
-  "pattern": "^-?(?:0|[1-9]\\d*)$",
-  "format": "int32"
-}
```
## Full enum documentation
Improves Enum documentation whether its a direct enum or string/ ints that use Fluent Validation enum validation - 
<img width="478" height="693" alt="image" src="https://github.com/user-attachments/assets/b06d8578-ff8b-495c-9df5-62e0316ba8b0" />

## Auto document DataAnnotations and FluentValidation
<img width="518" height="440" alt="image" src="https://github.com/user-attachments/assets/e6863f58-f7d1-454a-bdff-a1d573335def" />

## Ability to manually document rules, via `WithValidationRules`
If you don't want to auto document validation rules using DataAnnotation/ FluentValidation or you have additional validation yours that are checked as part of the endpoint handling, then you can manually documents these using the WithValidationRulesextension method. Check out its related doc - https://github.com/IeuanWalker/MinimalApi.Endpoints/wiki/WithValidationRules


