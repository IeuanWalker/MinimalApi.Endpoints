using FluentValidation;
using FluentValidation.Results;
using IeuanWalker.MinimalApi.Endpoints.Filters;
using Microsoft.AspNetCore.Http;
using NSubstitute;

namespace IeuanWalker.MinimalApi.Endpoints.Tests.Filters;

public class FluentValidationFilterTests
{
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

	[Fact]
	public async Task InvokeAsync_WhenMultipleArgumentsWithMatch_ValidatesFirstMatch()
	{
		// Arrange
		IValidator<TestModel> validator = Substitute.For<IValidator<TestModel>>();
		TestModel testModel = new() { Name = "Test" };
		OtherModel otherModel = new() { Id = 1 };

		validator.ValidateAsync(testModel, Arg.Any<CancellationToken>())
			.Returns(new ValidationResult());

		FluentValidationFilter<TestModel> filter = new(validator);

		EndpointFilterInvocationContext context = Substitute.For<EndpointFilterInvocationContext>();
		context.Arguments.Returns([42, "string", otherModel, testModel]); // TestModel is at index 3

		DefaultHttpContext httpContext = new();
		context.HttpContext.Returns(httpContext);

		EndpointFilterDelegate next = Substitute.For<EndpointFilterDelegate>();
		object expectedResult = new();
		next.Invoke(context).Returns(expectedResult);

		// Act
		object? result = await filter.InvokeAsync(context, next);

		// Assert
		result.ShouldBe(expectedResult);
		await validator.Received(1).ValidateAsync(testModel, Arg.Any<CancellationToken>());
		await next.Received(1).Invoke(context);
	}

	[Fact]
	public async Task InvokeAsync_WhenMultipleMatchingArguments_ValidatesFirstOne()
	{
		// Arrange
		IValidator<TestModel> validator = Substitute.For<IValidator<TestModel>>();
		TestModel firstModel = new() { Name = "First" };
		TestModel secondModel = new() { Name = "Second" };

		validator.ValidateAsync(firstModel, Arg.Any<CancellationToken>())
			.Returns(new ValidationResult());

		FluentValidationFilter<TestModel> filter = new(validator);

		EndpointFilterInvocationContext context = Substitute.For<EndpointFilterInvocationContext>();
		context.Arguments.Returns([firstModel, secondModel]); // Both match, should use first

		DefaultHttpContext httpContext = new();
		context.HttpContext.Returns(httpContext);

		EndpointFilterDelegate next = Substitute.For<EndpointFilterDelegate>();
		object expectedResult = new();
		next.Invoke(context).Returns(expectedResult);

		// Act
		object? result = await filter.InvokeAsync(context, next);

		// Assert
		result.ShouldBe(expectedResult);
		await validator.Received(1).ValidateAsync(firstModel, Arg.Any<CancellationToken>());
		await validator.DidNotReceive().ValidateAsync(secondModel, Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task InvokeAsync_WhenOnlyPrimitiveArguments_CallsNextDelegateWithoutValidation()
	{
		// Arrange
		IValidator<TestModel> validator = Substitute.For<IValidator<TestModel>>();
		FluentValidationFilter<TestModel> filter = new(validator);

		EndpointFilterInvocationContext context = Substitute.For<EndpointFilterInvocationContext>();
		context.Arguments.Returns([42, "string", true, 3.14]);

		DefaultHttpContext httpContext = new();
		context.HttpContext.Returns(httpContext);

		EndpointFilterDelegate next = Substitute.For<EndpointFilterDelegate>();
		object expectedResult = new();
		next.Invoke(context).Returns(expectedResult);

		// Act
		object? result = await filter.InvokeAsync(context, next);

		// Assert
		result.ShouldBe(expectedResult);
		await validator.DidNotReceive().ValidateAsync(Arg.Any<TestModel>(), Arg.Any<CancellationToken>());
		await next.Received(1).Invoke(context);
	}

	[Fact]
	public async Task InvokeAsync_PropagatesCancellationToken()
	{
		// Arrange
		IValidator<TestModel> validator = Substitute.For<IValidator<TestModel>>();
		TestModel testModel = new() { Name = "Test" };

		using CancellationTokenSource cts = new();
		CancellationToken expectedToken = cts.Token;

		validator.ValidateAsync(testModel, expectedToken)
			.Returns(new ValidationResult());

		FluentValidationFilter<TestModel> filter = new(validator);

		EndpointFilterInvocationContext context = Substitute.For<EndpointFilterInvocationContext>();
		context.Arguments.Returns([testModel]);

		DefaultHttpContext httpContext = new()
		{
			RequestAborted = expectedToken
		};
		context.HttpContext.Returns(httpContext);

		EndpointFilterDelegate next = Substitute.For<EndpointFilterDelegate>();
		next.Invoke(context).Returns(new object());

		// Act
		await filter.InvokeAsync(context, next);

		// Assert
		await validator.Received(1).ValidateAsync(testModel, expectedToken);
	}

	[Fact]
	public async Task InvokeAsync_WhenArgumentIsNull_CallsNextDelegate()
	{
		// Arrange
		IValidator<TestModel> validator = Substitute.For<IValidator<TestModel>>();
		FluentValidationFilter<TestModel> filter = new(validator);

		EndpointFilterInvocationContext context = Substitute.For<EndpointFilterInvocationContext>();
		context.Arguments.Returns([null!]);

		DefaultHttpContext httpContext = new();
		context.HttpContext.Returns(httpContext);

		EndpointFilterDelegate next = Substitute.For<EndpointFilterDelegate>();
		object expectedResult = new();
		next.Invoke(context).Returns(expectedResult);

		// Act
		object? result = await filter.InvokeAsync(context, next);

		// Assert
		result.ShouldBe(expectedResult);
		await validator.DidNotReceive().ValidateAsync(Arg.Any<TestModel>(), Arg.Any<CancellationToken>());
		await next.Received(1).Invoke(context);
	}

	[Fact]
	public async Task InvokeAsync_WhenMultipleValidationErrors_ReturnsAllErrors()
	{
		// Arrange
		IValidator<TestModel> validator = Substitute.For<IValidator<TestModel>>();
		TestModel testModel = new() { Name = string.Empty };

		ValidationFailure failure1 = new("Name", "Name is required");
		ValidationFailure failure2 = new("Name", "Name must be at least 3 characters");
		ValidationResult validationResult = new([failure1, failure2]);

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

	[Fact]
	public async Task InvokeAsync_WhenWrongTypeArguments_CallsNextWithoutValidation()
	{
		// Arrange
		IValidator<TestModel> validator = Substitute.For<IValidator<TestModel>>();
		OtherModel otherModel = new() { Id = 1 };

		FluentValidationFilter<TestModel> filter = new(validator);

		EndpointFilterInvocationContext context = Substitute.For<EndpointFilterInvocationContext>();
		context.Arguments.Returns([otherModel]); // Different type than TestModel

		DefaultHttpContext httpContext = new();
		context.HttpContext.Returns(httpContext);

		EndpointFilterDelegate next = Substitute.For<EndpointFilterDelegate>();
		object expectedResult = new();
		next.Invoke(context).Returns(expectedResult);

		// Act
		object? result = await filter.InvokeAsync(context, next);

		// Assert
		result.ShouldBe(expectedResult);
		await validator.DidNotReceive().ValidateAsync(Arg.Any<TestModel>(), Arg.Any<CancellationToken>());
		await next.Received(1).Invoke(context);
	}

	public class TestModel
	{
		public string Name { get; set; } = string.Empty;
	}

	public class OtherModel
	{
		public int Id { get; set; }
	}
}
