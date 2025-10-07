using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace IeuanWalker.MinimalApi.Endpoints.Generator.Tests;

[UsesVerify]
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
			
			public class V1EndpointGroup : IEndpointGroup
			{
				public static RouteGroupBuilder Configure(WebApplication app)
				{
					return app.MapGroup("/v1");
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
						.RequestBinding(RequestBindingType.FromBody);
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
						.Get("/api/users/search")
						.RequestBinding(RequestBindingType.AsParameters);
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
}
