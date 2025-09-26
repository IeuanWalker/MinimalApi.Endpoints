using FluentValidation;

namespace ExampleApi.Endpoints.Todos.PostFluentValidation;

public class OrderRequestModel
{
	public string OrderNumber { get; set; } = string.Empty;
	public CustomerModel Customer { get; set; } = new();
	public List<OrderItemModel> Items { get; set; } = [];
	public AddressModel ShippingAddress { get; set; } = new();
}

public class CustomerModel
{
	public string FirstName { get; set; } = string.Empty;
	public string LastName { get; set; } = string.Empty;
	public string Email { get; set; } = string.Empty;
	public int Age { get; set; }
}

public class OrderItemModel
{
	public string ProductName { get; set; } = string.Empty;
	public int Quantity { get; set; }
	public decimal Price { get; set; }
}

public class AddressModel
{
	public string Street { get; set; } = string.Empty;
	public string City { get; set; } = string.Empty;
	public string PostalCode { get; set; } = string.Empty;
	public string Country { get; set; } = string.Empty;
}

public class OrderValidator : AbstractValidator<OrderRequestModel>
{
	public OrderValidator()
	{
		RuleFor(x => x.OrderNumber)
			.NotEmpty()
			.MinimumLength(5)
			.MaximumLength(20)
			.Matches(@"^ORD-\d{4,10}$");

		RuleFor(x => x.Customer)
			.NotNull()
			.SetValidator(new CustomerValidator());

		RuleFor(x => x.Items)
			.NotEmpty();

		RuleForEach(x => x.Items)
			.SetValidator(new OrderItemValidator());

		RuleFor(x => x.ShippingAddress)
			.NotNull()
			.SetValidator(new AddressValidator());
	}
}

public class CustomerValidator : AbstractValidator<CustomerModel>
{
	public CustomerValidator()
	{
		RuleFor(x => x.FirstName)
			.NotEmpty()
			.MaximumLength(50);

		RuleFor(x => x.LastName)
			.NotEmpty()
			.MaximumLength(50);

		RuleFor(x => x.Email)
			.NotEmpty()
			.EmailAddress()
			.MaximumLength(100);

		RuleFor(x => x.Age)
			.GreaterThanOrEqualTo(18)
			.LessThanOrEqualTo(120);
	}
}

public class OrderItemValidator : AbstractValidator<OrderItemModel>
{
	public OrderItemValidator()
	{
		RuleFor(x => x.ProductName)
			.NotEmpty()
			.MaximumLength(200);

		RuleFor(x => x.Quantity)
			.GreaterThan(0)
			.LessThanOrEqualTo(999);

		RuleFor(x => x.Price)
			.GreaterThan(0)
			.LessThanOrEqualTo(999999.99m);
	}
}

public class AddressValidator : AbstractValidator<AddressModel>
{
	public AddressValidator()
	{
		RuleFor(x => x.Street)
			.NotEmpty()
			.MaximumLength(200);

		RuleFor(x => x.City)
			.NotEmpty()
			.MaximumLength(100);

		RuleFor(x => x.PostalCode)
			.NotEmpty()
			.Matches(@"^\d{5}(-\d{4})?$");

		RuleFor(x => x.Country)
			.NotEmpty()
			.Length(2, 3);
	}
}
