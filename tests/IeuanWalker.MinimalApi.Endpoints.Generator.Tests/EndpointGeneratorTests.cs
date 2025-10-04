using Shouldly;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace IeuanWalker.MinimalApi.Endpoints.Generator.Tests;

public class EndpointGeneratorTests
{
	[Fact]
	public void GeneratesEndpointExtensions_ForSimpleEndpoint()
	{
		// Arrange
		string source = """
			using IeuanWalker.MinimalApi.Endpoints;
			using Microsoft.AspNetCore.Builder;
			using Microsoft.AspNetCore.Http;

			namespace TestApp.Endpoints;

			public class GetUserEndpoint : IEndpoint<RequestModel, string>
			{
				public static void Configure(RouteHandlerBuilder builder)
				{
					builder.Get("/users/{id}");
				}

				public Task<string> Handle(RequestModel request, CancellationToken ct)
				{
					return Task.FromResult($"User {request.Id}");
				}
			}

			public class RequestModel
			{
				public int Id { get; set; }
			}
			""";

		// Act & Assert
		VerifySourceGenerator(source, result =>
		{
			result.GeneratedTrees.Length.ShouldBe(1);
			result.Diagnostics.ShouldBeEmpty();
			
			string generatedCode = result.GeneratedTrees[0].ToString();
			generatedCode.ShouldContain("AddEndpointsFromTestApp");
			generatedCode.ShouldContain("MapEndpointsFromTestApp");
			generatedCode.ShouldContain("GetUserEndpoint");
		});
	}

	[Fact]
	public void GeneratesEndpointExtensions_ForEndpointWithoutRequest()
	{
		// Arrange
		string source = """
			using IeuanWalker.MinimalApi.Endpoints;
			using Microsoft.AspNetCore.Builder;
			using Microsoft.AspNetCore.Http;

			namespace TestApp.Endpoints;

			public class GetHealthEndpoint : IEndpointWithoutRequest<string>
			{
				public static void Configure(RouteHandlerBuilder builder)
				{
					builder.Get("/health");
				}

				public Task<string> Handle(CancellationToken ct)
				{
					return Task.FromResult("Healthy");
				}
			}
			""";

		// Act & Assert
		VerifySourceGenerator(source, result =>
		{
			result.GeneratedTrees.Length.ShouldBe(1);
			result.Diagnostics.ShouldBeEmpty();
			
			string generatedCode = result.GeneratedTrees[0].ToString();
			generatedCode.ShouldContain("GetHealthEndpoint");
			generatedCode.ShouldContain(".Get(\"/health\"");
		});
	}

	[Fact]
	public void GeneratesEndpointExtensions_ForEndpointWithoutResponse()
	{
		// Arrange
		string source = """
			using IeuanWalker.MinimalApi.Endpoints;
			using Microsoft.AspNetCore.Builder;
			using Microsoft.AspNetCore.Http;

			namespace TestApp.Endpoints;

			public class DeleteUserEndpoint : IEndpointWithoutResponse<RequestModel>
			{
				public static void Configure(RouteHandlerBuilder builder)
				{
					builder.Delete("/users/{id}");
				}

				public Task Handle(RequestModel request, CancellationToken ct)
				{
					return Task.CompletedTask;
				}
			}

			public class RequestModel
			{
				public int Id { get; set; }
			}
			""";

		// Act & Assert
		VerifySourceGenerator(source, result =>
		{
			result.GeneratedTrees.Length.ShouldBe(1);
			result.Diagnostics.ShouldBeEmpty();
			
			string generatedCode = result.GeneratedTrees[0].ToString();
			generatedCode.ShouldContain("DeleteUserEndpoint");
			generatedCode.ShouldContain(".Delete(\"/users/{id}\"");
		});
	}

	[Fact]
	public void GeneratesEndpointExtensions_ForEndpointWithoutRequestOrResponse()
	{
		// Arrange
		string source = """
			using IeuanWalker.MinimalApi.Endpoints;
			using Microsoft.AspNetCore.Builder;
			using Microsoft.AspNetCore.Http;

			namespace TestApp.Endpoints;

			public class PingEndpoint : IEndpoint
			{
				public static void Configure(RouteHandlerBuilder builder)
				{
					builder.Get("/ping");
				}

				public Task Handle(CancellationToken ct)
				{
					return Task.CompletedTask;
				}
			}
			""";

		// Act & Assert
		VerifySourceGenerator(source, result =>
		{
			result.GeneratedTrees.Length.ShouldBe(1);
			result.Diagnostics.ShouldBeEmpty();
			
			string generatedCode = result.GeneratedTrees[0].ToString();
			generatedCode.ShouldContain("PingEndpoint");
			generatedCode.ShouldContain(".Get(\"/ping\"");
		});
	}

	[Fact]
	public void GeneratesEndpointExtensions_WithEndpointGroup()
	{
		// Arrange
		string source = """
			using IeuanWalker.MinimalApi.Endpoints;
			using Microsoft.AspNetCore.Builder;
			using Microsoft.AspNetCore.Http;

			namespace TestApp.Endpoints;

			public class UserEndpointGroup : IEndpointGroup
			{
				public static RouteGroupBuilder Configure(WebApplication app)
				{
					return app.MapGroup("/users");
				}
			}

			public class GetUserEndpoint : IEndpoint<RequestModel, string>
			{
				public static void Configure(RouteHandlerBuilder builder)
				{
					builder.Group<UserEndpointGroup>().Get("/{id}");
				}

				public Task<string> Handle(RequestModel request, CancellationToken ct)
				{
					return Task.FromResult($"User {request.Id}");
				}
			}

			public class RequestModel
			{
				public int Id { get; set; }
			}
			""";

		// Act & Assert
		VerifySourceGenerator(source, result =>
		{
			result.GeneratedTrees.Length.ShouldBe(1);
			result.Diagnostics.ShouldBeEmpty();
			
			string generatedCode = result.GeneratedTrees[0].ToString();
			generatedCode.ShouldContain("UserEndpointGroup");
			generatedCode.ShouldContain("GetUserEndpoint");
		});
	}

