using IeuanWalker.MinimalApi.Endpoints.Generator.Helpers;
using Microsoft.CodeAnalysis;
using Shouldly;

namespace IeuanWalker.MinimalApi.Endpoints.Generator.Tests.Helpers;

public class EndpointGeneratorHelpersTests
{
	#region ToEndpoint Tests

	[Fact]
	public void ToEndpoint_WithBasicGetEndpoint_GeneratesCorrectCode()
	{
		// Arrange
		using IndentedTextBuilder builder = new();
		EndpointInfo endpoint = CreateEndpointInfo(
			httpVerb: HttpVerb.Get,
			routePattern: "/users",
			typeName: "GetUsersEndpoint");

		const string expected = """
			// GET: /users
			RouteHandlerBuilder get_Users_0 = app
				.MapGet("/users", async (
					[FromServices] global::GetUsersEndpoint endpoint,
					CancellationToken ct) => await endpoint.Handle(ct))
				.WithTags("Users")
				.WithName("get_Users_0");

			global::GetUsersEndpoint.Configure(get_Users_0);

			""";

		// Act
		builder.ToEndpoint(endpoint, 0, [], null);

		// Assert
		string result = builder.ToString();
		result.ShouldBe(expected);
	}

	[Fact]
	public void ToEndpoint_WithRequestType_IncludesRequestParameter()
	{
		// Arrange
		using IndentedTextBuilder builder = new();
		EndpointInfo endpoint = CreateEndpointInfo(
			httpVerb: HttpVerb.Post,
			routePattern: "/users",
			typeName: "CreateUserEndpoint",
			requestType: "CreateUserRequest");

		const string expected = """
			// POST: /users
			RouteHandlerBuilder post_Users_1 = app
				.MapPost("/users", async (
					global::CreateUserRequest request,
					[FromServices] global::CreateUserEndpoint endpoint,
					CancellationToken ct) => await endpoint.Handle(request, ct))
				.WithTags("Users")
				.WithName("post_Users_1");

			global::CreateUserEndpoint.Configure(post_Users_1);
			
			""";

		// Act
		builder.ToEndpoint(endpoint, 1, [], null);

		// Assert
		string result = builder.ToString();
		result.ShouldBe(expected);
	}

	[Fact]
	public void ToEndpoint_WithRequestBindingTypeFromBody_IncludesFromBodyAttribute()
	{
		// Arrange
		using IndentedTextBuilder builder = new();
		EndpointInfo endpoint = CreateEndpointInfo(
			httpVerb: HttpVerb.Post,
			routePattern: "/users",
			typeName: "CreateUserEndpoint",
			requestType: "CreateUserRequest",
			requestBindingType: (RequestBindingTypeEnum.FromBody, null));

		const string expected = """
			// POST: /users
			RouteHandlerBuilder post_Users_2 = app
				.MapPost("/users", async (
					[FromBody] global::CreateUserRequest request,
					[FromServices] global::CreateUserEndpoint endpoint,
					CancellationToken ct) => await endpoint.Handle(request, ct))
				.WithTags("Users")
				.WithName("post_Users_2");

			global::CreateUserEndpoint.Configure(post_Users_2);

			""";

		// Act
		builder.ToEndpoint(endpoint, 2, [], null);

		// Assert
		string result = builder.ToString();
		result.ShouldBe(expected);
	}

	[Fact]
	public void ToEndpoint_WithRequestBindingTypeFromRoute_IncludesFromRouteAttribute()
	{
		// Arrange
		using IndentedTextBuilder builder = new();
		EndpointInfo endpoint = CreateEndpointInfo(
			httpVerb: HttpVerb.Get,
			routePattern: "/users/{id}",
			typeName: "GetUserEndpoint",
			requestType: "GetUserRequest",
			requestBindingType: (RequestBindingTypeEnum.FromRoute, null));

		const string expected = """
			// GET: /users/{id}
			RouteHandlerBuilder get_Users_3 = app
				.MapGet("/users/{id}", async (
					[FromRoute] global::GetUserRequest request,
					[FromServices] global::GetUserEndpoint endpoint,
					CancellationToken ct) => await endpoint.Handle(request, ct))
				.WithTags("Users")
				.WithName("get_Users_3");

			global::GetUserEndpoint.Configure(get_Users_3);
			
			""";

		// Act
		builder.ToEndpoint(endpoint, 3, [], null);

		// Assert
		string result = builder.ToString();
		result.ShouldBe(expected);
	}

