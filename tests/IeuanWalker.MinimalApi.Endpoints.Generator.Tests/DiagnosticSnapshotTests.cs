namespace IeuanWalker.MinimalApi.Endpoints.Generator.Tests;

public class DiagnosticSnapshotTests
{
	#region MINAPI009 - Abstract Validator Tests

	[Fact]
	public Task GeneratesEndpointExtensions_WithAbstractValidatorOnRequestType_ShouldHaveWarning()
	{
		// Arrange - This test covers the MINAPI009 diagnostic
		const string source = """
			using IeuanWalker.MinimalApi.Endpoints;
			using FluentValidation;
			using Microsoft.AspNetCore.Http.HttpResults;

			namespace TestNamespace;

			public class CreateUserEndpoint : IEndpoint<CreateUserRequest, Ok<UserResponse>>
			{
				public static void Configure(RouteHandlerBuilder builder)
				{
					builder.Post("/api/users");
				}

				public Task<Ok<UserResponse>> Handle(CreateUserRequest request, CancellationToken ct)
				{
					return Task.FromResult(TypedResults.Ok(new UserResponse(1, request.Name)));
				}
			}

			public record CreateUserRequest(string Name, string Email);
			public record UserResponse(int Id, string Name);

			// This should trigger MINAPI009 warning - using AbstractValidator directly instead of Validator<T>
			public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
			{
				public CreateUserRequestValidator()
				{
					RuleFor(x => x.Name).NotEmpty();
					RuleFor(x => x.Email).EmailAddress();
				}
			}
			""";

		// Act & Assert
		return TestHelper.Verify(source);
	}

	[Fact]
	public Task GeneratesEndpointExtensions_WithMultipleAbstractValidatorsOnRequestTypes_ShouldHaveMultipleWarnings()
	{
		// Arrange - Test multiple abstract validators matching request types
		const string source = """
			using IeuanWalker.MinimalApi.Endpoints;
			using FluentValidation;
			using Microsoft.AspNetCore.Http.HttpResults;

			namespace TestNamespace;

			public class CreateUserEndpoint : IEndpoint<CreateUserRequest, Ok<UserResponse>>
			{
				public static void Configure(RouteHandlerBuilder builder)
				{
					builder.Post("/api/users");
				}

				public Task<Ok<UserResponse>> Handle(CreateUserRequest request, CancellationToken ct)
				{
					return Task.FromResult(TypedResults.Ok(new UserResponse(1, request.Name)));
				}
			}

			public class UpdateUserEndpoint : IEndpoint<UpdateUserRequest, Ok<UserResponse>>
			{
				public static void Configure(RouteHandlerBuilder builder)
				{
					builder.Put("/api/users/{id}");
				}

				public Task<Ok<UserResponse>> Handle(UpdateUserRequest request, CancellationToken ct)
				{
					return Task.FromResult(TypedResults.Ok(new UserResponse(request.Id, request.Name)));
				}
			}

			public record CreateUserRequest(string Name, string Email);
			public record UpdateUserRequest(int Id, string Name, string Email);
			public record UserResponse(int Id, string Name);

			// Both of these should trigger MINAPI009 warnings
			public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
			{
				public CreateUserRequestValidator()
				{
					RuleFor(x => x.Name).NotEmpty();
					RuleFor(x => x.Email).EmailAddress();
				}
			}

			public class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
			{
				public UpdateUserRequestValidator()
				{
					RuleFor(x => x.Id).GreaterThan(0);
					RuleFor(x => x.Name).NotEmpty();
					RuleFor(x => x.Email).EmailAddress();
				}
			}
			""";

		// Act & Assert
		return TestHelper.Verify(source);
	}

	[Fact]
	public Task GeneratesEndpointExtensions_WithAbstractValidatorNotMatchingRequestType_ShouldHaveNoWarning()
	{
		// Arrange - Abstract validator for a type that's not used as a request type
		const string source = """
			using IeuanWalker.MinimalApi.Endpoints;
			using FluentValidation;
			using Microsoft.AspNetCore.Http.HttpResults;

			namespace TestNamespace;

			public class GetUserEndpoint : IEndpoint<GetUserRequest, Ok<UserResponse>>
			{
				public static void Configure(RouteHandlerBuilder builder)
				{
					builder.Get("/api/users/{id}");
				}

				public Task<Ok<UserResponse>> Handle(GetUserRequest request, CancellationToken ct)
				{
					return Task.FromResult(TypedResults.Ok(new UserResponse(request.Id, "John")));
				}
			}

			public record GetUserRequest(int Id);
			public record UserResponse(int Id, string Name);
			public record UnusedModel(string Value);

			// This should NOT trigger MINAPI009 warning - not used as request type
			public class UnusedModelValidator : AbstractValidator<UnusedModel>
			{
				public UnusedModelValidator()
				{
					RuleFor(x => x.Value).NotEmpty();
				}
			}
			""";

		// Act & Assert
		return TestHelper.Verify(source);
	}

