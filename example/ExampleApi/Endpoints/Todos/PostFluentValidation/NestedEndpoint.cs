using FluentValidation;
using FluentValidation.Results;
using IeuanWalker.MinimalApi.Endpoints;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ExampleApi.Endpoints.Todos.PostFluentValidation;

public class NestedEndpoint : IEndpoint
{
	public void MapEndpoint(IEndpointRouteBuilder app)
	{
		app.MapPost("/orders", ExecuteAsync)
			.WithName("CreateOrder")
			.WithSummary("Create a new order with nested validation")
			.WithDescription("Creates a new order with comprehensive nested validation using FluentValidation rules")
			.WithTags("Orders")
			.WithFluentValidationSchema(new OrderValidator()) // Explicit validator instance
			.WithResponse<OrderResponse>(201, "Order created successfully")
			.WithResponse(400, "Validation failed")
			.WithResponse(500, "Internal server error");
	}

	private static async Task<Results<Created<OrderResponse>, ValidationProblem>> ExecuteAsync(
		OrderRequestModel request,
		OrderValidator validator)
	{
		// Validate the request
		ValidationResult validationResult = await validator.ValidateAsync(request);
		if (!validationResult.IsValid)
		{
			return TypedResults.ValidationProblem(validationResult.ToDictionary());
		}

		// Process the request (mock implementation)
		OrderResponse order = new OrderResponse
		{
			Id = Guid.NewGuid(),
			OrderNumber = request.OrderNumber,
			CustomerName = $"{request.Customer.FirstName} {request.Customer.LastName}",
			ItemCount = request.Items.Count,
			TotalAmount = request.Items.Sum(i => i.Price * i.Quantity),
			CreatedAt = DateTime.UtcNow
		};

		return TypedResults.Created($"/orders/{order.Id}", order);
	}
}

public class OrderResponse
{
	public Guid Id { get; set; }
	public string OrderNumber { get; set; } = string.Empty;
	public string CustomerName { get; set; } = string.Empty;
	public int ItemCount { get; set; }
	public decimal TotalAmount { get; set; }
	public DateTime CreatedAt { get; set; }
}
