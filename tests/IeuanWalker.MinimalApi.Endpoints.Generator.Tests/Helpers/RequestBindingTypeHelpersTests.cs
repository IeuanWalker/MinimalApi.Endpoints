using IeuanWalker.MinimalApi.Endpoints.Generator.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace IeuanWalker.MinimalApi.Endpoints.Generator.Tests.Helpers;

public class RequestBindingTypeHelpersTests
{
	#region GetRequestTypeAndName Tests

	[Fact]
	public void GetRequestTypeAndName_WithNoConfigureMethod_ReturnsNull()
	{
		// Arrange
		const string sourceCode = """
			public class TestEndpoint
			{
				public void SomeOtherMethod()
				{
				}
			}
			""";

		TypeDeclarationSyntax typeDeclaration = ParseTypeDeclaration(sourceCode);
		List<DiagnosticInfo> diagnostics = [];

		// Act
		(RequestBindingTypeEnum requestType, string? name)? result = typeDeclaration.GetRequestTypeAndName(diagnostics);

		// Assert
		result.ShouldBeNull();
		diagnostics.ShouldBeEmpty();
	}

	[Fact]
	public void GetRequestTypeAndName_WithConfigureMethodButNoRequestTypeCalls_ReturnsNull()
	{
		// Arrange
		const string sourceCode = """
			public class TestEndpoint
			{
				public static void Configure(RouteHandlerBuilder builder)
				{
					builder.Get("/test");
				}
			}
			""";

		TypeDeclarationSyntax typeDeclaration = ParseTypeDeclaration(sourceCode);
		List<DiagnosticInfo> diagnostics = [];

		// Act
		(RequestBindingTypeEnum requestType, string? name)? result = typeDeclaration.GetRequestTypeAndName(diagnostics);

		// Assert
		result.ShouldBeNull();
		diagnostics.ShouldBeEmpty();
	}

	[Theory]
	[InlineData(RequestBindingTypeEnum.FromBody, "RequestFromBody")]
	[InlineData(RequestBindingTypeEnum.FromQuery, "RequestFromQuery")]
	[InlineData(RequestBindingTypeEnum.FromRoute, "RequestFromRoute")]
	[InlineData(RequestBindingTypeEnum.FromHeader, "RequestFromHeader")]
	[InlineData(RequestBindingTypeEnum.FromForm, "RequestFromForm")]
	[InlineData(RequestBindingTypeEnum.AsParameters, "RequestAsParameters")]
	public void GetRequestTypeAndName_WithRequestType_ReturnsRequestType(RequestBindingTypeEnum expectedRequestType, string requestTypeExtension)
	{
		// Arrange
		string sourceCode = $$"""
			public class TestEndpoint
			{
				public static void Configure(RouteHandlerBuilder builder)
				{
					builder.{{requestTypeExtension}}();
				}
			}
			""";

		TypeDeclarationSyntax typeDeclaration = ParseTypeDeclaration(sourceCode);
		List<DiagnosticInfo> diagnostics = [];

		// Act
		(RequestBindingTypeEnum requestType, string? name)? result = typeDeclaration.GetRequestTypeAndName(diagnostics);

		// Assert
		result.ShouldNotBeNull();
		result.Value.requestType.ShouldBe(expectedRequestType);
		result.Value.name.ShouldBeNull();
		diagnostics.ShouldBeEmpty();
	}

	[Theory]
	[InlineData(RequestBindingTypeEnum.FromBody, "RequestFromBody")]
	[InlineData(RequestBindingTypeEnum.FromQuery, "RequestFromQuery")]
	[InlineData(RequestBindingTypeEnum.FromRoute, "RequestFromRoute")]
	[InlineData(RequestBindingTypeEnum.FromHeader, "RequestFromHeader")]
	[InlineData(RequestBindingTypeEnum.FromForm, "RequestFromForm")]
	[InlineData(RequestBindingTypeEnum.AsParameters, "RequestAsParameters")]
	public void GetRequestTypeAndName_WithRequestTypeAndName_ReturnsRequestType(RequestBindingTypeEnum expectedRequestType, string requestTypeExtension)
	{
		// Arrange
		string sourceCode = $$"""
			public class TestEndpoint
			{
				public static void Configure(RouteHandlerBuilder builder)
				{
					builder.{{requestTypeExtension}}("test");
				}
			}
			""";

		TypeDeclarationSyntax typeDeclaration = ParseTypeDeclaration(sourceCode);
		List<DiagnosticInfo> diagnostics = [];

		// Act
		(RequestBindingTypeEnum requestType, string? name)? result = typeDeclaration.GetRequestTypeAndName(diagnostics);

		// Assert
		result.ShouldNotBeNull();
		result.Value.requestType.ShouldBe(expectedRequestType);
		result.Value.name.ShouldBe("test");
		diagnostics.ShouldBeEmpty();
	}

