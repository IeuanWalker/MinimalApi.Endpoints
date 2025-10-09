namespace IeuanWalker.MinimalApi.Endpoints.Generator.Tests;

public class SnapshotTests
{
	[Fact]
	public Task GeneratesEndpointExtensions_ForSimpleEndpoint()
	{
		// Arrange
		const string source = """
			using IeuanWalker.MinimalApi.Endpoints;
			using Microsoft.AspNetCore.Http.HttpResults;
			
			namespace TestNamespace;
			
			public class GetUsersEndpoint : IEndpoint<GetUsersRequest, Ok<GetUsersResponse>>
			{
				public static void Configure(RouteHandlerBuilder builder)
				{
					builder.Get("/api/users");
				}
				
				public Task<Ok<GetUsersResponse>> Handle(GetUsersRequest request, CancellationToken ct)
				{
					return Task.FromResult(TypedResults.Ok(new GetUsersResponse()));
				}
			}
			
			public record GetUsersRequest();
			public record GetUsersResponse();
			""";

		// Act & Assert
		return TestHelper.Verify(source);
	}

	[Fact]
	public Task GeneratesEndpointExtensions_ForEndpointWithoutRequest()
	{
		// Arrange
		const string source = """
				using IeuanWalker.MinimalApi.Endpoints;
				using Microsoft.AspNetCore.Http.HttpResults;

				namespace TestNamespace;

				public class GetAllUsersEndpoint : IEndpointWithoutRequest<Ok<List<User>>>
				{
					public static void Configure(RouteHandlerBuilder builder)
					{
						builder.Get("/api/users/all");
					}

					public Task<Ok<List<User>>> Handle(CancellationToken ct)
					{
						return Task.FromResult(TypedResults.Ok(new List<User>()));
					}
				}

				public record User(int Id, string Name);
				""";

		// Act & Assert
		return TestHelper.Verify(source);
	}

	[Fact]
	public Task GeneratesEndpointExtensions_ForEndpointWithoutResponse()
	{
		// Arrange
		const string source = """
				using IeuanWalker.MinimalApi.Endpoints;

				namespace TestNamespace;

				public class DeleteUserEndpoint : IEndpointWithoutResponse<DeleteUserRequest>
				{
					public static void Configure(RouteHandlerBuilder builder)
					{
						builder.Delete("/api/users/{id}");
					}

					public Task Handle(DeleteUserRequest request, CancellationToken ct)
					{
						return Task.CompletedTask;
					}
				}

				public record DeleteUserRequest(int Id);
				""";

		// Act & Assert
		return TestHelper.Verify(source);
	}

	[Fact]
	public Task GeneratesEndpointExtensions_ForEndpointWithoutRequestOrResponse()
	{
		// Arrange
		const string source = """
				using IeuanWalker.MinimalApi.Endpoints;

				namespace TestNamespace;

				public class HealthCheckEndpoint : IEndpoint
				{
					public static void Configure(RouteHandlerBuilder builder)
					{
						builder.Get("/health");
					}

					public Task Handle(CancellationToken ct)
					{
						return Task.CompletedTask;
					}
				}
				""";

		// Act & Assert
		return TestHelper.Verify(source);
	}

	[Fact]
	public Task GeneratesEndpointExtensions_WithEndpointGroup()
	{
		// Arrange
		const string source = """
				using IeuanWalker.MinimalApi.Endpoints;
				using Microsoft.AspNetCore.Http.HttpResults;

				namespace TestNamespace;

				public class UserEndpointGroup : IEndpointGroup
				{
					public static RouteGroupBuilder Configure(WebApplication app)
					{
						return app.MapGroup("/api/v1/users");
					}
				}

				public class GetUserByIdEndpoint : IEndpoint<GetUserRequest, Ok<UserResponse>>
				{
					public static void Configure(RouteHandlerBuilder builder)
					{
						builder
							.Group<UserEndpointGroup>()
							.Get("/{id}");
					}

					public Task<Ok<UserResponse>> Handle(GetUserRequest request, CancellationToken ct)
					{
						return Task.FromResult(TypedResults.Ok(new UserResponse(request.Id, "John")));
					}
				}

				public record GetUserRequest(int Id);
				public record UserResponse(int Id, string Name);
				""";

		// Act & Assert
		return TestHelper.Verify(source);
	}