	[Fact]
	public void ToEndpoint_WithRequestBindingTypeAndName_IncludesNamedBindingAttribute()
	{
		// Arrange
		using IndentedTextBuilder builder = new();
		EndpointInfo endpoint = CreateEndpointInfo(
			httpVerb: HttpVerb.Get,
			routePattern: "/users/{id}",
			typeName: "GetUserEndpoint",
			requestType: "GetUserRequest",
			requestBindingType: (RequestBindingTypeEnum.FromRoute, "userId"));

		const string expected = """
			// GET: /users/{id}
			RouteHandlerBuilder get_Users_4 = app
				.MapGet("/users/{id}", async (
					[FromRoute(Name = "userId")] global::GetUserRequest request,
					[FromServices] global::GetUserEndpoint endpoint,
					CancellationToken ct) => await endpoint.Handle(request, ct))
				.WithTags("Users")
				.WithName("get_Users_4");

			global::GetUserEndpoint.Configure(get_Users_4);

			""";

		// Act
		builder.ToEndpoint(endpoint, 4, [], null);

		// Assert
		string result = builder.ToString();
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData(RequestBindingTypeEnum.FromQuery, "[FromQuery] ")]
	[InlineData(RequestBindingTypeEnum.FromHeader, "[FromHeader] ")]
	[InlineData(RequestBindingTypeEnum.FromForm, "[FromForm] ")]
	[InlineData(RequestBindingTypeEnum.AsParameters, "[AsParameters] ")]
	public void ToEndpoint_WithDifferentRequestBindingTypes_GeneratesCorrectAttributes(RequestBindingTypeEnum bindingType, string expectedAttribute)
	{
		// Arrange
		using IndentedTextBuilder builder = new();
		EndpointInfo endpoint = CreateEndpointInfo(
			httpVerb: HttpVerb.Post,
			routePattern: "/test",
			typeName: "TestEndpoint",
			requestType: "TestRequest",
			requestBindingType: (bindingType, null));

		string expected = $"""
			// POST: /test
			RouteHandlerBuilder post_Test_5 = app
				.MapPost("/test", async (
					{expectedAttribute}global::TestRequest request,
					[FromServices] global::TestEndpoint endpoint,
					CancellationToken ct) => await endpoint.Handle(request, ct))
				.WithTags("Test")
				.WithName("post_Test_5");

			global::TestEndpoint.Configure(post_Test_5);

			""";

		// Act
		builder.ToEndpoint(endpoint, 5, [], null);

		// Assert
		string result = builder.ToString();
		result.ShouldBe(expected);
	}

	[Fact]
	public void ToEndpoint_WithRequestTypeButNoBindingType_DoesNotIncludeAttribute()
	{
		// Arrange
		using IndentedTextBuilder builder = new();
		EndpointInfo endpoint = CreateEndpointInfo(
			httpVerb: HttpVerb.Post,
			routePattern: "/users",
			typeName: "CreateUserEndpoint",
			requestType: "CreateUserRequest",
			requestBindingType: null);

		const string expected = """
			// POST: /users
			RouteHandlerBuilder post_Users_6 = app
				.MapPost("/users", async (
					global::CreateUserRequest request,
					[FromServices] global::CreateUserEndpoint endpoint,
					CancellationToken ct) => await endpoint.Handle(request, ct))
				.WithTags("Users")
				.WithName("post_Users_6");

			global::CreateUserEndpoint.Configure(post_Users_6);
			
			""";

		// Act
		builder.ToEndpoint(endpoint, 6, [], null);

		// Assert
		string result = builder.ToString();
		result.ShouldBe(expected);
	}

