The WithValidation extension method allow you to manually document validation rules.

> IMPORTANT: This does not perform any validation, just documents the rules.

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

There are several built in extension methods, that all maps to the open api spec except for the extension method `.Custom()`
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
Allows you to alter an existing rule. This may be useful if you want to change the default validation rules documented via Data Annotations or Fluent Validation

```csharp
.WithValidationRules<RequestModel>(x =>
{
	// Demonstrate Alter: Change the Fluent Validation pattern error message
	x.Property(p => p.Test).Alter("Must match pattern: ^[a-zA-Z0-9]+$", "Must be alphanumeric");
});
```

## Remove

```csharp
.WithValidationRules<RequestModel>(x =>
{
	// Demonstrate Remove: Remove the MaxLength rule from Fluent Validation
	x.Property(p => p.Test).Remove("Must be 100 characters or fewer");
});
```

## RemoveAll

```csharp
.WithValidationRules<RequestModel>(x =>
{
	// Demonstrate RemoveAll: Remove all Fluent Validation rules for this property
	x.Property(p => p.Test).RemoveAll();
});
```

## AppendRulesToPropertyDescription
Using this property you can control whether rules get listed in the property description or not. Rules will still be documented in the open api spec if it is supported but it would be appended to the property description.
```csharp
.WithValidationRules<RequestModel>(x =>
{
	x.AppendRulesToPropertyDescription(false); // Disable rule appending for all properties in RequestModel
	x.Property(p => p.Test).AppendRulesToPropertyDescription(false); // Disable rule appending for specific property within RequestModel
});
```