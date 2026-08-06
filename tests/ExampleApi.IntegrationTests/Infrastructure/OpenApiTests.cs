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
		var (document, diagnostic) = OpenApiDocument.Parse(
			json,
			"json",
			new OpenApiReaderSettings { RuleSet = rules });

		// Assert
		OpenApiDiagnostic validDiagnostic = diagnostic.ShouldNotBeNull();
		validDiagnostic.Errors.ShouldBeEmpty();
		OpenApiDocument validDocument = document.ShouldNotBeNull();
		validDocument.Validate(rules).ShouldBeEmpty();
	}
}