	[Fact]
	public void ToEndpoint_WithGroup_UsesGroupNameAndCombinesPatterns()
	{
		// Arrange
		using IndentedTextBuilder builder = new();
		EndpointInfo endpoint = CreateEndpointInfo(
			httpVerb: HttpVerb.Get,
			routePattern: "/users",
			typeName: "GetUsersEndpoint");

		EndpointGroupInfo groupInfo = CreateEndpointGroupInfo("/api/v1", "UserGroup");
		(EndpointGroupInfo groupInfo, string groupName) group = (groupInfo, "userGroup");

		const string expected = """
			// GET: /api/v1/users
			RouteHandlerBuilder get_Users_7 = userGroup
				.MapGet("/users", async (
					[FromServices] global::GetUsersEndpoint endpoint,
					CancellationToken ct) => await endpoint.Handle(ct))
				.WithTags("Users")
				.WithName("get_Users_7");

			global::GetUsersEndpoint.Configure(get_Users_7);
			
			""";

		// Act
		builder.ToEndpoint(endpoint, 7, [], group);

		// Assert
		string result = builder.ToString();
		result.ShouldBe(expected);
	}

	[Fact]
	public void ToEndpoint_WithEndpointWithName_DoesNotGenerateWithName()
	{
		// Arrange
		using IndentedTextBuilder builder = new();
		EndpointInfo endpoint = CreateEndpointInfo(
			httpVerb: HttpVerb.Get,
			routePattern: "/users",
			typeName: "GetUsersEndpoint",
			withName: "GetAllUsers");

		const string expected = """
			// GET: /users
			RouteHandlerBuilder get_Users_8 = app
				.MapGet("/users", async (
					[FromServices] global::GetUsersEndpoint endpoint,
					CancellationToken ct) => await endpoint.Handle(ct))
				.WithTags("Users");

			global::GetUsersEndpoint.Configure(get_Users_8);
			
			""";

		// Act
		builder.ToEndpoint(endpoint, 8, [], null);

		// Assert
		string result = builder.ToString();
		result.ShouldBe(expected);
	}

	[Fact]
	public void ToEndpoint_WithGroupWithName_DoesNotGenerateWithName()
	{
		// Arrange
		using IndentedTextBuilder builder = new();
		EndpointInfo endpoint = CreateEndpointInfo(
			httpVerb: HttpVerb.Get,
			routePattern: "/users",
			typeName: "GetUsersEndpoint");

		EndpointGroupInfo groupInfo = CreateEndpointGroupInfo("/api/v1", "UserGroup", withName: "UserApi");
		(EndpointGroupInfo groupInfo, string groupName) group = (groupInfo, "userGroup");

		const string expected = """
			// GET: /api/v1/users
			RouteHandlerBuilder get_Users_9 = userGroup
				.MapGet("/users", async (
					[FromServices] global::GetUsersEndpoint endpoint,
					CancellationToken ct) => await endpoint.Handle(ct))
				.WithTags("Users");

			global::GetUsersEndpoint.Configure(get_Users_9);
			
			""";

		// Act
		builder.ToEndpoint(endpoint, 9, [], group);

		// Assert
		string result = builder.ToString();
		result.ShouldBe(expected);
	}

	[Fact]
	public void ToEndpoint_WithEndpointWithTags_DoesNotGenerateWithTags()
	{
		// Arrange
		using IndentedTextBuilder builder = new();
		EndpointInfo endpoint = CreateEndpointInfo(
			httpVerb: HttpVerb.Get,
			routePattern: "/users",
			typeName: "GetUsersEndpoint",
			withTags: "UserManagement");

		const string expected = """
			// GET: /users
			RouteHandlerBuilder get_Users_10 = app
				.MapGet("/users", async (
					[FromServices] global::GetUsersEndpoint endpoint,
					CancellationToken ct) => await endpoint.Handle(ct))
				.WithName("get_Users_10");

			global::GetUsersEndpoint.Configure(get_Users_10);
			
			""";

		// Act
		builder.ToEndpoint(endpoint, 10, [], null);

		// Assert
		string result = builder.ToString();
		result.ShouldBe(expected);
	}