	[Fact]
	public void GeneratesEndpointExtensions_WithValidator()
	{
		// Arrange
		string source = """
			using IeuanWalker.MinimalApi.Endpoints;
			using Microsoft.AspNetCore.Builder;
			using Microsoft.AspNetCore.Http;
			using FluentValidation;

			namespace TestApp.Endpoints;

			public class CreateUserEndpoint : IEndpoint<RequestModel, string>
			{
				public static void Configure(RouteHandlerBuilder builder)
				{
					builder.Post("/users");
				}

				public Task<string> Handle(RequestModel request, CancellationToken ct)
				{
					return Task.FromResult($"Created user {request.Name}");
				}
			}

			public class RequestModel
			{
				public string Name { get; set; } = string.Empty;
			}

			public class RequestModelValidator : Validator<RequestModel>
			{
				public RequestModelValidator()
				{
					RuleFor(x => x.Name).NotEmpty();
				}
			}
			""";

		// Act & Assert
		VerifySourceGenerator(source, result =>
		{
			result.GeneratedTrees.Length.ShouldBe(1);
			result.Diagnostics.ShouldBeEmpty();
			
			string generatedCode = result.GeneratedTrees[0].ToString();
			generatedCode.ShouldContain("CreateUserEndpoint");
			generatedCode.ShouldContain("FluentValidationFilter");
			generatedCode.ShouldContain("RequestModelValidator");
		});
	}

	[Fact]
	public void GeneratesEndpointExtensions_ForMultipleEndpoints()
	{
		// Arrange
		string source = """
			using IeuanWalker.MinimalApi.Endpoints;
			using Microsoft.AspNetCore.Builder;
			using Microsoft.AspNetCore.Http;

			namespace TestApp.Endpoints;

			public class GetUsersEndpoint : IEndpointWithoutRequest<string>
			{
				public static void Configure(RouteHandlerBuilder builder)
				{
					builder.Get("/users");
				}

				public Task<string> Handle(CancellationToken ct)
				{
					return Task.FromResult("Users list");
				}
			}

			public class CreateUserEndpoint : IEndpoint<CreateRequest, string>
			{
				public static void Configure(RouteHandlerBuilder builder)
				{
					builder.Post("/users");
				}

				public Task<string> Handle(CreateRequest request, CancellationToken ct)
				{
					return Task.FromResult($"Created {request.Name}");
				}
			}

			public class CreateRequest
			{
				public string Name { get; set; } = string.Empty;
			}
			""";

		// Act & Assert
		VerifySourceGenerator(source, result =>
		{
			result.GeneratedTrees.Length.ShouldBe(1);
			result.Diagnostics.ShouldBeEmpty();
			
			string generatedCode = result.GeneratedTrees[0].ToString();
			generatedCode.ShouldContain("GetUsersEndpoint");
			generatedCode.ShouldContain("CreateUserEndpoint");
		});
	}

	[Fact]
	public void GeneratesDiagnostic_WhenNoHttpVerbDefined()
	{
		// Arrange
		string source = """
			using IeuanWalker.MinimalApi.Endpoints;
			using Microsoft.AspNetCore.Builder;
			using Microsoft.AspNetCore.Http;

			namespace TestApp.Endpoints;

			public class InvalidEndpoint : IEndpoint<RequestModel, string>
			{
				public static void Configure(RouteHandlerBuilder builder)
				{
					// Missing HTTP verb
				}

				public Task<string> Handle(RequestModel request, CancellationToken ct)
				{
					return Task.FromResult("Result");
				}
			}

			public class RequestModel
			{
				public int Id { get; set; }
			}
			""";

		// Act & Assert
		VerifySourceGenerator(source, result =>
		{
			result.Diagnostics.ShouldNotBeEmpty();
			result.Diagnostics.ShouldContain(d => d.Id == "MINAPI001");
		});
	}

	private static void VerifySourceGenerator(string source, Action<GeneratorDriverRunResult> assertions)
	{
		// Create compilation
		SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source);

		// Reference assemblies
		IEnumerable<MetadataReference> references = [
			MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
			MetadataReference.CreateFromFile(typeof(IEndpoint).Assembly.Location),
			MetadataReference.CreateFromFile(typeof(Microsoft.AspNetCore.Builder.EndpointRouteBuilderExtensions).Assembly.Location),
			MetadataReference.CreateFromFile(typeof(Microsoft.AspNetCore.Http.HttpContext).Assembly.Location),
			MetadataReference.CreateFromFile(typeof(FluentValidation.IValidator<>).Assembly.Location),
			// Add netstandard reference for basic types
			MetadataReference.CreateFromFile(Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location)!, "netstandard.dll")),
			MetadataReference.CreateFromFile(Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location)!, "System.Runtime.dll"))
		];

		CSharpCompilation compilation = CSharpCompilation.Create(
			assemblyName: "TestApp",
			syntaxTrees: [syntaxTree],
			references: references,
			options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

		// Run generator
		EndpointGenerator generator = new();
		GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
		driver = driver.RunGenerators(compilation);

		// Get the run result
		GeneratorDriverRunResult result = driver.GetRunResult();
		
		// Perform assertions
		assertions(result);
	}
}
