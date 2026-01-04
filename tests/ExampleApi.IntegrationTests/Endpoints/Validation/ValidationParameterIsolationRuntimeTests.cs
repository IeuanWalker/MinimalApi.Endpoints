using ExampleApi.IntegrationTests.Infrastructure;
using Shouldly;
using System.Net;

namespace ExampleApi.IntegrationTests.Endpoints.Validation;

/// <summary>
/// Runtime validation tests that verify actual HTTP request validation behavior for endpoints
/// with the same parameter names but different validation rules.
/// These tests confirm that FluentValidation rules are properly isolated per endpoint.
/// </summary>
public class ValidationParameterIsolationRuntimeTests : IClassFixture<ExampleApiWebApplicationFactory>
{
	readonly ExampleApiWebApplicationFactory _factory;
	readonly HttpClient _client;

	public ValidationParameterIsolationRuntimeTests(ExampleApiWebApplicationFactory factory)
	{
		_factory = factory;
		_client = _factory.CreateClient();
	}

	[Theory]
	[InlineData("tests")]      // 5 characters - should pass for EndpointA (minLength=5)
	[InlineData("testing")]    // 7 characters - should pass for EndpointA
	[InlineData("testinglonger")] // 14 characters - should pass for EndpointA
	public async Task EndpointA_WithValidNameLengths_ReturnsOk(string name)
	{
		// Act
		HttpResponseMessage response = await _client.GetAsync($"/api/v1/validation/EndpointA?Name={name}");

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.OK, $"EndpointA should accept name '{name}' with length {name.Length} (minLength=5)");
	}

	[Theory]
	[InlineData("a")]          // 1 character - should fail for EndpointA (minLength=5)
	[InlineData("test")]       // 4 characters - should fail for EndpointA (minLength=5)
	public async Task EndpointA_WithTooShortName_ReturnsBadRequest(string name)
	{
		// Act
		HttpResponseMessage response = await _client.GetAsync($"/api/v1/validation/EndpointA?Name={name}");

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.BadRequest, $"EndpointA should reject name '{name}' with length {name.Length} (minLength=5)");
	}

	[Theory]
	[InlineData("testinglonger")] // 14 characters - should pass for EndpointB (minLength=10)
	[InlineData("testingten")]    // 10 characters - should pass for EndpointB (minLength=10)
	public async Task EndpointB_WithValidNameLengths_ReturnsOk(string name)
	{
		// Act
		HttpResponseMessage response = await _client.GetAsync($"/api/v1/validation/EndpointB?Name={name}");

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.OK, $"EndpointB should accept name '{name}' with length {name.Length} (minLength=10)");
	}

	[Theory]
	[InlineData("test")]       // 4 characters - should fail for EndpointB (minLength=10)
	[InlineData("tests")]      // 5 characters - should fail for EndpointB (minLength=10)
	[InlineData("testing")]    // 7 characters - should fail for EndpointB (minLength=10)
	public async Task EndpointB_WithTooShortName_ReturnsBadRequest(string name)
	{
		// Act
		HttpResponseMessage response = await _client.GetAsync($"/api/v1/validation/EndpointB?Name={name}");

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.BadRequest, $"EndpointB should reject name '{name}' with length {name.Length} (minLength=10)");
	}

	[Fact]
	public async Task EndpointA_AcceptsLengthThatEndpointBRejects()
	{
		// This is the key test - a name with length 7 should:
		// - Pass for EndpointA (minLength=5)
		// - Fail for EndpointB (minLength=10)
		// This proves the validation rules are isolated per endpoint

		string name = "testing"; // 7 characters

		// Act
		HttpResponseMessage responseA = await _client.GetAsync($"/api/v1/validation/EndpointA?Name={name}");
		HttpResponseMessage responseB = await _client.GetAsync($"/api/v1/validation/EndpointB?Name={name}");

		// Assert
		responseA.StatusCode.ShouldBe(HttpStatusCode.OK, $"EndpointA should accept '{name}' (length {name.Length} >= 5)");
		responseB.StatusCode.ShouldBe(HttpStatusCode.BadRequest, $"EndpointB should reject '{name}' (length {name.Length} < 10)");
	}

	[Fact]
	public async Task EndpointA_WithMissingRequiredParameter_ReturnsBadRequest()
	{
		// Act - Call without the required Name parameter
		HttpResponseMessage response = await _client.GetAsync("/api/v1/validation/EndpointA");

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.BadRequest, "Missing required parameter should return BadRequest");
	}

	[Fact]
	public async Task EndpointB_WithMissingRequiredParameter_ReturnsBadRequest()
	{
		// Act - Call without the required Name parameter
		HttpResponseMessage response = await _client.GetAsync("/api/v1/validation/EndpointB");

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.BadRequest, "Missing required parameter should return BadRequest");
	}

	[Fact]
	public async Task BothEndpoints_AreIndependent_NoValidationCrossContamination()
	{
		// This test ensures that calling one endpoint doesn't affect the other
		// by verifying both can be called successfully with their respective valid inputs

		// Act - Use names that meet each endpoint's minimum length requirement
		HttpResponseMessage responseA = await _client.GetAsync("/api/v1/validation/EndpointA?Name=TestA"); // 5 chars - OK for A
		HttpResponseMessage responseB = await _client.GetAsync("/api/v1/validation/EndpointB?Name=TestBLonger"); // 11 chars - OK for B

		// Assert
		responseA.StatusCode.ShouldBe(HttpStatusCode.OK, "EndpointA should process independently");
		responseB.StatusCode.ShouldBe(HttpStatusCode.OK, "EndpointB should process independently");
	}
}