	[Fact]
	public void GetRequestTypeAndName_WithMultipleRequestTypeMethods_ReturnsNullAndReportsDiagnostics()
	{
		// Arrange
		const string sourceCode = """
			public class TestEndpoint
			{
				public static void Configure(RouteHandlerBuilder builder)
				{
					builder.RequestFromBody();
					builder.RequestFromQuery();
				}
			}
			""";

		TypeDeclarationSyntax typeDeclaration = ParseTypeDeclaration(sourceCode);
		List<DiagnosticInfo> diagnostics = [];

		// Act
		(RequestBindingTypeEnum requestType, string? name)? result = typeDeclaration.GetRequestTypeAndName(diagnostics);

		// Assert
		result.ShouldBeNull();
		diagnostics.Count.ShouldBe(2);

		diagnostics[0].Id.ShouldBe("MINAPI009");
		diagnostics[0].Title.ShouldBe("Multiple request type methods configured");
		diagnostics[0].MessageFormat.ShouldBe("Multiple request type methods are configured in the Configure method. Only one request type method should be specified per endpoint. Remove this '{0}' call or the other conflicting request type method calls.");
		diagnostics[0].MessageArgs.ShouldHaveSingleItem();
		diagnostics[0].MessageArgs[0].ShouldBe("RequestFromBody");

		diagnostics[1].Id.ShouldBe("MINAPI009");
		diagnostics[1].Title.ShouldBe("Multiple request type methods configured");
		diagnostics[1].MessageFormat.ShouldBe("Multiple request type methods are configured in the Configure method. Only one request type method should be specified per endpoint. Remove this '{0}' call or the other conflicting request type method calls.");
		diagnostics[1].MessageArgs.ShouldHaveSingleItem();
		diagnostics[1].MessageArgs[0].ShouldBe("RequestFromQuery");
	}

	[Fact]
	public void GetRequestTypeAndName_WithThreeRequestTypeMethods_ReportsThreeDiagnostics()
	{
		// Arrange
		const string sourceCode = """
			public class TestEndpoint
			{
				public static void Configure(RouteHandlerBuilder builder)
				{
					builder.RequestFromBody();
					builder.RequestFromQuery();
					builder.RequestFromRoute();
				}
			}
			""";

		TypeDeclarationSyntax typeDeclaration = ParseTypeDeclaration(sourceCode);
		List<DiagnosticInfo> diagnostics = [];

		// Act
		(RequestBindingTypeEnum requestType, string? name)? result = typeDeclaration.GetRequestTypeAndName(diagnostics);

		// Assert
		result.ShouldBeNull();
		diagnostics.Count.ShouldBe(3);
		diagnostics.All(d => d.Id == "MINAPI009").ShouldBeTrue();
		diagnostics.All(d => d.Title == "Multiple request type methods configured").ShouldBeTrue();
		diagnostics[0].MessageArgs[0].ShouldBe("RequestFromBody");
		diagnostics[1].MessageArgs[0].ShouldBe("RequestFromQuery");
		diagnostics[2].MessageArgs[0].ShouldBe("RequestFromRoute");
	}

