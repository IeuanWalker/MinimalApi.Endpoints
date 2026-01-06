# Validation Documentation for OpenAPI

This feature automatically documents validation rules in your OpenAPI specification from three sources:
1. **FluentValidation** validators (automatic)
2. **DataAnnotations** attributes (automatic)
3. **WithValidation** manual API (manual configuration)

All validation documentation is **OpenAPI-only** - no runtime validation is performed by this library. Your existing runtime validation (FluentValidation, DataAnnotations, etc.) continues to work as normal.

## Table of Contents

- [Setup](#setup)
- [Quick Start](#quick-start)
- [FluentValidation Auto-Documentation](#fluentvalidation-auto-documentation)
- [DataAnnotations Auto-Documentation](#dataannotations-auto-documentation)
- [WithValidation Manual API](#withvalidation-manual-api)
- [Combining Validation Sources](#combining-validation-sources)
- [Advanced Features](#advanced-features)
- [Complete Examples](#complete-examples)
- [Troubleshooting](#troubleshooting)

## Setup

To enable automatic validation documentation, call `EnhanceRequestProperties()` when configuring OpenAPI:

```csharp
builder.Services.AddOpenApi(config =>
{
    config.EnhanceRequestProperties();
});
```

### EnhanceRequestProperties Configuration

The `EnhanceRequestProperties()` method accepts optional parameters to customize behavior:

```csharp
builder.Services.AddOpenApi(config =>
{
    config.EnhanceRequestProperties(
        autoDocumentFluentValidation: true,           // Default: true
        appendRulesToPropertyDescription: true        // Default: true
    );
});
```

#### Parameters

**`autoDocumentFluentValidation`** (default: `true`)
- When `true`: Automatically extracts and documents all FluentValidation rules
- When `false`: Only FluentValidation rules explicitly overridden with `.WithValidation()` are documented
- Use `false` if you want complete manual control over which validation rules appear in OpenAPI

**`appendRulesToPropertyDescription`** (default: `true`)
- When `true`: Adds a "Validation rules:" section listing all validation constraints in property descriptions
- When `false`: Validation constraints are still applied to the schema (required, minLength, etc.) but not listed in descriptions
- Use `false` for a cleaner OpenAPI specification when you don't want verbose validation rule listings

**Example - Manual Control Only:**
```csharp
builder.Services.AddOpenApi(config =>
{
    config.EnhanceRequestProperties(
        autoDocumentFluentValidation: false,
        appendRulesToPropertyDescription: false
    );
});
```
This configuration:
- Disables automatic FluentValidation discovery
- Removes validation rule descriptions from properties
- Still applies schema constraints (required, min/max, etc.)
- Requires manual `.WithValidation()` calls to document any validation

### What EnhanceRequestProperties Does

This method configures multiple OpenAPI transformers that enhance your API documentation:

1. **Type Transformer** - Sets appropriate primitive types (fixes nullable/non-nullable representation)
2. **Enum Transformer** - Adds complete enum information (all possible values)
3. **Validation Transformer** - Documents validation rules from FluentValidation, DataAnnotations, and WithValidation API
4. **OneOf Reordering** - Ensures proper structure for discriminated unions

Without calling `EnhanceRequestProperties()`, validation documentation features will not be enabled.

## Quick Start

### Automatic Documentation

Once `EnhanceRequestProperties()` is configured, validation rules are automatically documented:

```csharp
// FluentValidation validator
public class CreateTodoRequestValidator : AbstractValidator<CreateTodoRequest>
{
    public CreateTodoRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaxLength(200);
        
        RuleFor(x => x.Priority)
            .InclusiveBetween(1, 5);
    }
}

// DataAnnotations model
public class CreateTodoRequest
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; }
    
    [Range(1, 5)]
    public int Priority { get; set; }
}

// Endpoint - validation rules automatically documented!
public class CreateTodoEndpoint : IEndpoint<CreateTodoRequest, TodoResponse>
{
    public static void Configure(RouteHandlerBuilder builder)
    {
        builder.Post("/todos");
    }
    
    public async Task<TodoResponse> Handle(CreateTodoRequest request, CancellationToken ct)
    {
        // Your logic here
    }
}
```

**Result:** The OpenAPI specification automatically includes:
- `Title`: required, maxLength: 200
- `Priority`: minimum: 1, maximum: 5

### Manual Documentation with WithValidation

Override or supplement automatic validation with manual configuration:

```csharp
public static void Configure(RouteHandlerBuilder builder)
{
    builder.Post("/todos")
        .WithValidation<CreateTodoRequest>(config =>
        {
            config.Property(x => x.Title)
                .Description("The title of the todo item")
                .Required()
                .MinLength(1)
                .MaxLength(200);
                
            config.Property(x => x.Priority)
                .Description("Priority level (1=lowest, 5=highest)")
                .GreaterThanOrEqual(1)
                .LessThanOrEqual(5);
        });
}
```

## FluentValidation Auto-Documentation

### Supported Validators

All FluentValidation validators are documented automatically:

#### String Validators
- `NotEmpty()`, `NotNull()` → Required
- `Length(min, max)` → minLength, maxLength
- `MinimumLength(n)` → minLength
- `MaximumLength(n)` → maxLength
- `Matches(regex)` → pattern
- `EmailAddress()` → format: email

#### Numeric Validators
- `GreaterThan(n)` → minimum (exclusive)
- `GreaterThanOrEqual(n)` → minimum
- `LessThan(n)` → maximum (exclusive)
- `LessThanOrEqual(n)` → maximum
- `InclusiveBetween(min, max)` → minimum, maximum
- `ExclusiveBetween(min, max)` → minimum (exclusive), maximum (exclusive)
- `Equal(n)` → Custom rule with message
- `NotEqual(n)` → Custom rule with message
- `PrecisionScale(precision, scale)` → Custom rule with message

#### Other Validators
- `Must(predicate).WithMessage("...")` → Custom rule with your message
- `CreditCard()` → Custom rule
- `IsEnumName()` → Custom rule
- Any custom validator → Custom rule with error message

### Comparison Property Validators

FluentValidation supports comparing one property to another:

```csharp
RuleFor(x => x.ConfirmPassword)
    .Equal(x => x.Password)
    .WithMessage("Passwords must match");

RuleFor(x => x.EndDate)
    .GreaterThan(x => x.StartDate)
    .WithMessage("End date must be after start date");
```

These are documented with the actual comparison value extracted from the validator.

### Nested Object Validation

Nested objects with their own validators are properly documented:

```csharp
public class CreateOrderRequest
{
    public Address ShippingAddress { get; set; }
    public List<OrderItem> Items { get; set; }
}

public class AddressValidator : AbstractValidator<Address>
{
    public AddressValidator()
    {
        RuleFor(x => x.Street).NotEmpty().MaxLength(100);
        RuleFor(x => x.City).NotEmpty().MaxLength(50);
        RuleFor(x => x.ZipCode).Matches(@"^\d{5}$");
    }
}

public class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderRequestValidator()
    {
        RuleFor(x => x.ShippingAddress).SetValidator(new AddressValidator());
        RuleForEach(x => x.Items).SetValidator(new OrderItemValidator());
    }
}
```

The OpenAPI spec will include:
- `ShippingAddress`: $ref to Address schema with all validation rules
- Each property in Address documented with its validation rules

### Custom Error Messages

Custom error messages defined with `.WithMessage()` are automatically extracted:

```csharp
RuleFor(x => x.Items)
    .Must(list => list.Count >= 1)
    .WithMessage("Order must contain at least one item");
```

**OpenAPI output:** "Order must contain at least one item" appears in the description.

## DataAnnotations Auto-Documentation

### Supported Attributes

All major DataAnnotations attributes are documented automatically:

#### Required and Null Validation
- `[Required]` → Required property
- `[Required(AllowEmptyStrings = true)]` → Required (allows empty strings)

#### String Validation
- `[StringLength(max)]` → maxLength
- `[StringLength(max, MinimumLength = min)]` → minLength, maxLength
- `[MinLength(n)]` → minLength
- `[MaxLength(n)]` → maxLength
- `[EmailAddress]` → format: email
- `[Url]` → format: uri
- `[Phone]` → Custom rule
- `[CreditCard]` → Custom rule
- `[RegularExpression(pattern)]` → pattern

#### Numeric Validation
- `[Range(min, max)]` → minimum, maximum
- `[Range(min, max, MinimumIsExclusive = true)]` → minimum (exclusive), maximum
- `[Range(min, max, MaximumIsExclusive = true)]` → minimum, maximum (exclusive)

#### Comparison Validation
- `[Compare("OtherProperty")]` → Custom rule

#### Length Validation
- `[Length(min, max)]` → minItems, maxItems (for collections)

### Custom Error Messages

DataAnnotations custom messages are extracted:

```csharp
public class CreateUserRequest
{
    [Required(ErrorMessage = "Username is required")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters")]
    public string Username { get; set; }
    
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; }
}
```

## WithValidation Manual API

The `WithValidation<TRequest>()` extension method provides a fluent API for manually configuring validation documentation.

### Basic Usage

```csharp
public static void Configure(RouteHandlerBuilder builder)
{
    builder.Post("/products")
        .WithValidation<CreateProductRequest>(config =>
        {
            config.Property(x => x.Name)
                .Required()
                .MinLength(1)
                .MaxLength(100);
                
            config.Property(x => x.Price)
                .GreaterThan(0);
        });
}
```

### Available Validation Methods

#### Required
```csharp
config.Property(x => x.Name).Required();
```

#### String Constraints
```csharp
config.Property(x => x.Description)
    .MinLength(10)
    .MaxLength(500)
    .Pattern(@"^[A-Za-z0-9\s]+$")  // Regex pattern
    .EmailFormat()                   // Email format
    .UriFormat();                    // URI format
```

#### Numeric Constraints
```csharp
config.Property(x => x.Age)
    .GreaterThan(0)                     // Exclusive minimum
    .GreaterThanOrEqual(18)             // Inclusive minimum
    .LessThan(150)                      // Exclusive maximum
    .LessThanOrEqual(120);              // Inclusive maximum
```

#### Custom Descriptions

Add custom descriptions that appear before validation rules:

```csharp
config.Property(x => x.Priority)
    .Description("Priority level: 1=Low, 2=Medium, 3=High, 4=Urgent, 5=Critical")
    .GreaterThanOrEqual(1)
    .LessThanOrEqual(5);
```

**OpenAPI output:**
```
Priority level: 1=Low, 2=Medium, 3=High, 4=Urgent, 5=Critical

Validation rules:
- Minimum value: 1
- Maximum value: 5
```

### Advanced Operations

#### Remove Rules

Remove all validation rules for a property:

```csharp
config.Property(x => x.OptionalField).Remove();
```

#### Alter Existing Rules

Modify automatically discovered validation rules:

```csharp
config.Property(x => x.Title)
    .Alter(rules =>
    {
        // Find and modify the MaxLength rule
        var maxLengthRule = rules.OfType<MaxLengthRule>().FirstOrDefault();
        if (maxLengthRule != null)
        {
            maxLengthRule.MaxLength = 500; // Change from 200 to 500
        }
    });
```

#### Control Rule Display

Hide validation rules from property descriptions globally or per-property:

```csharp
// Global: Hide all validation rules from descriptions
config.ListRulesInDescription(false);

// Per-property: Override global setting
config.Property(x => x.ImportantField)
    .Required()
    .MaxLength(100)
    .ListRulesInDescription(true);  // Show rules for this property only

config.Property(x => x.InternalId)
    .Required()
    .ListRulesInDescription(false);  // Hide rules for this property
```

**When `ListRulesInDescription(false)`:**
- OpenAPI constraints (minLength, maxLength, required, etc.) are still applied
- Custom descriptions via `.Description()` are still shown
- Only the "Validation rules:" bullet list is hidden

## Combining Validation Sources

### Override Priority

When the same property has validation from multiple sources, the priority is:

1. **WithValidation manual rules** (highest priority)
2. **FluentValidation auto-discovered rules**
3. **DataAnnotations auto-discovered rules** (lowest priority)

Rules are merged **per property**. If you configure a property with `WithValidation`, all automatic rules for that property are replaced.

### Example: Selective Override

```csharp
// FluentValidation validator
public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MaxLength(50);
        RuleFor(x => x.Email).EmailAddress();
        RuleFor(x => x.Age).GreaterThanOrEqual(18);
    }
}

// Endpoint configuration
public static void Configure(RouteHandlerBuilder builder)
{
    builder.Post("/users")
        .WithValidation<CreateUserRequest>(config =>
        {
            // Override Username validation completely
            config.Property(x => x.Username)
                .Description("Unique username for login")
                .Required()
                .MinLength(3)
                .MaxLength(30);  // Different from FluentValidation
            
            // Email and Age use FluentValidation rules (no override)
        });
}
```

**Result:**
- `Username`: Uses WithValidation rules (min: 3, max: 30, custom description)
- `Email`: Uses FluentValidation rules (email format)
- `Age`: Uses FluentValidation rules (minimum: 18)

## Advanced Features

### Nested Object Support

Nested objects preserve their schema structure and validation rules:

```csharp
public class CreateOrderRequest
{
    public Address ShippingAddress { get; set; }
    public Address BillingAddress { get; set; }
}

// The OpenAPI spec preserves $ref to Address schema
// Each Address property shows its own validation rules
```

### Type-Safe Configuration

All methods use strongly-typed property selectors:

```csharp
config.Property(x => x.Email)  // Compile-time checked
    .EmailFormat();
```

### Validation Rule Descriptions

Validation rules automatically generate human-readable descriptions:

- **Required** → "Required"
- **MinLength(5)** → "Minimum length: 5 characters"
- **MaxLength(100)** → "Maximum length: 100 characters"
- **Pattern("^\d+$")** → "Must match pattern: ^\d+$"
- **EmailFormat()** → "Must be a valid email address"
- **GreaterThanOrEqual(0)** → "Minimum value: 0"
- **LessThan(100)** → "Maximum value: 100 (exclusive)"

## Complete Examples

### Example 1: E-Commerce Product

```csharp
public class CreateProductRequest
{
    public string Name { get; set; }
    public string Description { get; set; }
    public decimal Price { get; set; }
    public string Sku { get; set; }
    public int StockQuantity { get; set; }
    public List<string> Categories { get; set; }
}

public class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaxLength(200);
        RuleFor(x => x.Price).GreaterThan(0).PrecisionScale(10, 2);
        RuleFor(x => x.Sku).Matches(@"^[A-Z0-9\-]+$");
    }
}

public class CreateProductEndpoint : IEndpoint<CreateProductRequest, ProductResponse>
{
    public static void Configure(RouteHandlerBuilder builder)
    {
        builder.Post("/products")
            .WithValidation<CreateProductRequest>(config =>
            {
                config.Property(x => x.Description)
                    .Description("Detailed product description for customers")
                    .MinLength(50)
                    .MaxLength(2000);
                
                config.Property(x => x.StockQuantity)
                    .Description("Current inventory level")
                    .GreaterThanOrEqual(0);
                
                config.Property(x => x.Categories)
                    .Description("Product category tags")
                    .Required();
            });
    }
    
    public async Task<ProductResponse> Handle(CreateProductRequest request, CancellationToken ct)
    {
        // Implementation
    }
}
```

### Example 2: User Registration

```csharp
public class RegisterUserRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; }
    
    [Required]
    public string Password { get; set; }
    
    [Required]
    [Compare("Password")]
    public string ConfirmPassword { get; set; }
    
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string FullName { get; set; }
    
    [Range(13, 120)]
    public int? Age { get; set; }
}

public class RegisterUserRequestValidator : AbstractValidator<RegisterUserRequest>
{
    public RegisterUserRequestValidator()
    {
        RuleFor(x => x.Password)
            .MinimumLength(8)
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter")
            .Matches(@"[0-9]").WithMessage("Password must contain at least one digit");
    }
}

public class RegisterUserEndpoint : IEndpoint<RegisterUserRequest, UserResponse>
{
    public static void Configure(RouteHandlerBuilder builder)
    {
        builder.Post("/auth/register")
            .WithValidation<RegisterUserRequest>(config =>
            {
                // Enhance FluentValidation password rules with description
                config.Property(x => x.Password)
                    .Description("Must be at least 8 characters with uppercase, lowercase, and number")
                    .MinLength(8)
                    .Pattern(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$");
                
                // Age uses DataAnnotations (no override needed)
                // Email uses DataAnnotations (no override needed)
                // ConfirmPassword uses DataAnnotations Compare attribute
            });
    }
    
    public async Task<UserResponse> Handle(RegisterUserRequest request, CancellationToken ct)
    {
        // Implementation
    }
}
```

### Example 3: Complex Nested Object

```csharp
public class CreateOrderRequest
{
    public string OrderNumber { get; set; }
    public Address ShippingAddress { get; set; }
    public Address BillingAddress { get; set; }
    public List<OrderItem> Items { get; set; }
}

public class Address
{
    [Required]
    [MaxLength(100)]
    public string Street { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string City { get; set; }
    
    [Required]
    [RegularExpression(@"^\d{5}(-\d{4})?$")]
    public string ZipCode { get; set; }
}

public class OrderItem
{
    [Required]
    public string ProductId { get; set; }
    
    [Range(1, 1000)]
    public int Quantity { get; set; }
}

public class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderRequestValidator()
    {
        RuleFor(x => x.OrderNumber).NotEmpty().Matches(@"^ORD-\d{6}$");
        RuleFor(x => x.ShippingAddress).NotNull();
        RuleFor(x => x.Items).NotEmpty();
    }
}

public class CreateOrderEndpoint : IEndpoint<CreateOrderRequest, OrderResponse>
{
    public static void Configure(RouteHandlerBuilder builder)
    {
        builder.Post("/orders")
            .WithValidation<CreateOrderRequest>(config =>
            {
                config.Property(x => x.OrderNumber)
                    .Description("Order reference number in format ORD-XXXXXX")
                    .Required()
                    .Pattern(@"^ORD-\d{6}$");
                
                config.Property(x => x.ShippingAddress)
                    .Description("Delivery address")
                    .Required();
                
                config.Property(x => x.BillingAddress)
                    .Description("Billing address (optional, defaults to shipping address)");
                
                config.Property(x => x.Items)
                    .Description("Order line items")
                    .Required();
            });
    }
    
    public async Task<OrderResponse> Handle(CreateOrderRequest request, CancellationToken ct)
    {
        // Implementation
    }
}
```

## Troubleshooting

### Validation Rules Not Appearing

**Problem:** Validation rules from FluentValidation don't appear in OpenAPI spec.

**Solutions:**
1. Ensure FluentValidation validators are registered in DI:
   ```csharp
   builder.Services.AddValidatorsFromAssemblyContaining<Program>();
   ```

2. Check that your validator inherits from `AbstractValidator<T>`

3. Verify the validator type name follows convention: `{RequestType}Validator`

### DataAnnotations Not Documenting

**Problem:** DataAnnotations attributes not showing in OpenAPI.

**Solutions:**
1. Ensure attributes are on public properties
2. Check that the attribute is supported (see list above)
3. Verify the model is used as the request type in the endpoint

### WithValidation Not Working

**Problem:** Manual validation rules not applying.

**Solutions:**
1. Ensure `WithValidation<T>()` is called on the `RouteHandlerBuilder`
2. Check the type parameter matches your request type exactly
3. Verify property selectors reference actual properties

### Rules Not Merging Correctly

**Problem:** Validation rules from multiple sources conflict.

**Solution:** Remember the override priority:
- WithValidation (highest) → FluentValidation → DataAnnotations (lowest)
- Rules merge **per property**, not per rule type

### Nested Objects Losing Validation

**Problem:** Nested object properties don't show validation rules.

**Solutions:**
1. Ensure nested object has its own validator (for FluentValidation)
2. Use `SetValidator()` in parent validator for FluentValidation
3. Add DataAnnotations to nested class properties
4. Verify nested object is not null in request

### Custom Messages Not Extracting

**Problem:** Custom error messages show as placeholders.

**Solutions:**
1. For FluentValidation, use `.WithMessage("Your message")`
2. For DataAnnotations, use `ErrorMessage = "Your message"` parameter
3. Check that message doesn't contain template placeholders like `{PropertyName}`

### Performance Concerns

**Problem:** Startup time increased with many validators.

**Solutions:**
1. The reflection-based discovery happens once at startup
2. Consider using `.ListRulesInDescription(false)` to reduce OpenAPI JSON size
3. For very large APIs, consider documenting only critical endpoints

## Best Practices

### 1. Use Automatic Documentation When Possible

Prefer FluentValidation or DataAnnotations for automatic documentation. Only use `WithValidation` for:
- Custom descriptions
- Overriding auto-discovered rules
- Properties without validators
- Hiding/showing specific rules

### 2. Add Meaningful Descriptions

```csharp
// Good
config.Property(x => x.Status)
    .Description("Order status: pending, processing, shipped, delivered, cancelled")
    .Required();

// Less helpful
config.Property(x => x.Status)
    .Required();
```

### 3. Keep Validation in One Place

Avoid mixing validation sources unnecessarily:

```csharp
// Good - All FluentValidation
public class Validator : AbstractValidator<Request> { }

// Or all DataAnnotations
public class Request { [Required] public string Name { get; set; } }

// Or all WithValidation
.WithValidation<Request>(config => { })

// Less ideal - mixing without reason
public class Request 
{ 
    [Required] public string Name { get; set; }  // DataAnnotations
}
public class Validator : AbstractValidator<Request>
{
    public Validator() 
    {
        RuleFor(x => x.Name).NotEmpty();  // Also FluentValidation
    }
}
```

### 4. Use WithValidation for Overrides

```csharp
// Override when auto-discovered rules aren't quite right
config.Property(x => x.Email)
    .Description("Primary contact email for notifications")
    .Required()
    .EmailFormat()
    .MaxLength(320);  // RFC 5321 maximum
```

### 5. Hide Internal Implementation Details

```csharp
config.Property(x => x.InternalProcessingFlag)
    .ListRulesInDescription(false);  // Don't expose internal validation rules
```

### 6. Document Complex Validations

For complex `.Must()` validations, always add descriptive messages:

```csharp
// FluentValidation
RuleFor(x => x.DeliveryDate)
    .Must(BeAWeekday)
    .WithMessage("Delivery must be scheduled for a weekday (Monday-Friday)");

// Or WithValidation with description
config.Property(x => x.DeliveryDate)
    .Description("Delivery date must be a weekday (Monday-Friday)")
    .Required();
```

---

## Summary

This validation documentation feature provides three complementary approaches:

1. **FluentValidation** - Automatic documentation from your existing validators
2. **DataAnnotations** - Automatic documentation from attributes
3. **WithValidation** - Manual API for fine-grained control

All three sources merge intelligently to provide comprehensive, accurate OpenAPI documentation that matches your actual validation logic.

The feature is **OpenAPI-only** - it documents validation rules but doesn't perform runtime validation. Your existing validation continues to work unchanged.
