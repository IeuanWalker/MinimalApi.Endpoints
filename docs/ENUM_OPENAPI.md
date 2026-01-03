# Enum Schema Enrichment in OpenAPI

## Overview

The Enum Schema Enrichment feature automatically enhances OpenAPI documentation for enum types by adding comprehensive metadata about enum values, member names, and descriptions. This provides better API documentation and improves the developer experience when working with your API.

## The Problem

By default, ASP.NET Core's OpenAPI generator represents enums as simple integers with minimal information:

```json
{
  "TodoPriority": {
    "type": "integer"
  }
}
```

This bare representation doesn't tell developers:
- What values are valid (0, 1, 2, 3)
- What each value means (Low, Medium, High, Critical)
- Any contextual information about when to use each value

## The Solution

The `EnumSchemaTransformer` automatically enriches enum schemas with:

1. **Valid Values** - The `enum` array lists all valid numeric values
2. **Member Names** - The `x-enum-varnames` extension maps values to their C# names
3. **Descriptions** (optional) - The `x-enum-descriptions` extension provides detailed descriptions when using `[Description]` attributes
4. **Human-Readable Summary** - The `description` field provides a quick overview of available values

### Example: Enriched Enum Schema

**C# Enum Definition:**
```csharp
using System.ComponentModel;

public enum TodoPriority
{
    [Description("Low priority task")]
    Low = 0,
    [Description("Medium priority task")]
    Medium = 1,
    [Description("High priority task")]
    High = 2,
    [Description("Critical priority task requiring immediate attention")]
    Critical = 3
}
```

**Generated OpenAPI Schema:**
```json
{
  "TodoPriority": {
    "type": "integer",
    "description": "Enum: Low, Medium, High, Critical",
    "enum": [0, 1, 2, 3],
    "x-enum-varnames": ["Low", "Medium", "High", "Critical"],
    "x-enum-descriptions": {
      "Low": "Low priority task",
      "Medium": "Medium priority task",
      "High": "High priority task",
      "Critical": "Critical priority task requiring immediate attention"
    }
  }
}
```

## Setup

### 1. Enable Enum Schema Enrichment

The enum schema enrichment is automatically enabled when you add FluentValidation schemas to your OpenAPI configuration:

```csharp
builder.Services.AddOpenApi(options =>
{
    options.AddFluentValidationSchemas(); // This includes enum enrichment
});
```

**Note:** The `AddFluentValidationSchemas()` method automatically includes the `EnumSchemaTransformer`, so you don't need a separate call to enable enum enrichment.
```

### 2. (Optional) Add Descriptions to Enum Members

For more detailed documentation, decorate enum members with the `[Description]` attribute:

```csharp
using System.ComponentModel;

public enum OrderStatus
{
    [Description("Order has been placed but not yet processed")]
    Pending,
    
    [Description("Order is being prepared for shipment")]
    Processing,
    
    [Description("Order has been shipped to the customer")]
    Shipped,
    
    [Description("Order has been delivered successfully")]
    Delivered,
    
    [Description("Order was cancelled by the customer or system")]
    Cancelled
}
```

## Benefits

### For API Documentation

- **API explorers** (Swagger UI, Scalar, Redoc) can display enum values in dropdowns and selection lists
- **Code generators** can create properly typed enums in client SDKs
- **Validation tools** can verify requests contain valid enum values

### For Developers

- **Autocomplete support** - Better IDE integration when consuming the API
- **Type safety** - Client libraries can generate strongly-typed enums
- **Self-documenting** - No need to search code or documentation for valid values
- **Localization-friendly** - Descriptions can be localized while maintaining value stability

## OpenAPI Extensions

The transformer uses these OpenAPI extension properties:

### `enum` (Standard OpenAPI)
The standard OpenAPI `enum` keyword containing all valid numeric values.

**Example:**
```json
"enum": [0, 1, 2, 3]
```

### `x-enum-varnames` (Community Extension)
A widely-supported extension that maps each enum value to its C# member name. Supported by many code generators and API tools.

**Example:**
```json
"x-enum-varnames": ["Low", "Medium", "High", "Critical"]
```

### `x-enum-descriptions` (Custom Extension)
A custom extension that provides detailed descriptions for each enum member. Generated only when `[Description]` attributes are present.

**Example:**
```json
"x-enum-descriptions": {
  "Low": "Low priority task",
  "Medium": "Medium priority task"
}
```

## Advanced Usage

### Multiple OpenAPI Documents

If you have multiple OpenAPI documents (e.g., for different API versions), the enum enrichment is automatically included with FluentValidation schemas:

```csharp
builder.Services.AddOpenApi("v1", options =>
{
    options.AddFluentValidationSchemas(); // Includes enum enrichment
});