	[Fact]
	public void GetRequestTypeAndName_WithAllRequestTypeMethods_ReportsAllDiagnostics()
	{
		// Arrange
		const string sourceCode = """
			public class TestEndpoint
			{
				public static void Configure(RouteHandlerBuilder builder)
				{
					builder.RequestFromBody();
					builder.RequestFromQuery();
					builder.RequestFromRoute();
					builder.RequestFromHeader();
					builder.RequestFromForm();
					builder.RequestAsParameters();
				}
			}
			""";

		TypeDeclarationSyntax typeDeclaration = ParseTypeDeclaration(sourceCode);
		List<DiagnosticInfo> diagnostics = [];

		// Act
		(RequestBindingTypeEnum requestType, string? name)? result = typeDeclaration.GetRequestTypeAndName(diagnostics);

		// Assert
		result.ShouldBeNull();
		diagnostics.Count.ShouldBe(6);
		diagnostics.All(d => d.Id == "MINAPI009").ShouldBeTrue();
		diagnostics.All(d => d.Title == "Multiple request type methods configured").ShouldBeTrue();

		string[] expectedMethods = ["RequestFromBody", "RequestFromQuery", "RequestFromRoute", "RequestFromHeader", "RequestFromForm", "RequestAsParameters"];
		string[] actualMethods = [.. diagnostics.Select(d => (string)d.MessageArgs[0])];
		actualMethods.ShouldBe(expectedMethods);
	}

	[Fact]
	public void GetRequestTypeAndName_WithMethodChaining_ReturnsCorrectType()
	{
		// Arrange
		const string sourceCode = """
			public class TestEndpoint
			{
				public static void Configure(RouteHandlerBuilder builder)
				{
					builder
						.RequestFromBody("requestBody")
						.Get("/test")
						.WithName("TestEndpoint");
				}
			}
			""";

		TypeDeclarationSyntax typeDeclaration = ParseTypeDeclaration(sourceCode);
		List<DiagnosticInfo> diagnostics = [];

		// Act
		(RequestBindingTypeEnum requestType, string? name)? result = typeDeclaration.GetRequestTypeAndName(diagnostics);

		// Assert
		result.ShouldNotBeNull();
		result.Value.requestType.ShouldBe(RequestBindingTypeEnum.FromBody);
		result.Value.name.ShouldBe("requestBody");
		diagnostics.ShouldBeEmpty();
	}

	[Fact]
	public void GetRequestTypeAndName_WithComplexMethodChaining_ReturnsFirstRequestType()
	{
		// Arrange
		const string sourceCode = """
			public class TestEndpoint
			{
				public static void Configure(RouteHandlerBuilder builder)
				{
					var configured = builder
						.Get("/test")
						.WithName("TestEndpoint");
					
					configured.RequestFromQuery("search");
				}
			}
			""";

		TypeDeclarationSyntax typeDeclaration = ParseTypeDeclaration(sourceCode);
		List<DiagnosticInfo> diagnostics = [];

		// Act
		(RequestBindingTypeEnum requestType, string? name)? result = typeDeclaration.GetRequestTypeAndName(diagnostics);

		// Assert
		result.ShouldNotBeNull();
		result.Value.requestType.ShouldBe(RequestBindingTypeEnum.FromQuery);
		result.Value.name.ShouldBe("search");
		diagnostics.ShouldBeEmpty();
	}

	[Fact]
	public void GetRequestTypeAndName_WithNonStringLiteralArgument_ReturnsNullName()
	{
		// Arrange
		const string sourceCode = """
			public class TestEndpoint
			{
				public static void Configure(RouteHandlerBuilder builder)
				{
					string paramName = "dynamicName";
					builder.RequestFromQuery(paramName);
				}
			}
			""";

		TypeDeclarationSyntax typeDeclaration = ParseTypeDeclaration(sourceCode);
		List<DiagnosticInfo> diagnostics = [];

		// Act
		(RequestBindingTypeEnum requestType, string? name)? result = typeDeclaration.GetRequestTypeAndName(diagnostics);

		// Assert
		result.ShouldNotBeNull();
		result.Value.requestType.ShouldBe(RequestBindingTypeEnum.FromQuery);
		result.Value.name.ShouldBeNull();
		diagnostics.ShouldBeEmpty();
	}

	[Fact]
	public void GetRequestTypeAndName_WithEmptyStringArgument_ReturnsEmptyStringName()
	{
		// Arrange
		const string sourceCode = """
			public class TestEndpoint
			{
				public static void Configure(RouteHandlerBuilder builder)
				{
					builder.RequestFromHeader("");
				}
			}
			""";

		TypeDeclarationSyntax typeDeclaration = ParseTypeDeclaration(sourceCode);
		List<DiagnosticInfo> diagnostics = [];

		// Act
		(RequestBindingTypeEnum requestType, string? name)? result = typeDeclaration.GetRequestTypeAndName(diagnostics);

		// Assert
		result.ShouldNotBeNull();
		result.Value.requestType.ShouldBe(RequestBindingTypeEnum.FromHeader);
		result.Value.name.ShouldBe("");
		diagnostics.ShouldBeEmpty();
	}

