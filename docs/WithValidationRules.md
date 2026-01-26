# WithValidationRules
The `WithValidationRules` extension lets you manually document validation rules for OpenAPI.

> IMPORTANT: This does not perform any validation, it only updates the OpenAPI spec.

## Usage
```csharp
public static void Configure(RouteHandlerBuilder builder)
{
	builder
		.Post("/WithValidation")
		.RequestFromBody()
		.WithValidationRules<RequestModel>(config =>
		{
			// All rules
			config.Property(x => x.AllRules)
				.Description("Custom description")
				.Required()
				.MinLength(10)
				.MaxLength(100)
				.Length(10, 100)
				.Pattern(@"^[a-zA-Z0-9]+$")
				.Email()
				.Url()
				.Custom("Custom rule")
				.GreaterThan(10)
				.GreaterThanOrEqual(11)
				.LessThan(100)
				.LessThanOrEqual(100)
				.Between(10, 100);

			// Nested object validation
			config.Property(x => x.NestedObject).Required();

			// Nested object property validation
			config.Property(x => x.NestedObject.StringMin).MinLength(5).Description("Nested string minimum length");
			config.Property(x => x.NestedObject.StringMax).MaxLength(100);
			config.Property(x => x.NestedObject.IntMin).GreaterThanOrEqual(10);
			config.Property(x => x.NestedObject.DoubleMax).LessThanOrEqual(1000.0);

			// Array element validation - applies validation to items in the array
			config.Property(x => x.ListNestedObject![0].StringPattern).Pattern(@"^[A-Z]+$");
			config.Property(x => x.ListNestedObject![0].IntMax).LessThan(500);
		});
}
```

## Behavior
- Rules are merged with auto-discovered rules (FluentValidation and DataAnnotations).
- Manual rules replace any auto-discovered rules for the same property.
- Use `Alter`, `Remove`, or `RemoveAll` to adjust auto-discovered rules instead of replacing them.

## Available rule methods
All methods map to OpenAPI schema constraints except `Custom()`, which only adds documentation.

| Extension | Details |
| --- | --- |
| `Description(string description)` | Adds a description to the property |
| `Custom(string errorMessage)` | Custom rule |
| `Required(string? errorMessage = null)` | Required rule |
| `Length(int min, int max, string? errorMessage = null)` | String length rule |
| `MinLength(int min, string? errorMessage = null)` | String length rule |
| `MaxLength(int max, string? errorMessage = null)` | String length rule |
| `Pattern(string regex, string? errorMessage = null)` | Regex rule |
| `Email(string? errorMessage = null)` | Email rule |
| `Url(string? errorMessage = null)` | Url rule |
| `Between<TValue>(TValue min, TValue max, string? errorMessage = null)` | Number rule |
| `GreaterThan<TValue>(TValue value, string? errorMessage = null)` | Number rule |
| `GreaterThanOrEqual<TValue>(TValue value, string? errorMessage = null)` | Number rule |
| `LessThan<TValue>(TValue value, string? errorMessage = null)` | Number rule |
| `LessThanOrEqual<TValue>(TValue value, string? errorMessage = null)` | Number rule |

## Alter
Change the error message for an existing rule (typically from auto-discovered rules).
```csharp
.WithValidationRules<RequestModel>(x =>
{
	x.Property(p => p.Test).Alter("Must match pattern: ^[a-zA-Z0-9]+$", "Must be alphanumeric");
});
```

## Remove
Remove a specific rule by its error message.
```csharp
.WithValidationRules<RequestModel>(x =>
{
	x.Property(p => p.Test).Remove("Must be 100 characters or fewer");
});
```

## RemoveAll
Remove all rules for a property.
```csharp
.WithValidationRules<RequestModel>(x =>
{
	x.Property(p => p.Test).RemoveAll();
});
```

## AppendRulesToPropertyDescription
Control whether rules are appended to property descriptions.
```csharp
.WithValidationRules<RequestModel>(x =>
{
	x.AppendRulesToPropertyDescription(false); // Disable rule appending for all properties in RequestModel
	x.Property(p => p.Test).AppendRulesToPropertyDescription(false); // Disable for a specific property
});
```