	[Fact]
	public Task GeneratesEndpointExtensions_WithValidator()
	{
		// Arrange
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

				public record CreateUserRequest(string Name);
				public record UserResponse(int Id, string Name);

				public class CreateUserRequestValidator : Validator<CreateUserRequest>
				{
					public CreateUserRequestValidator()
					{
						RuleFor(x => x.Name).NotEmpty();
					}
				}
				""";

		// Act & Assert
		return TestHelper.Verify(source);
	}

	[Fact]
	public Task GeneratesEndpointExtensions_WithMultipleEndpoints()
	{
		// Arrange
		const string source = """
				using IeuanWalker.MinimalApi.Endpoints;
				using Microsoft.AspNetCore.Http.HttpResults;

				namespace TestNamespace;

				public class GetUsersEndpoint : IEndpoint<GetUsersRequest, Ok<List<User>>>
				{
					public static void Configure(RouteHandlerBuilder builder)
					{
						builder.Get("/api/users");
					}

					public Task<Ok<List<User>>> Handle(GetUsersRequest request, CancellationToken ct)
					{
						return Task.FromResult(TypedResults.Ok(new List<User>()));
					}
				}

				public class CreateUserEndpoint : IEndpoint<CreateUserRequest, Ok<User>>
				{
					public static void Configure(RouteHandlerBuilder builder)
					{
						builder.Post("/api/users");
					}

					public Task<Ok<User>> Handle(CreateUserRequest request, CancellationToken ct)
					{
						return Task.FromResult(TypedResults.Ok(new User(1, request.Name)));
					}
				}

				public class DeleteUserEndpoint : IEndpointWithoutResponse<DeleteUserRequest>
				{
					public static void Configure(RouteHandlerBuilder builder)
					{
						builder.Delete("/api/users/{id}");
					}

					public Task Handle(DeleteUserRequest request, CancellationToken ct)
					{
						return Task.CompletedTask;
					}
				}

				public record GetUsersRequest();
				public record CreateUserRequest(string Name);
				public record DeleteUserRequest(int Id);
				public record User(int Id, string Name);
				""";

		// Act & Assert
		return TestHelper.Verify(source);
	}

	[Fact]
	public Task GeneratesEndpointExtensions_WithComplexGroupHierarchy()
	{
		// Arrange
		const string source = """
				using IeuanWalker.MinimalApi.Endpoints;
				using Microsoft.AspNetCore.Http.HttpResults;

				namespace TestNamespace;

				public class ApiEndpointGroup : IEndpointGroup
				{
					public static RouteGroupBuilder Configure(WebApplication app)
					{
						return app.MapGroup("/api");
					}
				}

				public class UsersEndpointGroup : IEndpointGroup
				{
					public static RouteGroupBuilder Configure(WebApplication app)
					{
						return app.MapGroup("/users");
					}
				}

				public class GetUserEndpoint : IEndpoint<GetUserRequest, Ok<UserResponse>>
				{
					public static void Configure(RouteHandlerBuilder builder)
					{
						builder
							.Group<ApiEndpointGroup>()
							.Get("/{id}");
					}

					public Task<Ok<UserResponse>> Handle(GetUserRequest request, CancellationToken ct)
					{
						return Task.FromResult(TypedResults.Ok(new UserResponse(request.Id, "John")));
					}
				}

				public class GetAllUsersEndpoint : IEndpointWithoutRequest<Ok<List<UserResponse>>>
				{
					public static void Configure(RouteHandlerBuilder builder)
					{
						builder
							.Group<UsersEndpointGroup>()
							.Get("/");
					}

					public Task<Ok<List<UserResponse>>> Handle(CancellationToken ct)
					{
						return Task.FromResult(TypedResults.Ok(new List<UserResponse>()));
					}
				}

				public record GetUserRequest(int Id);
				public record UserResponse(int Id, string Name);
				""";

		// Act & Assert
		return TestHelper.Verify(source);
	}

	[Fact]
	public Task GeneratesEndpointExtensions_WithFromBodyBinding()
	{
		// Arrange
		const string source = """
				using IeuanWalker.MinimalApi.Endpoints;
				using Microsoft.AspNetCore.Http.HttpResults;

				namespace TestNamespace;

				public class CreateUserEndpoint : IEndpoint<CreateUserRequest, Ok<UserResponse>>
				{
					public static void Configure(RouteHandlerBuilder builder)
					{
						builder
							.Post("/api/users")
							.RequestFromBody();
					}

					public Task<Ok<UserResponse>> Handle(CreateUserRequest request, CancellationToken ct)
					{
						return Task.FromResult(TypedResults.Ok(new UserResponse(1, request.Name)));
					}
				}

				public record CreateUserRequest(string Name, string Email);
				public record UserResponse(int Id, string Name);
				""";

		// Act & Assert
		return TestHelper.Verify(source);
	}

	[Fact]
	public Task GeneratesEndpointExtensions_WithAsParametersBinding()
	{
		// Arrange
		const string source = """
				using IeuanWalker.MinimalApi.Endpoints;
				using Microsoft.AspNetCore.Http.HttpResults;

				namespace TestNamespace;

				public class SearchUsersEndpoint : IEndpoint<SearchUsersRequest, Ok<List<UserResponse>>>
				{
					public static void Configure(RouteHandlerBuilder builder)
					{
						builder
							.Get("/api/users/search")
							.RequestAsParameters();
					}

					public Task<Ok<List<UserResponse>>> Handle(SearchUsersRequest request, CancellationToken ct)
					{
						return Task.FromResult(TypedResults.Ok(new List<UserResponse>()));
					}
				}

				public record SearchUsersRequest(string Query, int Page, int PageSize);
				public record UserResponse(int Id, string Name);
				""";

		// Act & Assert
		return TestHelper.Verify(source);
	}

	[Fact]
	public Task GeneratesEndpointExtensions_WithFromRouteBinding()
	{
		// Arrange
		const string source = """
				using IeuanWalker.MinimalApi.Endpoints;
				using Microsoft.AspNetCore.Http.HttpResults;

				namespace TestNamespace;

				public class GetUserEndpoint : IEndpoint<GetUserRequest, Ok<UserResponse>>
				{
					public static void Configure(RouteHandlerBuilder builder)
					{
						builder
							.Get("/api/users/{id}")
							.RequestFromRoute();
					}

					public Task<Ok<UserResponse>> Handle(GetUserRequest request, CancellationToken ct)
					{
						return Task.FromResult(TypedResults.Ok(new UserResponse(request.Id, "John")));
					}
				}

				public record GetUserRequest(int Id);
				public record UserResponse(int Id, string Name);
				""";

		// Act & Assert
		return TestHelper.Verify(source);
	}

	[Fact]
	public Task GeneratesEndpointExtensions_WithFromHeaderBinding()
	{
		// Arrange
		const string source = """
				using IeuanWalker.MinimalApi.Endpoints;
				using Microsoft.AspNetCore.Http.HttpResults;

				namespace TestNamespace;

				public class GetUserEndpoint : IEndpoint<GetUserRequest, Ok<UserResponse>>
				{
					public static void Configure(RouteHandlerBuilder builder)
					{
						builder
							.Get("/api/users/current")
							.RequestFromHeader();
					}

					public Task<Ok<UserResponse>> Handle(GetUserRequest request, CancellationToken ct)
					{
						return Task.FromResult(TypedResults.Ok(new UserResponse(1, request.UserId)));
					}
				}

				public record GetUserRequest(string UserId);
				public record UserResponse(int Id, string Name);
				""";

		// Act & Assert
		return TestHelper.Verify(source);
	}

	[Fact]
	public Task GeneratesEndpointExtensions_WithFromHeaderBindingWithName()
	{
		// Arrange
		const string source = """
				using IeuanWalker.MinimalApi.Endpoints;
				using Microsoft.AspNetCore.Http.HttpResults;

				namespace TestNamespace;

				public class GetUserEndpoint : IEndpoint<GetUserRequest, Ok<UserResponse>>
				{
					public static void Configure(RouteHandlerBuilder builder)
					{
						builder
							.Get("/api/users/current")
							.RequestFromHeader("X-User-Id");
					}

					public Task<Ok<UserResponse>> Handle(GetUserRequest request, CancellationToken ct)
					{
						return Task.FromResult(TypedResults.Ok(new UserResponse(1, request.UserId)));
					}
				}

				public record GetUserRequest(string UserId);
				public record UserResponse(int Id, string Name);
				""";

		// Act & Assert
		return TestHelper.Verify(source);
	}

	[Fact]
	public Task GeneratesEndpointExtensions_WithFromQueryBinding()
	{
		// Arrange
		const string source = """
				using IeuanWalker.MinimalApi.Endpoints;
				using Microsoft.AspNetCore.Http.HttpResults;

				namespace TestNamespace;

				public class SearchUsersEndpoint : IEndpoint<SearchUsersRequest, Ok<List<UserResponse>>>
				{
					public static void Configure(RouteHandlerBuilder builder)
					{
						builder
							.Get("/api/users")
							.RequestFromQuery();
					}

					public Task<Ok<List<UserResponse>>> Handle(SearchUsersRequest request, CancellationToken ct)
					{
						return Task.FromResult(TypedResults.Ok(new List<UserResponse>()));
					}
				}

				public record SearchUsersRequest(string Query);
				public record UserResponse(int Id, string Name);
				""";

		// Act & Assert
		return TestHelper.Verify(source);
	}

	[Fact]
	public Task GeneratesEndpointExtensions_WithFromQueryBindingWithName()
	{
		// Arrange
		const string source = """
				using IeuanWalker.MinimalApi.Endpoints;
				using Microsoft.AspNetCore.Http.HttpResults;

				namespace TestNamespace;

				public class SearchUsersEndpoint : IEndpoint<SearchUsersRequest, Ok<List<UserResponse>>>
				{
					public static void Configure(RouteHandlerBuilder builder)
					{
						builder
							.Get("/api/users")
							.RequestFromQuery("searchTerm");
					}

					public Task<Ok<List<UserResponse>>> Handle(SearchUsersRequest request, CancellationToken ct)
					{
						return Task.FromResult(TypedResults.Ok(new List<UserResponse>()));
					}
				}

				public record SearchUsersRequest(string Query);
				public record UserResponse(int Id, string Name);
				""";

		// Act & Assert
		return TestHelper.Verify(source);
	}

	[Fact]
	public Task GeneratesEndpointExtensions_WithFromFormBinding()
	{
		// Arrange
		const string source = """
				using IeuanWalker.MinimalApi.Endpoints;
				using Microsoft.AspNetCore.Http.HttpResults;

				namespace TestNamespace;

				public class UploadFileEndpoint : IEndpoint<UploadFileRequest, Ok<UploadFileResponse>>
				{
					public static void Configure(RouteHandlerBuilder builder)
					{
						builder
							.Post("/api/files")
							.RequestFromForm();
					}

					public Task<Ok<UploadFileResponse>> Handle(UploadFileRequest request, CancellationToken ct)
					{
						return Task.FromResult(TypedResults.Ok(new UploadFileResponse(request.FileName)));
					}
				}

				public record UploadFileRequest(string FileName, string Description);
				public record UploadFileResponse(string FileName);
				""";

		// Act & Assert
		return TestHelper.Verify(source);
	}

	[Fact]
	public Task GeneratesEndpointExtensions_WithExplicitWithName()
	{
		// Arrange
		const string source = """
				using IeuanWalker.MinimalApi.Endpoints;
				using Microsoft.AspNetCore.Http.HttpResults;

				namespace TestNamespace;

				public class GetUserEndpoint : IEndpoint<GetUserRequest, Ok<UserResponse>>
				{
					public static void Configure(RouteHandlerBuilder builder)
					{
						builder
							.Get("/api/users/{id}")
							.WithName("GetUserById");
					}

					public Task<Ok<UserResponse>> Handle(GetUserRequest request, CancellationToken ct)
					{
						return Task.FromResult(TypedResults.Ok(new UserResponse(request.Id, "John")));
					}
				}

				public record GetUserRequest(int Id);
				public record UserResponse(int Id, string Name);
				""";

		// Act & Assert
		return TestHelper.Verify(source);
	}

	[Fact]
	public Task GeneratesEndpointExtensions_WithExplicitWithTags()
	{
		// Arrange
		const string source = """
				using IeuanWalker.MinimalApi.Endpoints;
				using Microsoft.AspNetCore.Http.HttpResults;

				namespace TestNamespace;

				public class GetUserEndpoint : IEndpoint<GetUserRequest, Ok<UserResponse>>
				{
					public static void Configure(RouteHandlerBuilder builder)
					{
						builder
							.Get("/api/users/{id}")
							.WithTags("UserManagement");
					}

					public Task<Ok<UserResponse>> Handle(GetUserRequest request, CancellationToken ct)
					{
						return Task.FromResult(TypedResults.Ok(new UserResponse(request.Id, "John")));
					}
				}

				public record GetUserRequest(int Id);
				public record UserResponse(int Id, string Name);
				""";

		// Act & Assert
		return TestHelper.Verify(source);
	}

	[Fact]
	public Task GeneratesEndpointExtensions_WithGroupHavingWithName()
	{
		// Arrange
		const string source = """
				using IeuanWalker.MinimalApi.Endpoints;
				using Microsoft.AspNetCore.Http.HttpResults;

				namespace TestNamespace;

				public class UserEndpointGroup : IEndpointGroup
				{
					public static RouteGroupBuilder Configure(WebApplication app)
					{
						return app.MapGroup("/api/users")
							.WithName("UserGroup");
					}
				}

				public class GetUserEndpoint : IEndpoint<GetUserRequest, Ok<UserResponse>>
				{
					public static void Configure(RouteHandlerBuilder builder)
					{
						builder
							.Group<UserEndpointGroup>()
							.Get("/{id}");
					}

					public Task<Ok<UserResponse>> Handle(GetUserRequest request, CancellationToken ct)
					{
						return Task.FromResult(TypedResults.Ok(new UserResponse(request.Id, "John")));
					}
				}

				public record GetUserRequest(int Id);
				public record UserResponse(int Id, string Name);
				""";

		// Act & Assert
		return TestHelper.Verify(source);
	}

	[Fact]
	public Task GeneratesEndpointExtensions_WithGroupHavingWithTags()
	{
		// Arrange
		const string source = """
				using IeuanWalker.MinimalApi.Endpoints;
				using Microsoft.AspNetCore.Http.HttpResults;

				namespace TestNamespace;

				public class UserEndpointGroup : IEndpointGroup
				{
					public static RouteGroupBuilder Configure(WebApplication app)
					{
						return app.MapGroup("/api/users")
							.WithTags("Users");
					}
				}

				public class GetUserEndpoint : IEndpoint<GetUserRequest, Ok<UserResponse>>
				{
					public static void Configure(RouteHandlerBuilder builder)
					{
						builder
							.Group<UserEndpointGroup>()
							.Get("/{id}");
					}

					public Task<Ok<UserResponse>> Handle(GetUserRequest request, CancellationToken ct)
					{
						return Task.FromResult(TypedResults.Ok(new UserResponse(request.Id, "John")));
					}
				}

				public record GetUserRequest(int Id);
				public record UserResponse(int Id, string Name);
				""";

		// Act & Assert
		return TestHelper.Verify(source);
	}

	[Fact]
	public Task GeneratesEndpointExtensions_WithDisableValidation()
	{
		// Arrange
		const string source = """
				using IeuanWalker.MinimalApi.Endpoints;
				using FluentValidation;
				using Microsoft.AspNetCore.Http.HttpResults;

				namespace TestNamespace;

				public class CreateUserEndpoint : IEndpoint<CreateUserRequest, Ok<UserResponse>>
				{
					public static void Configure(RouteHandlerBuilder builder)
					{
						builder
							.Post("/api/users")
							.DisableValidation();
					}

					public Task<Ok<UserResponse>> Handle(CreateUserRequest request, CancellationToken ct)
					{
						return Task.FromResult(TypedResults.Ok(new UserResponse(1, request.Name)));
					}
				}

				public record CreateUserRequest(string Name);
				public record UserResponse(int Id, string Name);

				public class CreateUserRequestValidator : Validator<CreateUserRequest>
				{
					public CreateUserRequestValidator()
					{
						RuleFor(x => x.Name).NotEmpty();
					}
				}
				""";

		// Act & Assert
		return TestHelper.Verify(source);
	}

	[Fact]
	public Task GeneratesEndpointExtensions_WithNonRequestModelValidator()
	{
		// Arrange
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
				public record NestedModel(string Value);

				public class NestedModelValidator : Validator<NestedModel>
				{
					public NestedModelValidator()
					{
						RuleFor(x => x.Value).NotEmpty();
					}
				}
				""";

		// Act & Assert
		return TestHelper.Verify(source);
	}

