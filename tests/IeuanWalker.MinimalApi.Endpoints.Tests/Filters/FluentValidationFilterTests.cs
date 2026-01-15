using FluentValidation;
using FluentValidation.Results;
using IeuanWalker.MinimalApi.Endpoints.Filters;
using Microsoft.AspNetCore.Http;
using NSubstitute;

namespace IeuanWalker.MinimalApi.Endpoints.Tests.Filters;

public class FluentValidationFilterTests
{
	public class TestModel
	{
		public string Name { get; set; } = string.Empty;
	}

	[Fact]
	public void Constructor_WhenValidatorIsNull_ThrowsArgumentNullException()
	{
		//  Arrange, Act & Assert
		Should.Throw<ArgumentNullException>(() => new FluentValidationFilter<TestModel>(null!))
			.ParamName.ShouldBe("validator");
	}

	[Fact]
	public async Task InvokeAsync_WhenNoMatchingArgument_CallsNextDelegate()
	{
		// Arrange
		IValidator<TestModel> validator = Substitute.For<IValidator<TestModel>>();
		FluentValidationFilter<TestModel> filter = new(validator);

		EndpointFilterInvocationContext context = Substitute.For<EndpointFilterInvocationContext>();
		context.Arguments.Returns([]);

		DefaultHttpContext httpContext = new();
		context.HttpContext.Returns(httpContext);

		EndpointFilterDelegate next = Substitute.For<EndpointFilterDelegate>();
		object expectedResult = new();
		next.Invoke(context).Returns(expectedResult);

		// Act
		object? result = await filter.InvokeAsync(context, next);

		// Assert
		result.ShouldBe(expectedResult);
		await next.Received(1).Invoke(context);
	}

	[Fact]
	public async Task InvokeAsync_WhenValidationPasses_CallsNextDelegate()
	{
		// Arrange
		IValidator<TestModel> validator = Substitute.For<IValidator<TestModel>>();
		TestModel testModel = new()
		{
			Name = "Test"
		};

		validator.ValidateAsync(testModel, Arg.Any<CancellationToken>())
			.Returns(new ValidationResult());

		FluentValidationFilter<TestModel> filter = new(validator);

		EndpointFilterInvocationContext context = Substitute.For<EndpointFilterInvocationContext>();
		context.Arguments.Returns([testModel]);

		DefaultHttpContext httpContext = new();
		context.HttpContext.Returns(httpContext);

		EndpointFilterDelegate next = Substitute.For<EndpointFilterDelegate>();
		object expectedResult = new();
		next.Invoke(context).Returns(expectedResult);

		// Act
		object? result = await filter.InvokeAsync(context, next);

		// Assert
		result.ShouldBe(expectedResult);
		await next.Received(1).Invoke(context);
	}

	[Fact]
	public async Task InvokeAsync_WhenValidationFails_ReturnsValidationProblem()
	{
		// Arrange
		IValidator<TestModel> validator = Substitute.For<IValidator<TestModel>>();
		TestModel testModel = new()
		{
			Name = string.Empty
		};

		ValidationFailure failure = new("Name", "Name is required");
		ValidationResult validationResult = new([failure]);

		validator.ValidateAsync(testModel, Arg.Any<CancellationToken>())
			.Returns(validationResult);

		FluentValidationFilter<TestModel> filter = new(validator);

		EndpointFilterInvocationContext context = Substitute.For<EndpointFilterInvocationContext>();
		context.Arguments.Returns([testModel]);

		DefaultHttpContext httpContext = new();
		context.HttpContext.Returns(httpContext);

		EndpointFilterDelegate next = Substitute.For<EndpointFilterDelegate>();

		// Act
		object? result = await filter.InvokeAsync(context, next);

		// Assert
		result.ShouldNotBeNull();
		await next.DidNotReceive().Invoke(context);
	}
}