	#endregion

	#region ConvertFromRequestBindingType Tests

	[Theory]
	[InlineData(RequestBindingTypeEnum.FromBody, "FromBody")]
	[InlineData(RequestBindingTypeEnum.FromQuery, "FromQuery")]
	[InlineData(RequestBindingTypeEnum.FromRoute, "FromRoute")]
	[InlineData(RequestBindingTypeEnum.FromHeader, "FromHeader")]
	[InlineData(RequestBindingTypeEnum.FromForm, "FromForm")]
	[InlineData(RequestBindingTypeEnum.AsParameters, "AsParameters")]
	public void ConvertFromRequestBindingType_WithValidEnum_ReturnsCorrectString(RequestBindingTypeEnum requestType, string expected)
	{
		// Act
		string result = requestType.ConvertFromRequestBindingType();

		// Assert
		result.ShouldBe(expected);
	}

	[Fact]
	public void ConvertFromRequestBindingType_WithInvalidEnum_ThrowsNotImplementedException()
	{
		// Arrange
		const RequestBindingTypeEnum invalidEnum = (RequestBindingTypeEnum)999;

		// Act & Assert
		Should.Throw<NotImplementedException>(() => invalidEnum.ConvertFromRequestBindingType());
	}

	#endregion

	#region Edge Cases and Integration Tests

	[Fact]
	public void GetRequestTypeAndName_WithNestedConfigureMethodCalls_WorksCorrectly()
	{
		// Arrange
		const string sourceCode = """
			public class TestEndpoint
			{
				public static void Configure(RouteHandlerBuilder builder)
				{
					if (true)
					{
						builder.RequestFromQuery("nestedParam");
					}
				}
			}
			""";

		TypeDeclarationSyntax typeDeclaration = ParseTypeDeclaration(sourceCode);
		List<DiagnosticInfo> diagnostics = [];

		// Act
		(RequestBindingTypeEnum requestType, string? name)? result = typeDeclaration.GetRequestTypeAndName(diagnostics);

		// Assert
		result.ShouldNotBeNull();
		result.Value.requestType.ShouldBe(RequestBindingTypeEnum.FromQuery);
		result.Value.name.ShouldBe("nestedParam");
		diagnostics.ShouldBeEmpty();
	}

	[Fact]
	public void GetRequestTypeAndName_WithRequestTypeCallsInDifferentMethods_OnlyChecksConfigureMethod()
	{
		// Arrange
		const string sourceCode = """
			public class TestEndpoint
			{
				public static void Configure(RouteHandlerBuilder builder)
				{
					builder.Get("/test");
				}
				
				public static void SomeOtherMethod(RouteHandlerBuilder builder)
				{
					builder.RequestFromBody();
				}
			}
			""";

		TypeDeclarationSyntax typeDeclaration = ParseTypeDeclaration(sourceCode);
		List<DiagnosticInfo> diagnostics = [];

		// Act
		(RequestBindingTypeEnum requestType, string? name)? result = typeDeclaration.GetRequestTypeAndName(diagnostics);

		// Assert
		result.ShouldBeNull();
		diagnostics.ShouldBeEmpty();
	}

	[Fact]
	public void GetRequestTypeAndName_WithNonStaticConfigureMethod_ReturnsNull()
	{
		// Arrange
		const string sourceCode = """
			public class TestEndpoint
			{
				public void Configure(RouteHandlerBuilder builder)
				{
					builder.RequestFromBody();
				}
			}
			""";

		TypeDeclarationSyntax typeDeclaration = ParseTypeDeclaration(sourceCode);
		List<DiagnosticInfo> diagnostics = [];

		// Act
		(RequestBindingTypeEnum requestType, string? name)? result = typeDeclaration.GetRequestTypeAndName(diagnostics);

		// Assert
		result.ShouldBeNull();
		diagnostics.ShouldBeEmpty();
	}

