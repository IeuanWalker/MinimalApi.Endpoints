namespace IeuanWalker.MinimalApi.Endpoints.Generator.Tests;

public class RequestBindingSnapshotTests
{

	[Fact]
	public Task GeneratesEndpointExtensions_WithFromBodyBinding()
	{
		// Arrange
		const string source =
			/* language=C#-test */
			//lang=csharp
			"""
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
		const string source =
			/* language=C#-test */
			//lang=csharp
			"""
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
		const string source =
			/* language=C#-test */
			//lang=csharp
			"""
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
		const string source =
			/* language=C#-test */
			//lang=csharp
			"""
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
		const string source =
			/* language=C#-test */
			//lang=csharp
			"""
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
		const string source =
			/* language=C#-test */
			//lang=csharp
			"""
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
		const string source =
			/* language=C#-test */
			//lang=csharp
			"""
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
		const string source =
			/* language=C#-test */
			//lang=csharp
			"""
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
}
