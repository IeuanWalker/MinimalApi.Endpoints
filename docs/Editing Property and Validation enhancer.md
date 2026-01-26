# Property and validation enhancer
This improves the generated OpenAPI spec by fixing types, enriching enums, and documenting validation rules.

# Usage
```csharp
builder.Services.AddOpenApi(options =>
{
	options.EnhancePropertiesAndValidation(
		autoDocumentFluentValidation: true,         // Default: true
		autoDocumentDataAnnotationValidation: true, // Default: true
		appendRulesToPropertyDescription: true      // Default: true (global switch)
	);
});
```

> NOTE: This only affects OpenAPI documentation. It does not perform runtime validation.

__________ 

# Features
## Correct type handling
Automatically inlines supported types (int, double, long, string, arrays, lists, dictionaries, IFormFile, IFormFileCollection, etc) and fixes nullability.
```diff
 "intValue": {
-  "$ref": "#/components/schemas/System.Int32"
+  "type": "integer",
+  "format": "int32"
 }
```

## Full enum documentation
Enriches enum schemas with values, names, and descriptions (also covers FluentValidation enum rules like `IsEnumName` / `IsInEnum`).
<img width="478" height="693" alt="image" src="https://github.com/user-attachments/assets/b06d8578-ff8b-495c-9df5-62e0316ba8b0" />

## Automatic validation documentation
Documents rules discovered from DataAnnotations and FluentValidation, and applies them to request bodies and parameters.
<img width="518" height="440" alt="image" src="https://github.com/user-attachments/assets/e6863f58-f7d1-454a-bdff-a1d573335def" />

## Manual rules via `WithValidationRules`
You can override or add rules per endpoint using `WithValidationRules`. This is useful when you disable auto-documentation or have custom validation logic.
See: https://github.com/IeuanWalker/MinimalApi.Endpoints/wiki/WithValidationRules
