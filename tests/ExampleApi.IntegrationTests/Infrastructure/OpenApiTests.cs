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
}
