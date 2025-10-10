namespace IeuanWalker.MinimalApi.Endpoints.Generator.Tests;

public class DiagnosticSnapshotTests
{
	#region MINAPI001 - HTTP Verb: Missing HTTP verb

	[Fact]
	public Task DiagnosticMINAPI001_NoHttpVerbConfigured_ShouldError()
	{
		// Arrange - This test covers the MINAPI001 diagnostic
		const string source = """
			using IeuanWalker.MinimalApi.Endpoints;
			using Microsoft.AspNetCore.Http.HttpResults;

			namespace TestNamespace;

			public class NoVerbEndpoint : IEndpoint<GetRequest, Ok<GetResponse>>
			{
				public static void Configure(RouteHandlerBuilder builder)
				{
					// No verb configured
				}

				public Task<Ok<GetResponse>> Handle(GetRequest request, CancellationToken ct)
				{
					return Task.FromResult(TypedResults.Ok(new GetResponse(request.Id)));
				}
			}

			public record GetRequest(int Id);
			public record GetResponse(int Id);
		""";

		// Act & Assert
		return TestHelper.Verify(source);
	}

	#endregion

	#region MINAPI002 - HTTP Verb: Multiple HTTP verbs configured

	[Fact]
	public Task DiagnosticMINAPI002_MultipleHttpVerbsConfigured_ShouldError()
	{
		// Arrange - This test covers the MINAPI002 diagnostic
		const string source = """
			using IeuanWalker.MinimalApi.Endpoints;
			using Microsoft.AspNetCore.Http.HttpResults;

			namespace TestNamespace;

			public class MultiVerbEndpoint : IEndpoint<CreateRequest, Ok<CreateResponse>>
			{
				public static void Configure(RouteHandlerBuilder builder)
				{
					builder.Get("/api/items");
					builder.Post("/api/items");
				}

				public Task<Ok<CreateResponse>> Handle(CreateRequest request, CancellationToken ct)
				{
					return Task.FromResult(TypedResults.Ok(new CreateResponse(1)));
				}
			}

			public record CreateRequest(string Name);
			public record CreateResponse(int Id);
		""";

		// Act & Assert
		return TestHelper.Verify(source);
	}

	#endregion

	#region MINAPI003 - Map group: No MapGroup configured

	[Fact]
	public Task DiagnosticMINAPI003_GroupConfigureMissingMapGroup_ShouldError()
	{
		// Arrange - This test covers the MINAPI003 diagnostic
		const string source = """
			using IeuanWalker.MinimalApi.Endpoints;
			using Microsoft.AspNetCore.Builder;
			using Microsoft.AspNetCore.Http.HttpResults;

			namespace TestNamespace;

			public class GrouplessEndpoint : IEndpoint<GetRequest, Ok<GetResponse>>
			{
				public static void Configure(RouteHandlerBuilder builder)
				{
					builder.Group<UserEndpointGroup>();
					builder.Get("/api/users/{id}");
				}

				public Task<Ok<GetResponse>> Handle(GetRequest request, CancellationToken ct)
				{
					return Task.FromResult(TypedResults.Ok(new GetResponse(request.Id)));
				}
			}

			public record GetRequest(int Id);
			public record GetResponse(int Id);

			public class UserEndpointGroup : IEndpointGroup
			{
				public static RouteGroupBuilder Configure(WebApplication app)
				{
					// Intentionally not calling MapGroup to trigger diagnostic
					throw new System.NotImplementedException();
				}
			}
		""";

		// Act & Assert
		return TestHelper.Verify(source);
	}

	#endregion

	#region MINAPI004 - Map group: Multiple MapGroup calls

	[Fact]
	public Task DiagnosticMINAPI004_MultipleMapGroupCallsInGroupConfigure_ShouldError()
	{
		// Arrange - This test covers the MINAPI004 diagnostic
		const string source = """
			using IeuanWalker.MinimalApi.Endpoints;
			using Microsoft.AspNetCore.Builder;
			using Microsoft.AspNetCore.Http.HttpResults;

			namespace TestNamespace;

			public class DuplicateMapGroupEndpoint : IEndpoint<GetRequest, Ok<GetResponse>>
			{
				public static void Configure(RouteHandlerBuilder builder)
				{
					builder.Group<DuplicatedGroup>();
					builder.Get("/api/dup/{id}");
				}

				public Task<Ok<GetResponse>> Handle(GetRequest request, CancellationToken ct)
				{
					return Task.FromResult(TypedResults.Ok(new GetResponse(request.Id)));
				}
			}

			public record GetRequest(int Id);
			public record GetResponse(int Id);

			public class DuplicatedGroup : IEndpointGroup
			{
				public static RouteGroupBuilder Configure(WebApplication app)
				{
					app.MapGroup("/a");
					app.MapGroup("/b");
					return app.MapGroup("/a");
				}
			}
		""";

		// Act & Assert
		return TestHelper.Verify(source);
	}

	#endregion

	#region MINAPI005 - Map group: Multiple Group calls in Configure