	[Fact]
	public void ToEndpoint_WithGroupWithTags_DoesNotGenerateWithTags()
	{
		// Arrange
		using IndentedTextBuilder builder = new();
		EndpointInfo endpoint = CreateEndpointInfo(
			httpVerb: HttpVerb.Get,
			routePattern: "/users",
			typeName: "GetUsersEndpoint");

		EndpointGroupInfo groupInfo = CreateEndpointGroupInfo("/api/v1", "UserGroup", withTags: "ApiV1");
		(EndpointGroupInfo groupInfo, string groupName) group = (groupInfo, "userGroup");

		const string expected = """
			// GET: /api/v1/users
			RouteHandlerBuilder get_Users_11 = userGroup
				.MapGet("/users", async (
					[FromServices] global::GetUsersEndpoint endpoint,
					CancellationToken ct) => await endpoint.Handle(ct))
				.WithName("get_Users_11");

			global::GetUsersEndpoint.Configure(get_Users_11);
			
			""";

		// Act
		builder.ToEndpoint(endpoint, 11, [], group);

		// Assert
		string result = builder.ToString();
		result.ShouldBe(expected);
	}

	[Fact]
	public void ToEndpoint_WithValidatorForRequestType_AddsValidationFilter()
	{
		// Arrange
		using IndentedTextBuilder builder = new();
		EndpointInfo endpoint = CreateEndpointInfo(
			httpVerb: HttpVerb.Post,
			routePattern: "/users",
			typeName: "CreateUserEndpoint",
			requestType: "CreateUserRequest");

		List<ValidatorInfo> validators = [
			CreateValidatorInfo("CreateUserRequestValidator", "CreateUserRequest")
		];

		const string expected = """
			// POST: /users
			RouteHandlerBuilder post_Users_12 = app
				.MapPost("/users", async (
					global::CreateUserRequest request,
					[FromServices] global::CreateUserEndpoint endpoint,
					CancellationToken ct) => await endpoint.Handle(request, ct))
				.WithTags("Users")
				.WithName("post_Users_12")
				.DisableValidation()
				.AddEndpointFilter<FluentValidationFilter<global::CreateUserRequest>>()
				.ProducesValidationProblem();

			global::CreateUserEndpoint.Configure(post_Users_12);
			
			""";

		// Act
		builder.ToEndpoint(endpoint, 12, validators, null);

		// Assert
		string result = builder.ToString();
		result.ShouldBe(expected);
	}

	[Fact]
	public void ToEndpoint_WithoutValidatorForRequestType_DoesNotAddValidationFilter()
	{
		// Arrange
		using IndentedTextBuilder builder = new();
		EndpointInfo endpoint = CreateEndpointInfo(
			httpVerb: HttpVerb.Post,
			routePattern: "/users",
			typeName: "CreateUserEndpoint",
			requestType: "CreateUserRequest");

		List<ValidatorInfo> validators = [
			CreateValidatorInfo("OtherRequestValidator", "OtherRequest")
		];

		const string expected = """
			// POST: /users
			RouteHandlerBuilder post_Users_13 = app
				.MapPost("/users", async (
					global::CreateUserRequest request,
					[FromServices] global::CreateUserEndpoint endpoint,
					CancellationToken ct) => await endpoint.Handle(request, ct))
				.WithTags("Users")
				.WithName("post_Users_13");

			global::CreateUserEndpoint.Configure(post_Users_13);
			
			""";

		// Act
		builder.ToEndpoint(endpoint, 13, validators, null);

		// Assert
		string result = builder.ToString();
		result.ShouldBe(expected);
	}

	[Fact]
	public void ToEndpoint_WithoutRequestType_DoesNotAddValidationFilter()
	{
		// Arrange
		using IndentedTextBuilder builder = new();
		EndpointInfo endpoint = CreateEndpointInfo(
			httpVerb: HttpVerb.Get,
			routePattern: "/users",
			typeName: "GetUsersEndpoint",
			requestType: null);

		List<ValidatorInfo> validators = [
			CreateValidatorInfo("SomeValidator", "SomeRequest")
		];

		const string expected = """
			// GET: /users
			RouteHandlerBuilder get_Users_14 = app
				.MapGet("/users", async (
					[FromServices] global::GetUsersEndpoint endpoint,
					CancellationToken ct) => await endpoint.Handle(ct))
				.WithTags("Users")
				.WithName("get_Users_14");

			global::GetUsersEndpoint.Configure(get_Users_14);
			
			""";

		// Act
		builder.ToEndpoint(endpoint, 14, validators, null);

		// Assert
		string result = builder.ToString();
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData(HttpVerb.Get, "MapGet")]
	[InlineData(HttpVerb.Post, "MapPost")]
	[InlineData(HttpVerb.Put, "MapPut")]
	[InlineData(HttpVerb.Patch, "MapPatch")]
	[InlineData(HttpVerb.Delete, "MapDelete")]
	public void ToEndpoint_WithDifferentHttpVerbs_GeneratesCorrectMapMethod(HttpVerb httpVerb, string expectedMapMethod)
	{
		// Arrange
		using IndentedTextBuilder builder = new();
		EndpointInfo endpoint = CreateEndpointInfo(
			httpVerb: httpVerb,
			routePattern: "/test",
			typeName: "TestEndpoint");

		string expected = $"""
			// {httpVerb.ToString().ToUpper()}: /test
			RouteHandlerBuilder {httpVerb.ToString().ToLower()}_Test_15 = app
				.{expectedMapMethod}("/test", async (
					[FromServices] global::TestEndpoint endpoint,
					CancellationToken ct) => await endpoint.Handle(ct))
				.WithTags("Test")
				.WithName("{httpVerb.ToString().ToLower()}_Test_15");

			global::TestEndpoint.Configure({httpVerb.ToString().ToLower()}_Test_15);
			
			""";

		// Act
		builder.ToEndpoint(endpoint, 15, [], null);

		// Assert
		string result = builder.ToString();
		result.ShouldBe(expected);
	}