	[Fact]
	public void GetRequestTypeAndName_WithWrongMethodName_ReturnsNull()
	{
		// Arrange
		const string sourceCode = """
			public class TestEndpoint
			{
				public static void Setup(RouteHandlerBuilder builder)
				{
					builder.RequestFromBody();
				}
			}
			""";

		TypeDeclarationSyntax typeDeclaration = ParseTypeDeclaration(sourceCode);
		List<DiagnosticInfo> diagnostics = [];

		// Act
		(RequestBindingTypeEnum requestType, string? name)? result = typeDeclaration.GetRequestTypeAndName(diagnostics);

		// Assert
		result.ShouldBeNull();
		diagnostics.ShouldBeEmpty();
	}

	[Fact]
	public void GetRequestTypeAndName_WithCommentedOutCode_IgnoresComments()
	{
		// Arrange
		const string sourceCode = """
			public class TestEndpoint
			{
				public static void Configure(RouteHandlerBuilder builder)
				{
					// builder.RequestFromBody();
					builder.RequestFromQuery();
					/* builder.RequestFromRoute(); */
				}
			}
			""";

		TypeDeclarationSyntax typeDeclaration = ParseTypeDeclaration(sourceCode);
		List<DiagnosticInfo> diagnostics = [];

		// Act
		(RequestBindingTypeEnum requestType, string? name)? result = typeDeclaration.GetRequestTypeAndName(diagnostics);

		// Assert
		result.ShouldNotBeNull();
		result.Value.requestType.ShouldBe(RequestBindingTypeEnum.FromQuery);
		result.Value.name.ShouldBeNull();
		diagnostics.ShouldBeEmpty();
	}

	#endregion

	#region Real-world Integration Tests

	[Fact]
	public void GetRequestTypeAndName_WithCompleteEndpointExample_WorksCorrectly()
	{
		// Arrange
		const string sourceCode = """
			public class CreateUserEndpoint : IEndpoint<CreateUserRequest, Results<Created<UserResponse>, BadRequest>>
			{
				public static void Configure(RouteHandlerBuilder builder)
				{
					builder
						.RequestFromBody("user")
						.Post("/api/users")
						.WithName("CreateUser")
						.WithSummary("Create a new user")
						.WithTags("Users")
						.Produces<UserResponse>(201)
						.ProducesValidationProblem();
				}
			}
			""";

		TypeDeclarationSyntax typeDeclaration = ParseTypeDeclaration(sourceCode);
		List<DiagnosticInfo> diagnostics = [];

		// Act
		(RequestBindingTypeEnum requestType, string? name)? result = typeDeclaration.GetRequestTypeAndName(diagnostics);

		// Assert
		result.ShouldNotBeNull();
		result.Value.requestType.ShouldBe(RequestBindingTypeEnum.FromBody);
		result.Value.name.ShouldBe("user");
		diagnostics.ShouldBeEmpty();
	}

	[Fact]
	public void GetRequestTypeAndName_WithQueryParameterEndpoint_WorksCorrectly()
	{
		// Arrange
		const string sourceCode = """
			public class SearchUsersEndpoint : IEndpoint<SearchRequest, Ok<List<UserResponse>>>
			{
				public static void Configure(RouteHandlerBuilder builder)
				{
					builder
						.Get("/api/users/search")
						.RequestFromQuery()
						.WithName("SearchUsers")
						.WithSummary("Search users by criteria")
						.WithTags("Users", "Search")
						.Produces<List<UserResponse>>(200);
				}
			}
			""";

		TypeDeclarationSyntax typeDeclaration = ParseTypeDeclaration(sourceCode);
		List<DiagnosticInfo> diagnostics = [];

		// Act
		(RequestBindingTypeEnum requestType, string? name)? result = typeDeclaration.GetRequestTypeAndName(diagnostics);

		// Assert
		result.ShouldNotBeNull();
		result.Value.requestType.ShouldBe(RequestBindingTypeEnum.FromQuery);
		result.Value.name.ShouldBeNull();
		diagnostics.ShouldBeEmpty();
	}

	#endregion

	#region Helper Methods

	static TypeDeclarationSyntax ParseTypeDeclaration(string sourceCode)
	{
		SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
		CompilationUnitSyntax root = syntaxTree.GetCompilationUnitRoot();
		return root.DescendantNodes().OfType<TypeDeclarationSyntax>().First();
	}

	#endregion
}