	[Fact]
	public Task DiagnosticMINAPI005_MultipleGroupCallsInConfigure_ShouldError()
	{
		// Arrange - This test covers the MINAPI005 diagnostic
		const string source = """
			using IeuanWalker.MinimalApi.Endpoints;
			using Microsoft.AspNetCore.Http.HttpResults;

			namespace TestNamespace;

			public class MultipleGroupCallsEndpoint : IEndpoint<GetRequest, Ok<GetResponse>>
			{
				public static void Configure(RouteHandlerBuilder builder)
				{
					// Call Group twice on the same builder
					builder.Group<UserEndpointGroup>();
					builder.Group<UserEndpointGroup>();
					builder.Get("/api/multi/{id}");
				}

				public Task<Ok<GetResponse>> Handle(GetRequest request, CancellationToken ct)
				{
					return Task.FromResult(TypedResults.Ok(new GetResponse(request.Id)));
				}
			}

			public record GetRequest(int Id);
			public record GetResponse(int Id);

			public class UserEndpointGroup : IEndpointGroup
			{
				public static RouteGroupBuilder Configure(WebApplication app)
				{
					return app.MapGroup("/api/users");
				}
			}
		""";

		// Act & Assert
		return TestHelper.Verify(source);
	}

	#endregion

	#region MINAPI006 - Map group: Unused endpoint group

	[Fact]
	public Task DiagnosticMINAPI006_UnusedEndpointGroup_ShouldWarn()
	{
		// Arrange - This test covers the MINAPI006 diagnostic
		const string source = """
			using IeuanWalker.MinimalApi.Endpoints;
			using Microsoft.AspNetCore.Builder;
			using Microsoft.AspNetCore.Http.HttpResults;

			namespace TestNamespace;

			public class SimpleEndpoint : IEndpoint<GetRequest, Ok<GetResponse>>
			{
				public static void Configure(RouteHandlerBuilder builder)
				{
					builder.Get("/api/simple/{id}");
				}

				public Task<Ok<GetResponse>> Handle(GetRequest request, CancellationToken ct)
				{
					return Task.FromResult(TypedResults.Ok(new GetResponse(request.Id)));
				}
			}

			public record GetRequest(int Id);
			public record GetResponse(int Id);

			// This group is defined but not used - should trigger MINAPI006 warning
			public class UnusedGroup : IEndpointGroup
			{
				public static RouteGroupBuilder Configure(WebApplication app)
				{
					return app.MapGroup("/unused");
				}
			}
		""";

		// Act & Assert
		return TestHelper.Verify(source);
	}

	#endregion

	#region MINAPI007 - Validation: Multiple validators for same type

	[Fact]
	public Task DiagnosticMINAPI007_MultipleValidatorsForSameType_ShouldError()
	{
		// Arrange - This test covers the MINAPI007 diagnostic
		const string source = """
			using IeuanWalker.MinimalApi.Endpoints;
			using FluentValidation;
			using Microsoft.AspNetCore.Http.HttpResults;

			namespace TestNamespace;

			public class ValidatorEndpoint : IEndpoint<CreateRequest, Ok<CreateResponse>>
			{
				public static void Configure(RouteHandlerBuilder builder)
				{
					builder.Post("/api/val");
				}

				public Task<Ok<CreateResponse>> Handle(CreateRequest request, CancellationToken ct)
				{
					return Task.FromResult(TypedResults.Ok(new CreateResponse(1)));
				}
			}

			public record CreateRequest(string Name);
			public record CreateResponse(int Id);

			public class CreateRequestValidatorA : Validator<CreateRequest>
			{
				public CreateRequestValidatorA()
				{
					RuleFor(x => x.Name).NotEmpty();
				}
			}

			public class CreateRequestValidatorB : Validator<CreateRequest>
			{
				public CreateRequestValidatorB()
				{
					RuleFor(x => x.Name).MinimumLength(3);
				}
			}
		""";

		// Act & Assert
		return TestHelper.Verify(source);
	}

	#endregion

	#region MINAPI008 - Validation: Validator present but validation disabled

	[Fact]
	public Task DiagnosticMINAPI008_ValidatorButValidationDisabled_ShouldWarn()
	{
		// Arrange - This test covers the MINAPI008 diagnostic
		const string source = """
			using IeuanWalker.MinimalApi.Endpoints;
			using FluentValidation;
			using Microsoft.AspNetCore.Http.HttpResults;

			namespace TestNamespace;

			public class DisabledValidationEndpoint : IEndpoint<CreateRequest, Ok<CreateResponse>>
			{
				public static void Configure(RouteHandlerBuilder builder)
				{
					builder.Post("/api/novalid").DisableValidation();
				}

				public Task<Ok<CreateResponse>> Handle(CreateRequest request, CancellationToken ct)
				{
					return Task.FromResult(TypedResults.Ok(new CreateResponse(1)));
				}
			}

			public record CreateRequest(string Name);
			public record CreateResponse(int Id);

			public class CreateRequestValidator : Validator<CreateRequest>
			{
				public CreateRequestValidator()
				{
					RuleFor(x => x.Name).NotEmpty();
				}
			}
		""";

		// Act & Assert
		return TestHelper.Verify(source);
	}

	#endregion

	#region MINAPI009 - Request type: Multiple request types / Abstract Validator

	[Fact]
	public Task DiagnosticMINAPI009_AbstractValidatorOnRequest_ShouldWarn()
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
	public Task DiagnosticMINAPI009_MultipleAbstractValidatorsOnRequestTypes_ShouldWarnMultiple()
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
	public Task DiagnosticMINAPI009_AbstractValidatorNotMatchingRequestType_ShouldNotWarn()
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
	public Task DiagnosticMINAPI009_MixedValidatorTypes_ShouldOnlyWarnForAbstractValidators()
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
	public Task DiagnosticMINAPI009_AbstractValidatorInheritanceChain_ShouldWarn()
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