	[Fact]
	public Task GeneratesEndpointExtensions_WithDifferentHttpVerbs()
	{
		// Arrange
		const string source = """
				using IeuanWalker.MinimalApi.Endpoints;
				using Microsoft.AspNetCore.Http.HttpResults;

				namespace TestNamespace;

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

				public class PatchUserEndpoint : IEndpoint<PatchUserRequest, Ok<UserResponse>>
				{
					public static void Configure(RouteHandlerBuilder builder)
					{
						builder.Patch("/api/users/{id}");
					}

					public Task<Ok<UserResponse>> Handle(PatchUserRequest request, CancellationToken ct)
					{
						return Task.FromResult(TypedResults.Ok(new UserResponse(request.Id, "Updated")));
					}
				}

				public record UpdateUserRequest(int Id, string Name);
				public record PatchUserRequest(int Id);
				public record UserResponse(int Id, string Name);
				""";

		// Act & Assert
		return TestHelper.Verify(source);
	}

	[Fact]
	public Task GeneratesEndpointExtensions_WithResultsUnionResponse()
	{
		// Arrange
		const string source = """
				using IeuanWalker.MinimalApi.Endpoints;
				using Microsoft.AspNetCore.Http.HttpResults;

				namespace TestNamespace;

				public class GetUserEndpoint : IEndpoint<GetUserRequest, Results<Ok<UserResponse>, NotFound, BadRequest>>
				{
					public static void Configure(RouteHandlerBuilder builder)
					{
						builder.Get("/api/users/{id}");
					}

					public Task<Results<Ok<UserResponse>, NotFound, BadRequest>> Handle(GetUserRequest request, CancellationToken ct)
					{
						if (request.Id <= 0)
						{
							return Task.FromResult<Results<Ok<UserResponse>, NotFound, BadRequest>>(TypedResults.BadRequest());
						}

						return Task.FromResult<Results<Ok<UserResponse>, NotFound, BadRequest>>(TypedResults.Ok(new UserResponse(request.Id, "John")));
					}
				}

				public record GetUserRequest(int Id);
				public record UserResponse(int Id, string Name);
				""";

		// Act & Assert
		return TestHelper.Verify(source);
	}

