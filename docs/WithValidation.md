# WithValidation - OpenAPI Validation Documentation

## Overview

The `WithValidation` extension method provides a fluent API for documenting validation rules in your OpenAPI specification. It supports both **manual validation rule definition** and **automatic FluentValidation rule extraction**.

**Key Features:**
- 🎯 **OpenAPI Documentation Only** - No runtime validation (use FluentValidation or DataAnnotations for that)
- 📝 **Human-Readable Descriptions** - Validation rules appear in property descriptions
- 🔄 **FluentValidation Auto-Discovery** - Automatically extracts and documents existing FluentValidation rules
- ✏️ **Manual Override** - Use `WithValidation()` to override or supplement auto-discovered rules
- ⚡ **Zero Configuration** - Works immediately without setup (FluentValidation auto-discovery is optional)

## Table of Contents
- [Quick Start](#quick-start)
- [Manual Validation Rules](#manual-validation-rules)
- [FluentValidation Auto-Discovery](#fluentvalidation-auto-discovery)
- [Supported Validation Rules](#supported-validation-rules)
- [Advanced Usage](#advanced-usage)
- [Best Practices](#best-practices)
- [Examples](#examples)

---

## Quick Start

### Basic Usage (Manual Rules)

```csharp
app.MapPost("/todos", async (TodoRequest request) => 
{
    // Your endpoint implementation
})
.WithValidation<TodoRequest>(config =>
{
    config.Property(x => x.Title)
        .Required()
        .MinLength(1)
        .MaxLength(200);
    
    config.Property(x => x.Email)
        .Required()
        .Email();
});
```

**Generated OpenAPI:**
```json
{
  "components": {
    "schemas": {
      "TodoRequest": {
        "required": ["title", "email"],
        "type": "object",
        "properties": {
          "title": {
            "maxLength": 200,
            "minLength": 1,
            "type": "string",
            "description": "Validation rules:\n- Required\n- Minimum length: 1 characters\n- Maximum length: 200 characters"
          },
          "email": {
            "type": "string",
            "format": "email",
            "description": "Validation rules:\n- Required\n- Must be a valid email address"
          }
        }
      }
    }
  }
}
```

---

## Manual Validation Rules

### Custom Property Descriptions

You can add custom descriptions to properties that will appear before the validation rules:

```csharp
config.Property(x => x.Title)
    .Description("The title of the todo item")
    .Required()
    .MinLength(1)
    .MaxLength(200);

config.Property(x => x.Email)
    .Description("Contact email for notifications")
    .Required()
    .Email();
```

**Generated OpenAPI:**
```json
{
  "title": {
    "maxLength": 200,
    "minLength": 1,
    "type": "string",
    "description": "The title of the todo item\n\nValidation rules:\n- Required\n- Minimum length: 1 characters\n- Maximum length: 200 characters"
  },
  "email": {
    "type": "string",
    "format": "email",
    "description": "Contact email for notifications\n\nValidation rules:\n- Required\n- Must be a valid email address"
  }
}
```

**Note:** The custom description appears first, followed by a blank line, then the validation rules section.

### Hiding Validation Rules from Description

By default, all validation rules are listed in the property's description field in the OpenAPI documentation. If you prefer to only include the OpenAPI constraint fields (like `minLength`, `maxLength`, `required`) without listing the rules in the description, you can disable it:

```csharp
app.MapPost("/products", async (ProductRequest request) => 
{
    // Your endpoint implementation
})
.WithValidation<ProductRequest>(config =>
{
    // Disable listing validation rules in descriptions
    config.ListRulesInDescription(false);
    
    config.Property(x => x.Name)
        .Description("The product name")  // Custom description is still shown
        .Required()
        .MinLength(1)
        .MaxLength(200);
    
    config.Property(x => x.Price)
        .GreaterThan(0);
});
```

**Generated OpenAPI (with `ListRulesInDescription(false)`):**
```json
{
  "name": {
    "maxLength": 200,
    "minLength": 1,
    "type": "string",
    "description": "The product name"
  },
  "price": {
    "type": "number",
    "format": "decimal",
    "exclusiveMinimum": "0"
  }
}
```

**Note:** 
- The validation constraints (`minLength`, `maxLength`, `required`, etc.) are still applied to the OpenAPI schema
- Custom descriptions added via `.Description()` are still included
- Only the "Validation rules:" section is omitted from the description field
- Default value is `true` (validation rules are listed by default)

#### Per-Property Control

You can also control this setting on a per-property basis, which is useful when you want to hide validation rules for some properties but not others:

```csharp
app.MapPost("/products", async (ProductRequest request) => 
{
    // Your endpoint implementation
})
.WithValidation<ProductRequest>(config =>
{
    // This property will show validation rules in description (default behavior)
    config.Property(x => x.Name)
        .Description("The product name")
        .Required()
        .MinLength(1)
        .MaxLength(200);
    
    // This property will NOT show validation rules in description
    config.Property(x => x.Price)
        .Description("The product price in USD")
        .ListRulesInDescription(false)  // Per-property override
        .GreaterThan(0);
    
    // This property will also NOT show validation rules
    config.Property(x => x.Sku)
        .ListRulesInDescription(false)
        .Required()
        .Pattern(@"^[A-Z0-9-]+$");
});
```

**Generated OpenAPI:**
```json
{
  "name": {
    "maxLength": 200,
    "minLength": 1,
    "type": "string",
    "description": "The product name\n\nValidation rules:\n- Required\n- Minimum length: 1 characters\n- Maximum length: 200 characters"
  },
  "price": {
    "type": "number",
    "format": "decimal",
    "exclusiveMinimum": "0",
    "description": "The product price in USD"
  },
  "sku": {
    "type": "string",
    "pattern": "^[A-Z0-9-]+$"
  }
}
```

**Priority:** Per-property settings take precedence over global configuration settings.

#### Controlling FluentValidation Auto-Discovered Rules

The per-property `ListRulesInDescription()` also works with FluentValidation auto-discovered rules. You can use `WithValidation` to control the display of auto-discovered rules without overriding the validation logic:

```csharp
// FluentValidation validator (auto-discovered)
public class ProductRequestValidator : AbstractValidator<ProductRequest>
{
    public ProductRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Price).GreaterThan(0);
        RuleFor(x => x.Sku).NotEmpty().Matches(@"^[A-Z0-9-]+$");
    }
}

// In your endpoint - control how auto-discovered rules are displayed
app.MapPost("/products", async (ProductRequest request) => { ... })
   .WithValidation<ProductRequest>(config =>
   {
       // Show FluentValidation rules for Name (uses auto-discovered rules)
       config.Property(x => x.Name)
           .Description("The product name");
       
       // Hide FluentValidation rules for Price
       config.Property(x => x.Price)
           .Description("The product price in USD")
           .ListRulesInDescription(false);
       
       // Hide FluentValidation rules for Sku
       config.Property(x => x.Sku)
           .ListRulesInDescription(false);
   });
```

This allows you to keep your FluentValidation logic intact while controlling the OpenAPI documentation presentation on a per-property basis.

### String Validation

```csharp
config.Property(x => x.Title)
    .Required()                          // Marks as required in OpenAPI
    .MinLength(5)                        // Sets minLength constraint
    .MaxLength(100)                      // Sets maxLength constraint
    .Pattern(@"^[A-Za-z0-9\s]+$");      // Sets regex pattern

config.Property(x => x.Email)
    .Email();                            // Sets format: "email"

config.Property(x => x.Website)
    .Url();                              // Sets format: "uri"
```

### Numeric Validation

```csharp
config.Property(x => x.Priority)
    .GreaterThanOrEqual(0)               // Sets minimum (inclusive)
    .LessThanOrEqual(10);                // Sets maximum (inclusive)

config.Property(x => x.Score)
    .GreaterThan(0)                      // Sets minimum (exclusive)
    .LessThan(100);                      // Sets maximum (exclusive)

config.Property(x => x.Rating)
    .Between(1, 5);                      // Sets both min and max (inclusive)
```

### Custom Validation Messages

```csharp
config.Property(x => x.DueDate)
    .Custom("Due date must be in the future");

config.Property(x => x.Budget)
    .Custom("Budget must be greater than zero and less than $1,000,000");
```

**Note:** Custom rules appear only in the description - they don't create OpenAPI schema constraints since they can't be represented in the standard.

---

## FluentValidation Auto-Discovery

### Enable Auto-Discovery (Optional)

To automatically extract validation rules from FluentValidation validators, add this to your OpenAPI configuration:

```csharp
builder.Services.AddOpenApi(options =>
{
    options.AddValidationSupport(); // Enables FluentValidation auto-discovery
});
```

**Important:** `WithValidation()` works without this configuration. `AddValidationSupport()` is only needed for automatic FluentValidation rule extraction.

### Automatic Rule Extraction

Given a FluentValidation validator:

```csharp
public class TodoRequestValidator : AbstractValidator<TodoRequest>
{
    public TodoRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(200);
        
        RuleFor(x => x.Description)
            .MaximumLength(1000);
        
        RuleFor(x => x.Email)
            .EmailAddress();
        
        RuleFor(x => x.Priority)
            .InclusiveBetween(0, 10);
    }
}
```

**With `AddValidationSupport()` enabled**, these rules automatically appear in your OpenAPI spec - no manual configuration needed!

### Supported FluentValidation Rules

The library automatically converts the following FluentValidation rules to OpenAPI constraints:

| FluentValidation Rule | Converts To | OpenAPI Output |
|----------------------|-------------|----------------|
| `NotNull()` / `NotEmpty()` | `RequiredRule` | Added to `required` array |
| `MinimumLength(n)` | `StringLengthRule` | `minLength: n` |
| `MaximumLength(n)` | `StringLengthRule` | `maxLength: n` |
| `Length(min, max)` | `StringLengthRule` | `minLength`, `maxLength` |
| `Matches(regex)` | `PatternRule` | `pattern: "regex"` |
| `EmailAddress()` | `EmailRule` | `format: "email"` |
| `GreaterThan(n)` | `RangeRule<T>` | `minimum: n` (exclusive) |
| `GreaterThanOrEqual(n)` | `RangeRule<T>` | `minimum: n` |
| `LessThan(n)` | `RangeRule<T>` | `maximum: n` (exclusive) |
| `LessThanOrEqual(n)` | `RangeRule<T>` | `maximum: n` |
| `InclusiveBetween(min, max)` | `RangeRule<T>` | `minimum`, `maximum` |
| `ExclusiveBetween(min, max)` | `RangeRule<T>` | `minimum`, `maximum` (exclusive) |
| **All other validators** | `CustomRule<T>` | **Appears in description only** |

**Note:** Validators not explicitly listed above (such as `CreditCard()`, `Must()`, custom validators, etc.) are automatically documented as custom rules in the property's description field. The error message from FluentValidation is extracted and displayed in the "Validation rules:" section.

---

## Advanced Usage

### Manual Override of FluentValidation Rules

You can override or supplement auto-discovered FluentValidation rules on a **per-property** basis:

```csharp
// FluentValidation validator defines: Title (NotEmpty, MaxLength:200)
public class TodoRequestValidator : AbstractValidator<TodoRequest>
{
    public TodoRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
    }
}

// Endpoint with manual override
app.MapPost("/todos", async (TodoRequest request) => { ... })
   .WithValidation<TodoRequest>(config =>
   {
       // Override Title validation (replaces FluentValidation rules for Title)
       config.Property(x => x.Title)
           .Required()
           .MinLength(5)     // Different from FluentValidation
           .MaxLength(100);  // Different from FluentValidation
       
       // Description uses FluentValidation rules (no manual override)
       
       // Add new validation not in FluentValidation
       config.Property(x => x.Priority)
           .GreaterThanOrEqual(0)
           .LessThanOrEqual(10);
   });
```

**Result:**
- `title`: Uses manual rules (minLength: 5, maxLength: 100, required: true)
- `description`: Uses FluentValidation rules (maxLength: 1000)
- `priority`: Uses manual rules (minimum: 0, maximum: 10)

### Combining Multiple Rules

You can chain multiple validation rules on the same property:

```csharp
config.Property(x => x.Username)
    .Required()
    .MinLength(3)
    .MaxLength(20)
    .Pattern(@"^[a-zA-Z0-9_]+$")
    .Custom("Username must not contain profanity");
```

**Generated Description:**
```
Validation rules:
- Required
- Minimum length: 3 characters
- Maximum length: 20 characters
- Must match pattern: ^[a-zA-Z0-9_]+$
- Username must not contain profanity
```

---

## Supported Validation Rules

### PropertyValidationBuilder Methods

| Method | Parameters | OpenAPI Schema Impact | Description Entry |
|--------|-----------|----------------------|-------------------|
| `Required()` | `errorMessage?` | Adds to `required` array | "Required" |
| `MinLength(int)` | `min, errorMessage?` | `minLength: n` | "Minimum length: n characters" |
| `MaxLength(int)` | `max, errorMessage?` | `maxLength: n` | "Maximum length: n characters" |
| `Length(int, int)` | `min, max, errorMessage?` | `minLength`, `maxLength` | "Length must be between n and m characters" |
| `Pattern(string)` | `regex, errorMessage?` | `pattern: "regex"` | "Must match pattern: regex" |
| `Email()` | `errorMessage?` | `format: "email"` | "Must be a valid email address" |
| `Url()` | `errorMessage?` | `format: "uri"` | "Must be a valid URL" |
| `Custom(string)` | `message` | None | Uses custom message |

### Extension Methods (for IComparable<T> types)

| Method | Parameters | OpenAPI Schema Impact | Description Entry |
|--------|-----------|----------------------|-------------------|
| `GreaterThan(T)` | `value, errorMessage?` | `minimum: value` (exclusive) | "Must be > value" |
| `GreaterThanOrEqual(T)` | `value, errorMessage?` | `minimum: value` | "Must be >= value" |
| `LessThan(T)` | `value, errorMessage?` | `maximum: value` (exclusive) | "Must be < value" |
| `LessThanOrEqual(T)` | `value, errorMessage?` | `maximum: value` | "Must be <= value" |
| `Between(T, T)` | `min, max, errorMessage?` | `minimum`, `maximum` | "Must be >= min and <= max" |

---

## Best Practices

### 1. Use for OpenAPI Documentation Only
`WithValidation` is designed purely for OpenAPI documentation. For actual request validation:
- Use **FluentValidation** for complex validation logic
- Use **DataAnnotations** for simple validation
- The library already integrates both seamlessly

### 2. Leverage FluentValidation Auto-Discovery
If you already have FluentValidation validators:
```csharp
// Enable auto-discovery
builder.Services.AddOpenApi(options => options.AddValidationSupport());

// No need to manually duplicate rules - they're auto-documented!
```

### 3. Use Manual Rules Strategically
Use `WithValidation()` when:
- You don't have a FluentValidation validator for the type
- You want to override specific FluentValidation rules for OpenAPI
- You need to add additional documentation-only rules

### 4. Keep Validation Rules Close to Your Endpoint
```csharp
// ✅ Good: Validation defined with the endpoint
app.MapPost("/todos", handler)
   .WithValidation<TodoRequest>(config => { ... });

// ❌ Avoid: Validation configuration scattered across files
```

### 5. Use Custom Rules for Business Logic Documentation
```csharp
config.Property(x => x.DueDate)
    .Custom("Due date must be in the future and not on a weekend");

config.Property(x => x.Budget)
    .GreaterThan(0)
    .LessThan(1000000)
    .Custom("Budget requests over $100,000 require manager approval");
```

---

## Examples

### Example 1: E-commerce Product Request

```csharp
public record CreateProductRequest
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public int StockQuantity { get; init; }
    public string? Sku { get; init; }
    public string? ImageUrl { get; init; }
}

app.MapPost("/products", async (CreateProductRequest request) => 
{
    // Implementation
})
.WithValidation<CreateProductRequest>(config =>
{
    config.Property(x => x.Name)
        .Required()
        .MinLength(3)
        .MaxLength(100);
    
    config.Property(x => x.Description)
        .MaxLength(500);
    
    config.Property(x => x.Price)
        .GreaterThan(0)
        .Custom("Price must be greater than $0");
    
    config.Property(x => x.StockQuantity)
        .GreaterThanOrEqual(0);
    
    config.Property(x => x.Sku)
        .Pattern(@"^[A-Z]{3}-\d{4}$")
        .Custom("SKU format: XXX-0000 (e.g., ABC-1234)");
    
    config.Property(x => x.ImageUrl)
        .Url();
});
```

### Example 2: User Registration with FluentValidation Override

```csharp
// Existing FluentValidation validator
public class RegisterUserRequestValidator : AbstractValidator<RegisterUserRequest>
{
    public RegisterUserRequestValidator()
    {
        RuleFor(x => x.Email).EmailAddress();
        RuleFor(x => x.Password).MinimumLength(8);
        RuleFor(x => x.Username).MinimumLength(3).MaximumLength(20);
    }
}

// Endpoint with manual override for specific requirements
app.MapPost("/auth/register", async (RegisterUserRequest request) => 
{
    // Implementation
})
.WithValidation<RegisterUserRequest>(config =>
{
    // Override Username rules to be more specific for OpenAPI docs
    config.Property(x => x.Username)
        .Required()
        .MinLength(3)
        .MaxLength(20)
        .Pattern(@"^[a-zA-Z0-9_]+$")
        .Custom("Username must contain only letters, numbers, and underscores");
    
    // Email and Password use FluentValidation rules automatically
    
    // Add additional documentation
    config.Property(x => x.Age)
        .GreaterThanOrEqual(13)
        .Custom("Users must be at least 13 years old per COPPA regulations");
});
```

### Example 3: Complex Nested Object

```csharp
public record CreateOrderRequest
{
    public string CustomerEmail { get; init; } = string.Empty;
    public AddressModel ShippingAddress { get; init; } = new();
    public List<OrderItemModel> Items { get; init; } = [];
}

public record AddressModel
{
    public string Street { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string PostalCode { get; init; } = string.Empty;
}

app.MapPost("/orders", async (CreateOrderRequest request) => 
{
    // Implementation
})
.WithValidation<CreateOrderRequest>(config =>
{
    config.Property(x => x.CustomerEmail)
        .Required()
        .Email();
    
    config.Property(x => x.Items)
        .Required()
        .Custom("Order must contain at least one item");
});
```

---

## Troubleshooting

### Validation Rules Not Appearing in OpenAPI

**Problem:** Rules configured with `WithValidation()` don't appear in the generated OpenAPI spec.

**Solution:** 
1. Ensure you're calling `WithValidation()` on the `RouteHandlerBuilder`
2. Check that the property names match exactly (case-sensitive)
3. Verify the OpenAPI document is being generated for the correct endpoint

### FluentValidation Rules Not Auto-Discovered

**Problem:** FluentValidation validators aren't automatically documented.

**Solution:**
1. Add `options.AddValidationSupport()` in your `AddOpenApi()` configuration
2. Ensure validators are registered in DI (the source generator does this automatically)
3. Check that the validator implements `AbstractValidator<T>` or `IValidator<T>`

### Rules Appearing Multiple Times

**Problem:** Validation rules appear duplicated in the description.

**Solution:** 
- Don't call both `WithValidation()` and have FluentValidation auto-discovery enabled for the same property
- Use `WithValidation()` to override FluentValidation rules, not supplement them (per-property replacement)

---

## Migration Guide

### From Manual OpenAPI Configuration

**Before:**
```csharp
builder.WithOpenApi(operation =>
{
    operation.RequestBody.Content["application/json"].Schema.Properties["title"].MaxLength = 200;
    operation.RequestBody.Content["application/json"].Schema.Required.Add("title");
    return operation;
});
```

**After:**
```csharp
builder.WithValidation<TodoRequest>(config =>
{
    config.Property(x => x.Title)
        .Required()
        .MaxLength(200);
});
```

### From FluentValidation.AspNetCore (Deprecated)

The old FluentValidation.AspNetCore package required manual OpenAPI integration. With this library:

**Before:**
```csharp
// Manual OpenAPI configuration + runtime validation
services.AddFluentValidationRulesToSwagger();
services.AddFluentValidationAutoValidation();
```

**After:**
```csharp
// Auto-documentation only (use FluentValidation directly for validation)
builder.Services.AddOpenApi(options => options.AddValidationSupport());
```

---

## FAQ

**Q: Does `WithValidation` perform runtime validation?**  
A: No. It only documents validation rules in OpenAPI. Use FluentValidation or DataAnnotations for actual request validation.

**Q: Can I use `WithValidation` without FluentValidation?**  
A: Yes! `WithValidation()` works independently. FluentValidation auto-discovery is an optional convenience feature.

**Q: What happens if I configure the same property in both FluentValidation and `WithValidation`?**  
A: `WithValidation` rules completely replace FluentValidation rules for that specific property. Other properties still use FluentValidation rules.

**Q: Can I use this with Swagger UI?**  
A: Yes! The OpenAPI spec generated by this library works with any OpenAPI-compatible tool (Swagger UI, Scalar, Redoc, etc.).

**Q: Does this work with .NET 8 and earlier?**  
A: This library targets .NET 10.0 and uses the new `Microsoft.AspNetCore.OpenApi` package. For earlier versions, consider manual OpenAPI configuration.

---

## Related Documentation

- [FluentValidation Integration](https://github.com/IeuanWalker/MinimalApi.Endpoints/wiki/Fluent-validation)
- [DataAnnotations Validation](https://github.com/IeuanWalker/MinimalApi.Endpoints/wiki/Data-annotations)
- [OpenAPI Customization](https://github.com/IeuanWalker/MinimalApi.Endpoints/wiki)

---

## Feedback & Contributions

Found an issue or have a suggestion? [Open an issue on GitHub](https://github.com/IeuanWalker/MinimalApi.Endpoints/issues).