	[Fact]
	public void ToEndpoint_WithComplexRoutePattern_GeneratesCorrectComment()
	{
		// Arrange
		using IndentedTextBuilder builder = new();
		EndpointInfo endpoint = CreateEndpointInfo(
			httpVerb: HttpVerb.Get,
			routePattern: "/users/{id}/posts/{postId}",
			typeName: "GetUserPostEndpoint");

		EndpointGroupInfo groupInfo = CreateEndpointGroupInfo("/api/v1", "UserGroup");
		(EndpointGroupInfo groupInfo, string groupName) group = (groupInfo, "userGroup");

		const string expected = """
			// GET: /api/v1/users/{id}/posts/{postId}
			RouteHandlerBuilder get_UsersPosts_16 = userGroup
				.MapGet("/users/{id}/posts/{postId}", async (
					[FromServices] global::GetUserPostEndpoint endpoint,
					CancellationToken ct) => await endpoint.Handle(ct))
				.WithTags("Users")
				.WithName("get_UsersPosts_16");

			global::GetUserPostEndpoint.Configure(get_UsersPosts_16);
			
			""";

		// Act
		builder.ToEndpoint(endpoint, 16, [], group);

		// Assert
		string result = builder.ToString();
		result.ShouldBe(expected);
	}

	[Fact]
	public void ToEndpoint_WithEmptyValidatorsList_DoesNotThrow()
	{
		// Arrange
		using IndentedTextBuilder builder = new();
		EndpointInfo endpoint = CreateEndpointInfo(
			httpVerb: HttpVerb.Get,
			routePattern: "/test",
			typeName: "TestEndpoint");

		// Act & Assert - Should not throw
		Should.NotThrow(() => builder.ToEndpoint(endpoint, 17, [], null));
	}

	[Fact]
	public void ToEndpoint_WithNullGroup_UsesAppAsBaseBuilder()
	{
		// Arrange
		using IndentedTextBuilder builder = new();
		EndpointInfo endpoint = CreateEndpointInfo(
			httpVerb: HttpVerb.Get,
			routePattern: "/test",
			typeName: "TestEndpoint");

		const string expected = """
			// GET: /test
			RouteHandlerBuilder get_Test_18 = app
				.MapGet("/test", async (
					[FromServices] global::TestEndpoint endpoint,
					CancellationToken ct) => await endpoint.Handle(ct))
				.WithTags("Test")
				.WithName("get_Test_18");

			global::TestEndpoint.Configure(get_Test_18);
			
			""";

		// Act
		builder.ToEndpoint(endpoint, 18, [], null);

		// Assert
		string result = builder.ToString();
		result.ShouldBe(expected);
	}