	[Fact]
	public Task GeneratesEndpointExtensions_WithComplexCombination()
	{
		// Arrange
		const string source = """
				using IeuanWalker.MinimalApi.Endpoints;
				using FluentValidation;
				using Microsoft.AspNetCore.Http.HttpResults;

				namespace TestNamespace;

				public class UserEndpointGroup : IEndpointGroup
				{
					public static RouteGroupBuilder Configure(WebApplication app)
					{
						return app.MapGroup("/api/v1/users");
					}
				}

				public class CreateUserEndpoint : IEndpoint<CreateUserRequest, Results<Ok<UserResponse>, BadRequest>>
				{
					public static void Configure(RouteHandlerBuilder builder)
					{
						builder
							.Group<UserEndpointGroup>()
							.Post("/")
							.RequestFromBody()
							.WithName("CreateUser")
							.WithTags("UserManagement");
					}

					public Task<Results<Ok<UserResponse>, BadRequest>> Handle(CreateUserRequest request, CancellationToken ct)
					{
						return Task.FromResult<Results<Ok<UserResponse>, BadRequest>>(TypedResults.Ok(new UserResponse(1, request.Name)));
					}
				}

				public record CreateUserRequest(string Name, string Email);
				public record UserResponse(int Id, string Name);

				public class CreateUserRequestValidator : Validator<CreateUserRequest>
				{
					public CreateUserRequestValidator()
					{
						RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
						RuleFor(x => x.Email).EmailAddress();
					}
				}
				""";

		// Act & Assert
		return TestHelper.Verify(source);
	}