	[Fact]
	public Task GeneratesEndpointExtensions_WithMixedValidatorTypes_ShouldOnlyWarnForAbstractValidators()
	{
		// Arrange - Mix of proper Validator<T> and AbstractValidator<T>
		const string source = """
			using IeuanWalker.MinimalApi.Endpoints;
			using FluentValidation;
			using Microsoft.AspNetCore.Http.HttpResults;

			namespace TestNamespace;

			public class CreateUserEndpoint : IEndpoint<CreateUserRequest, Ok<UserResponse>>
			{
				public static void Configure(RouteHandlerBuilder builder)
				{
					builder.Post("/api/users");
				}

				public Task<Ok<UserResponse>> Handle(CreateUserRequest request, CancellationToken ct)
				{
					return Task.FromResult(TypedResults.Ok(new UserResponse(1, request.Name)));
				}
			}

			public class UpdateUserEndpoint : IEndpoint<UpdateUserRequest, Ok<UserResponse>>
			{
				public static void Configure(RouteHandlerBuilder builder)
				{
					builder.Put("/api/users/{id}");
				}

				public Task<Ok<UserResponse>> Handle(UpdateUserRequest request, CancellationToken ct)
				{
					return Task.FromResult(TypedResults.Ok(new UserResponse(request.Id, request.Name)));
				}
			}

			public record CreateUserRequest(string Name, string Email);
			public record UpdateUserRequest(int Id, string Name, string Email);
			public record UserResponse(int Id, string Name);

			// This should be properly registered - no warning
			public class CreateUserRequestValidator : Validator<CreateUserRequest>
			{
				public CreateUserRequestValidator()
				{
					RuleFor(x => x.Name).NotEmpty();
					RuleFor(x => x.Email).EmailAddress();
				}
			}

			// This should trigger MINAPI009 warning - using AbstractValidator
			public class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
			{
				public UpdateUserRequestValidator()
				{
					RuleFor(x => x.Id).GreaterThan(0);
					RuleFor(x => x.Name).NotEmpty();
					RuleFor(x => x.Email).EmailAddress();
				}
			}
			""";

		// Act & Assert
		return TestHelper.Verify(source);
	}

	[Fact]
	public Task GeneratesEndpointExtensions_WithAbstractValidatorInheritanceChain_ShouldHaveWarning()
	{
		// Arrange - Abstract validator with inheritance chain
		const string source = """
			using IeuanWalker.MinimalApi.Endpoints;
			using FluentValidation;
			using Microsoft.AspNetCore.Http.HttpResults;

			namespace TestNamespace;

			public class CreateUserEndpoint : IEndpoint<CreateUserRequest, Ok<UserResponse>>
			{
				public static void Configure(RouteHandlerBuilder builder)
				{
					builder.Post("/api/users");
				}

				public Task<Ok<UserResponse>> Handle(CreateUserRequest request, CancellationToken ct)
				{
					return Task.FromResult(TypedResults.Ok(new UserResponse(1, request.Name)));
				}
			}

			public record CreateUserRequest(string Name, string Email);
			public record UserResponse(int Id, string Name);

			// Base abstract validator
			public abstract class BaseRequestValidator<T> : AbstractValidator<T>
			{
				protected BaseRequestValidator()
				{
					// Common validation rules
				}
			}

			// This should trigger MINAPI009 warning - inherits from AbstractValidator through inheritance chain
			public class CreateUserRequestValidator : BaseRequestValidator<CreateUserRequest>
			{
				public CreateUserRequestValidator()
				{
					RuleFor(x => x.Name).NotEmpty();
					RuleFor(x => x.Email).EmailAddress();
				}
			}
			""";

		// Act & Assert
		return TestHelper.Verify(source);
	}

	#endregion
}