	[Fact]
	public void ToEndpoint_GeneratesCorrectStructure_AndEndsWithConfigureCall()
	{
		// Arrange
		using IndentedTextBuilder builder = new();
		EndpointInfo endpoint = CreateEndpointInfo(
			httpVerb: HttpVerb.Post,
			routePattern: "/users",
			typeName: "CreateUserEndpoint",
			requestType: "CreateUserRequest");

		const string expected = """
			// POST: /users
			RouteHandlerBuilder post_Users_19 = app
				.MapPost("/users", async (
					global::CreateUserRequest request,
					[FromServices] global::CreateUserEndpoint endpoint,
					CancellationToken ct) => await endpoint.Handle(request, ct))
				.WithTags("Users")
				.WithName("post_Users_19");

			global::CreateUserEndpoint.Configure(post_Users_19);
			
			""";

		// Act
		builder.ToEndpoint(endpoint, 19, [], null);

		// Assert
		string result = builder.ToString();
		result.ShouldBe(expected);
	}

	[Fact]
	public void ToEndpoint_WithNullRequestTypeAndNullBindingType_DoesNotIncludeRequestParameter()
	{
		// Arrange
		using IndentedTextBuilder builder = new();
		EndpointInfo endpoint = CreateEndpointInfo(
			httpVerb: HttpVerb.Get,
			routePattern: "/users",
			typeName: "GetUsersEndpoint",
			requestType: null,
			requestBindingType: null);

		const string expected = """
			// GET: /users
			RouteHandlerBuilder get_Users_20 = app
				.MapGet("/users", async (
					[FromServices] global::GetUsersEndpoint endpoint,
					CancellationToken ct) => await endpoint.Handle(ct))
				.WithTags("Users")
				.WithName("get_Users_20");

			global::GetUsersEndpoint.Configure(get_Users_20);
			
			""";

		// Act
		builder.ToEndpoint(endpoint, 20, [], null);

		// Assert
		string result = builder.ToString();
		result.ShouldBe(expected);
	}

	[Fact]
	public void ToEndpoint_WithMultipleValidators_OnlyUsesMatchingValidator()
	{
		// Arrange
		using IndentedTextBuilder builder = new();
		EndpointInfo endpoint = CreateEndpointInfo(
			httpVerb: HttpVerb.Post,
			routePattern: "/users",
			typeName: "CreateUserEndpoint",
			requestType: "CreateUserRequest");

		List<ValidatorInfo> validators = [
			CreateValidatorInfo("CreateUserRequestValidator", "CreateUserRequest"),
			CreateValidatorInfo("UpdateUserRequestValidator", "UpdateUserRequest"),
			CreateValidatorInfo("DeleteUserRequestValidator", "DeleteUserRequest")
		];

		const string expected = """
			// POST: /users
			RouteHandlerBuilder post_Users_21 = app
				.MapPost("/users", async (
					global::CreateUserRequest request,
					[FromServices] global::CreateUserEndpoint endpoint,
					CancellationToken ct) => await endpoint.Handle(request, ct))
				.WithTags("Users")
				.WithName("post_Users_21")
				.DisableValidation()
				.AddEndpointFilter<FluentValidationFilter<global::CreateUserRequest>>()
				.ProducesValidationProblem();

			global::CreateUserEndpoint.Configure(post_Users_21);
			
			""";

		// Act
		builder.ToEndpoint(endpoint, 21, validators, null);

		// Assert
		string result = builder.ToString();
		result.ShouldBe(expected);
	}

	#endregion

	#region Helper Methods

	static EndpointInfo CreateEndpointInfo(
		HttpVerb httpVerb,
		string routePattern,
		string typeName,
		string? withName = null,
		string? withTags = null,
		string? group = null,
		string? requestType = null,
		(RequestBindingTypeEnum requestType, string? name)? requestBindingType = null,
		bool disableValidation = false,
		string? responseType = null)
	{
		return new EndpointInfo(
			typeName,
			httpVerb,
			routePattern,
			withName,
			withTags,
			group,
			requestType,
			requestBindingType,
			disableValidation,
			responseType,
			Location.None,
			[]);
	}

	static EndpointGroupInfo CreateEndpointGroupInfo(
		string pattern,
		string typeName,
		string? withName = null,
		string? withTags = null)
	{
		return new EndpointGroupInfo(
			typeName,
			pattern,
			withName,
			withTags,
			Location.None,
			[]);
	}

	static ValidatorInfo CreateValidatorInfo(string typeName, string validatedTypeName)
	{
		return new ValidatorInfo(
			typeName,
			validatedTypeName,
			Location.None,
			[]);
	}

	#endregion
}