	[Fact]
	public Task GeneratesEndpointExtensions_WithMinimalRoutePattern()
	{
		// Arrange
		const string source = """
				using IeuanWalker.MinimalApi.Endpoints;
				using Microsoft.AspNetCore.Http.HttpResults;

				namespace TestNamespace;

				public class RootEndpoint : IEndpoint<RootRequest, Ok<RootResponse>>
				{
					public static void Configure(RouteHandlerBuilder builder)
					{
						builder.Get("/");
					}

					public Task<Ok<RootResponse>> Handle(RootRequest request, CancellationToken ct)
					{
						return Task.FromResult(TypedResults.Ok(new RootResponse("Welcome")));
					}
				}

				public record RootRequest();
				public record RootResponse(string Message);
				""";

		// Act & Assert
		return TestHelper.Verify(source);
	}

	[Fact]
	public Task GeneratesEndpointExtensions_WithMultipleValidatorsForDifferentTypes()
	{
		// Arrange
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
				public record Address(string Street, string City);

				public class CreateUserRequestValidator : Validator<CreateUserRequest>
				{
					public CreateUserRequestValidator()
					{
						RuleFor(x => x.Name).NotEmpty();
						RuleFor(x => x.Email).EmailAddress();
					}
				}

				public class UpdateUserRequestValidator : Validator<UpdateUserRequest>
				{
					public UpdateUserRequestValidator()
					{
						RuleFor(x => x.Id).GreaterThan(0);
						RuleFor(x => x.Name).NotEmpty();
					}
				}

				public class AddressValidator : Validator<Address>
				{
					public AddressValidator()
					{
						RuleFor(x => x.Street).NotEmpty();
						RuleFor(x => x.City).NotEmpty();
					}
				}
				""";

		// Act & Assert
		return TestHelper.Verify(source);
	}


	#region Nested Groups Tests

	[Fact]
	public Task GeneratesEndpointExtensions_WithNestedEndpointGroups()
	{
		// Arrange
		const string source = """
				using IeuanWalker.MinimalApi.Endpoints;
				using Microsoft.AspNetCore.Http.HttpResults;

				namespace TestNamespace;

				public class ApiEndpointGroup : IEndpointGroup
				{
					public static RouteGroupBuilder Configure(WebApplication app)
					{
						return app.MapGroup("/api")
							.WithTags("API");
					}
				}

				public class V1EndpointGroup : IEndpointGroup
				{
					public static RouteGroupBuilder Configure(WebApplication app)
					{
						return app.MapGroup("/v1")
							.WithTags("V1");
					}
				}

				public class UsersEndpointGroup : IEndpointGroup
				{
					public static RouteGroupBuilder Configure(WebApplication app)
					{
						return app.MapGroup("/users")
							.WithTags("Users");
					}
				}

				public class GetUserEndpoint : IEndpoint<GetUserRequest, Ok<UserResponse>>
				{
					public static void Configure(RouteHandlerBuilder builder)
					{
						builder
							.Group<ApiEndpointGroup>()
							.Get("/{id}");
					}

					public Task<Ok<UserResponse>> Handle(GetUserRequest request, CancellationToken ct)
					{
						return Task.FromResult(TypedResults.Ok(new UserResponse(request.Id, "John")));
					}
				}

				public class GetUserV1Endpoint : IEndpoint<GetUserRequest, Ok<UserResponse>>
				{
					public static void Configure(RouteHandlerBuilder builder)
					{
						builder
							.Group<V1EndpointGroup>()
							.Get("/user/{id}");
					}

					public Task<Ok<UserResponse>> Handle(GetUserRequest request, CancellationToken ct)
					{
						return Task.FromResult(TypedResults.Ok(new UserResponse(request.Id, "John V1")));
					}
				}

				public class GetAllUsersEndpoint : IEndpointWithoutRequest<Ok<List<UserResponse>>>
				{
					public static void Configure(RouteHandlerBuilder builder)
					{
						builder
							.Group<UsersEndpointGroup>()
							.Get("/");
					}

					public Task<Ok<List<UserResponse>>> Handle(CancellationToken ct)
					{
						return Task.FromResult(TypedResults.Ok(new List<UserResponse>()));
					}
				}

				public record GetUserRequest(int Id);
				public record UserResponse(int Id, string Name);
				""";

		// Act & Assert
		return TestHelper.Verify(source);
	}

	#endregion

	#region Real-World Combination Test

	[Fact]
	public Task GeneratesEndpointExtensions_WithRealWorldCombination()
	{
		// Arrange
		const string source = """
				using IeuanWalker.MinimalApi.Endpoints;
				using FluentValidation;
				using Microsoft.AspNetCore.Http.HttpResults;

				namespace TestNamespace;

				// Endpoint Groups
				public class ApiEndpointGroup : IEndpointGroup
				{
					public static RouteGroupBuilder Configure(WebApplication app)
					{
						return app.MapGroup("/api")
							.WithTags("API")
							.WithName("APIGroup");
					}
				}

				public class UsersEndpointGroup : IEndpointGroup
				{
					public static RouteGroupBuilder Configure(WebApplication app)
					{
						return app.MapGroup("/users")
							.WithTags("Users");
					}
				}

				// Create User Endpoint with validation
				public class CreateUserEndpoint : IEndpoint<CreateUserRequest, Results<Created<UserResponse>, BadRequest, Conflict>>
				{
					public static void Configure(RouteHandlerBuilder builder)
					{
						builder
							.Group<ApiEndpointGroup>()
							.Post("/")
							.RequestFromBody()
							.WithName("CreateUser")
							.WithSummary("Create a new user")
							.WithDescription("Creates a new user in the system");
					}

					public Task<Results<Created<UserResponse>, BadRequest, Conflict>> Handle(CreateUserRequest request, CancellationToken ct)
					{
						return Task.FromResult<Results<Created<UserResponse>, BadRequest, Conflict>>(
							TypedResults.Created($"/api/users/{1}", new UserResponse(1, request.Name, request.Email)));
					}
				}

				// Get User by ID with route binding
				public class GetUserByIdEndpoint : IEndpoint<GetUserByIdRequest, Results<Ok<UserResponse>, NotFound>>
				{
					public static void Configure(RouteHandlerBuilder builder)
					{
						builder
							.Group<UsersEndpointGroup>()
							.Get("/{id}")
							.RequestFromRoute()
							.WithName("GetUserById")
							.WithTags("Queries");
					}

					public Task<Results<Ok<UserResponse>, NotFound>> Handle(GetUserByIdRequest request, CancellationToken ct)
					{
						return Task.FromResult<Results<Ok<UserResponse>, NotFound>>(
							TypedResults.Ok(new UserResponse(request.Id, "John", "john@example.com")));
					}
				}

				// Search Users with query parameters
				public class SearchUsersEndpoint : IEndpoint<SearchUsersRequest, Ok<List<UserResponse>>>
				{
					public static void Configure(RouteHandlerBuilder builder)
					{
						builder
							.Group<ApiEndpointGroup>()
							.Get("/search")
							.RequestAsParameters()
							.WithName("SearchUsers")
							.WithTags("Queries", "Search");
					}

					public Task<Ok<List<UserResponse>>> Handle(SearchUsersRequest request, CancellationToken ct)
					{
						return Task.FromResult(TypedResults.Ok(new List<UserResponse>()));
					}
				}

				// Update User
				public class UpdateUserEndpoint : IEndpoint<UpdateUserRequest, Results<Ok<UserResponse>, NotFound, BadRequest>>
				{
					public static void Configure(RouteHandlerBuilder builder)
					{
						builder
							.Group<UsersEndpointGroup>()
							.Put("/{id}")
							.RequestFromBody()
							.WithName("UpdateUser");
					}

					public Task<Results<Ok<UserResponse>, NotFound, BadRequest>> Handle(UpdateUserRequest request, CancellationToken ct)
					{
						return Task.FromResult<Results<Ok<UserResponse>, NotFound, BadRequest>>(
							TypedResults.Ok(new UserResponse(request.Id, request.Name, request.Email)));
					}
				}

				// Delete User
				public class DeleteUserEndpoint : IEndpointWithoutResponse<DeleteUserRequest>
				{
					public static void Configure(RouteHandlerBuilder builder)
					{
						builder
							.Group<UsersEndpointGroup>()
							.Delete("/{id}")
							.RequestFromRoute()
							.WithName("DeleteUser");
					}

					public Task Handle(DeleteUserRequest request, CancellationToken ct)
					{
						return Task.CompletedTask;
					}
				}

				// Health Check without request or response
				public class HealthCheckEndpoint : IEndpoint
				{
					public static void Configure(RouteHandlerBuilder builder)
					{
						builder
							.Get("/health")
							.WithName("HealthCheck")
							.WithTags("Health");
					}

					public Task Handle(CancellationToken ct)
					{
						return Task.CompletedTask;
					}
				}

				// Request Models
				public record CreateUserRequest(string Name, string Email, string Password);
				public record GetUserByIdRequest(int Id);
				public record SearchUsersRequest(string Query, int Page, int PageSize);
				public record UpdateUserRequest(int Id, string Name, string Email);
				public record DeleteUserRequest(int Id);

				// Response Models
				public record UserResponse(int Id, string Name, string Email);

				// Validators
				public class CreateUserRequestValidator : Validator<CreateUserRequest>
				{
					public CreateUserRequestValidator()
					{
						RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
						RuleFor(x => x.Email).NotEmpty().EmailAddress();
						RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
					}
				}

				public class UpdateUserRequestValidator : Validator<UpdateUserRequest>
				{
					public UpdateUserRequestValidator()
					{
						RuleFor(x => x.Id).GreaterThan(0);
						RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
						RuleFor(x => x.Email).NotEmpty().EmailAddress();
					}
				}

				public class SearchUsersRequestValidator : Validator<SearchUsersRequest>
				{
					public SearchUsersRequestValidator()
					{
						RuleFor(x => x.Query).NotEmpty().MinimumLength(2);
						RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
						RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
					}
				}
				""";

		// Act & Assert
		return TestHelper.Verify(source);
	}

	#endregion
}
