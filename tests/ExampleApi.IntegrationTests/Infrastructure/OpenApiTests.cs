using System.Text.Json;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;

namespace ExampleApi.IntegrationTests.Infrastructure;

public partial class OpenApiTests : IClassFixture<ExampleApiWebApplicationFactory>
{
	readonly HttpClient _client;

	public OpenApiTests(ExampleApiWebApplicationFactory factory)
	{
		_client = factory.CreateClient();
	}

	[Fact]
	public async Task OpenApiJson_ReturnsValidResponse()
	{
		// Act
		HttpResponseMessage response = await _client.GetAsync("/openapi/v1.json", TestContext.Current.CancellationToken);

		// Assert
		await Verify(response)
			.IgnoreMember("Content-Length");
	}

	[Fact]
	public async Task OpenApiJson_ReturnsValidOpenApiDocument()
	{
		// Act
		string json = await _client.GetStringAsync("/openapi/v1.json", TestContext.Current.CancellationToken);
		ValidationRuleSet rules = ValidationRuleSet.GetDefaultRuleSet();
		(OpenApiDocument? document, OpenApiDiagnostic? diagnostic) = OpenApiDocument.Parse(
			json,
			"json",
			new OpenApiReaderSettings { RuleSet = rules });

		// Assert
		OpenApiDiagnostic validDiagnostic = diagnostic.ShouldNotBeNull();
		validDiagnostic.Errors.ShouldBeEmpty();
		OpenApiDocument validDocument = document.ShouldNotBeNull();
		validDocument.Validate(rules).ShouldBeEmpty();
	}

	[Fact]
	public async Task OpenApiJson_RepresentsNullableEndpointResponse()
	{
		// Act
		string json = await _client.GetStringAsync("/openapi/v1.json", TestContext.Current.CancellationToken);
		using JsonDocument document = JsonDocument.Parse(json);
		JsonElement responseSchema = document.RootElement
			.GetProperty("paths")
			.GetProperty("/api/v1/todos/{id}")
			.GetProperty("patch")
			.GetProperty("responses")
			.GetProperty("200")
			.GetProperty("content")
			.GetProperty("application/json")
			.GetProperty("schema");

		// Assert
		JsonElement oneOf = responseSchema.GetProperty("oneOf");
		oneOf.GetArrayLength().ShouldBe(2);
		oneOf[0].GetProperty("$ref").GetString().ShouldBe("#/components/schemas/ExampleApi.Endpoints.Todos.Patch.ResponseModel");

		JsonElement nullableSchemaEnum = oneOf[1].GetProperty("enum");
		nullableSchemaEnum.GetArrayLength().ShouldBe(1);
		nullableSchemaEnum[0].ValueKind.ShouldBe(JsonValueKind.Null);
	}

	[Fact]
	public async Task OpenApiJson_RepresentsRequiredBodyAndValidationNullability()
	{
		// Act
		string json = await _client.GetStringAsync("/openapi/v1.json", TestContext.Current.CancellationToken);
		using JsonDocument document = JsonDocument.Parse(json);

		JsonElement requestBody = document.RootElement
			.GetProperty("paths")
			.GetProperty("/api/v1/validation/DataValidation")
			.GetProperty("post")
			.GetProperty("requestBody");

		JsonElement properties = document.RootElement
			.GetProperty("components")
			.GetProperty("schemas")
			.GetProperty("ExampleApi.Endpoints.Validation.PostDataAnnotationsFromBody.RequestModel")
			.GetProperty("properties");

		// Assert
		requestBody.GetProperty("required").GetBoolean().ShouldBeTrue();

		JsonElement requiredString = properties.GetProperty("allBuiltInStringValidators");
		requiredString.TryGetProperty("nullable", out _).ShouldBeFalse();

		JsonElement requiredNumber = properties.GetProperty("allBuiltInNumberValidators");
		requiredNumber.TryGetProperty("nullable", out _).ShouldBeFalse();
		requiredNumber.TryGetProperty("minLength", out _).ShouldBeFalse();
		requiredNumber.TryGetProperty("maxLength", out _).ShouldBeFalse();
	}

	[Fact]
	public async Task OpenApiJson_RepresentsDecimalsWithoutDoubleFormat()
	{
		// Act
		string json = await _client.GetStringAsync("/openapi/v1.json", TestContext.Current.CancellationToken);
		using JsonDocument document = JsonDocument.Parse(json);
		JsonElement schemas = document.RootElement
			.GetProperty("components")
			.GetProperty("schemas");

		// Assert
		foreach (string schemaName in new[]
		{
			"ExampleApi.Endpoints.TypeExamples.PostFromBody.RequestModel",
			"ExampleApi.Endpoints.TypeExamples.PostFromForm.RequestModel"
		})
		{
			JsonElement properties = schemas.GetProperty(schemaName).GetProperty("properties");
			AssertDecimalSchema(properties.GetProperty("decimalValue"), nullable: false);
			AssertDecimalSchema(properties.GetProperty("nullableDecimalValue"), nullable: true);
		}
	}

	static void AssertDecimalSchema(JsonElement schema, bool nullable)
	{
		JsonElement type = schema.GetProperty("type");
		if (nullable)
		{
			type.ValueKind.ShouldBe(JsonValueKind.Array);
			type.GetArrayLength().ShouldBe(2);
			type[0].GetString().ShouldBe("null");
			type[1].GetString().ShouldBe("number");
		}
		else
		{
			type.GetString().ShouldBe("number");
		}

		schema.TryGetProperty("format", out _).ShouldBeFalse();
		schema.TryGetProperty("nullable", out _).ShouldBeFalse();
	}
}