builder.Services.AddOpenApi("v2", options =>
{
    options.AddFluentValidationSchemas(); // Includes enum enrichment
});
```

### Combining with Other Transformers

Enum enrichment is automatically included with FluentValidation and works alongside other transformers:

```csharp
builder.Services.AddOpenApi(options =>
{
    options.AddFluentValidationSchemas();    // Includes enum enrichment + FluentValidation constraints
    options.AddAuthenticationSchemes();      // Add security schemes
    options.AddAuthorizationPoliciesAndRequirements(); // Add authorization policies
});
```

## Supported Enum Types

The transformer supports all .NET enum types with any underlying integer type:

- `byte`, `sbyte`
- `short`, `ushort`
- `int`, `uint`
- `long`, `ulong`

**Example with different underlying type:**
```csharp
public enum StatusCode : byte
{
    Success = 0,
    Warning = 1,
    Error = 2
}
```

## Examples

### Simple Enum (No Descriptions)

```csharp
public enum DayOfWeek
{
    Sunday,
    Monday,
    Tuesday,
    Wednesday,
    Thursday,
    Friday,
    Saturday
}
```

**Generated Schema:**
```json
{
  "DayOfWeek": {
    "type": "integer",
    "description": "Enum: Sunday, Monday, Tuesday, Wednesday, Thursday, Friday, Saturday",
    "enum": [0, 1, 2, 3, 4, 5, 6],
    "x-enum-varnames": ["Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"]
  }
}
```

### Enum with Custom Values

```csharp
public enum HttpStatusCode
{
    OK = 200,
    Created = 201,
    BadRequest = 400,
    Unauthorized = 401,
    NotFound = 404,
    InternalServerError = 500
}
```

**Generated Schema:**
```json
{
  "HttpStatusCode": {
    "type": "integer",
    "description": "Enum: OK, Created, BadRequest, Unauthorized, NotFound, InternalServerError",
    "enum": [200, 201, 400, 401, 404, 500],
    "x-enum-varnames": ["OK", "Created", "BadRequest", "Unauthorized", "NotFound", "InternalServerError"]
  }
}
```

## Integration with Scalar

When using [Scalar](https://scalar.com/) for API documentation, enum values are automatically displayed in interactive dropdowns and request examples:

```csharp
builder.Services.AddOpenApi(options =>
{
    options.AddEnumSchemas();
});

app.MapScalarApiReference();
```

Scalar will render:
- Dropdown lists for enum parameters
- Inline documentation for each enum value
- Type-ahead search for enum values

## Best Practices

### ✅ DO

- Use descriptive enum member names (e.g., `InProgress` instead of `Status2`)
- Add `[Description]` attributes for complex enums or when context is needed
- Keep enum values stable across API versions
- Document breaking changes when adding/removing enum values

### ❌ DON'T

- Don't rely solely on numeric values in documentation
- Don't change enum numeric values in production APIs without versioning
- Don't use generic names like `Value1`, `Value2`
- Don't forget to update descriptions when enum behavior changes

## Version Compatibility

- **ASP.NET Core:** 10.0+
- **Microsoft.AspNetCore.OpenApi:** 10.0+
- **Microsoft.OpenApi:** 2.0.0+
- **OpenAPI Specification:** 3.0+

## Troubleshooting

### Enum values not appearing in OpenAPI spec

**Cause:** Transformer not registered or enum type not detected

**Solution:** 
1. Ensure `options.AddEnumSchemas()` is called in OpenAPI configuration
2. Verify enum types are referenced in request/response models
3. Check that enums are in the same assembly or referenced assemblies

### Descriptions not showing up

**Cause:** Missing `[Description]` attribute import or incorrect placement

**Solution:**
```csharp
using System.ComponentModel; // Required namespace

public enum MyEnum
{
    [Description("This is correct")]  // ✅ Attribute on member
    Value1
}
```

## Related Features

- **[FluentValidation Schema Enrichment](FLUENTVALIDATION_OPENAPI.md)** - Adds validation constraints to OpenAPI schemas
- **[Authentication Schemes](../README.md#authentication-schemes)** - Documents security requirements
- **[Authorization Policies](../README.md#authorization-policies)** - Documents endpoint authorization

## References

- [OpenAPI 3.1 Specification](https://spec.openapis.org/oas/v3.1.0)
- [JSON Schema Validation](https://json-schema.org/draft/2020-12/json-schema-validation.html)
- [System.ComponentModel.DescriptionAttribute](https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.descriptionattribute)
