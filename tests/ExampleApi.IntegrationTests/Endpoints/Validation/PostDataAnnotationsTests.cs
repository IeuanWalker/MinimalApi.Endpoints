using System.Net.Http.Json;
using ExampleApi.Endpoints.Validation.PostDataAnnotationsFromBody;

namespace ExampleApi.IntegrationTests.Endpoints.Validation;

public class PostDataAnnotationsTests : IClassFixture<ExampleApiWebApplicationFactory>
{
	readonly ExampleApiWebApplicationFactory _factory;
	readonly HttpClient _client;

	public PostDataAnnotationsTests(ExampleApiWebApplicationFactory factory)
	{
		_factory = factory;
		_client = _factory.CreateClient();
	}

	[Fact]
	public async Task PostDataAnnotations_WithAllBuiltInStringValidators_ReturnsBadRequest()
	{
		// Arrange
		RequestModel request = new()
		{
			RequiredString = "test",
			RequiredInt = 1,
			StringLength = "test",
			StringMin = "test",
			ListMin = ["test", "test2", "test3"],
			StringMax = "test",
			ListMax = ["test"],
			RangeInt = 15,
			RangeDateTime = new DateTime(2000, 1, 1),
			StringPattern = "ABC123",
			StringEmail = "test@example.com",
			StringPhoneNumber = "1234567890",
			StringUrl = "https://example.com",
			Compare1 = "match",
			Compare2 = "match",
			StringCreditCard = "4111111111111111",
			IntMin = 10,
			IntMax = 50,
			IntRange = 50,
			DoubleMin = 1.0,
			DoubleMax = 50.0,
			DoubleRange = 50.0,
			ListStringMinCount = ["x"],
			ListStringMaxCount = [],
			ListStringRangeCount = ["x"],
			ListIntMinCount = [1],
			ListIntMaxCount = [],
			ListIntRangeCount = [1],
			CustomValidatedProperty = "ok-test",
			ThrowsCustomValidationProperty = "test",
			NestedObject = new NestedObjectModel
			{
				AllBuiltInStringValidators = "ok12",
				AllBuiltInNumberValidators = 15,
				RequiredString = "test",
				RequiredInt = 1,
				StringLength = "test",
				StringMin = "test",
				ListMin = ["test", "test2", "test3"],
				StringMax = "test",
				ListMax = ["test"],
				RangeInt = 15,
				RangeDateTime = new DateTime(2000, 1, 1),
				StringPattern = "ABC123",
				StringEmail = "ok12@example.com",
				StringPhoneNumber = "1234567890",
				StringUrl = "https://example.com",
				Compare1 = "match",
				Compare2 = "match",
				StringCreditCard = "4111111111111111",
				IntMin = 10,
				IntMax = 50,
				IntRange = 50,
				DoubleMin = 1.0,
				DoubleMax = 50.0,
				DoubleRange = 50.0,
				ListStringMinCount = ["x"],
				ListStringMaxCount = [],
				ListStringRangeCount = ["x"],
				ListIntMinCount = [1],
				ListIntMaxCount = [],
				ListIntRangeCount = [1],
				CustomValidatedProperty = "ok-test",
				CustomValidationWithDefaultMessage = "test",
				CustomValidationWithoutDefaultMessage = "test",
				CustomValidationWithoutDefaultMessageSetManually = "test",
				CustomValidationWithDefaultMessageOverrideMessage = "test"
			},
			AllBuiltInStringValidators = "ok12",
			AllBuiltInNumberValidators = 15,
			CustomValidationWithDefaultMessage = "test",
			CustomValidationWithoutDefaultMessage = "test",
			CustomValidationWithoutDefaultMessageSetManually = "test",
			CustomValidationWithDefaultMessageOverrideMessage = "test"
		};

		// Act
		HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/validation/DataValidation", request, TestContext.Current.CancellationToken);

		// Assert
		await Verify(response)
			.IgnoreMembers("Content-Length", "traceId");
	}

	[Fact]
	public async Task PostDataAnnotations_WithMissingRequiredString_ReturnsBadRequest()
	{
		// Arrange
		RequestModel request = new()
		{
			RequiredString = "", // Empty string (required)
			RequiredInt = 1,
			StringLength = "test",
			StringMin = "test",
			ListMin = ["test"],
			StringMax = "test",
			ListMax = ["test"],
			RangeInt = 15,
			RangeDateTime = new DateTime(2000, 1, 1),
			StringPattern = "ABC123",
			StringEmail = "test@example.com",
			StringPhoneNumber = "1234567890",
			StringUrl = "https://example.com",
			Compare1 = "match",
			Compare2 = "match",
			StringCreditCard = "4111111111111111",
			IntMin = 10,
			IntMax = 50,
			IntRange = 50,
			DoubleMin = 1.0,
			DoubleMax = 50.0,
			DoubleRange = 50.0,
			ListStringMinCount = ["x"],
			ListStringMaxCount = [],
			ListStringRangeCount = ["x"],
			ListIntMinCount = [1],
			ListIntMaxCount = [],
			ListIntRangeCount = [1],
			CustomValidatedProperty = "ok-test",
			ThrowsCustomValidationProperty = "test",
			NestedObject = new NestedObjectModel
			{
				RequiredString = "test",
				RequiredInt = 1,
				StringLength = "test",
				StringMin = "test",
				ListMin = ["test"],
				StringMax = "test",
				ListMax = ["test"],
				RangeInt = 15,
				RangeDateTime = new DateTime(2000, 1, 1),
				StringPattern = "ABC123",
				StringEmail = "test@example.com",
				StringPhoneNumber = "1234567890",
				StringUrl = "https://example.com",
				Compare1 = "match",
				Compare2 = "match",
				StringCreditCard = "4111111111111111",
				IntMin = 10,
				IntMax = 50,
				IntRange = 50,
				DoubleMin = 1.0,
				DoubleMax = 50.0,
				DoubleRange = 50.0,
				ListStringMinCount = ["x"],
				ListStringMaxCount = [],
				ListStringRangeCount = ["x"],
				ListIntMinCount = [1],
				ListIntMaxCount = [],
				ListIntRangeCount = [1],
				CustomValidatedProperty = "ok-test",
				CustomValidationWithDefaultMessage = "test",
				CustomValidationWithoutDefaultMessage = "test",
				CustomValidationWithoutDefaultMessageSetManually = "test",
				CustomValidationWithDefaultMessageOverrideMessage = "test"
			},
			CustomValidationWithDefaultMessage = "test",
			CustomValidationWithoutDefaultMessage = "test",
			CustomValidationWithoutDefaultMessageSetManually = "test",
			CustomValidationWithDefaultMessageOverrideMessage = "test"
		};

		// Act
		HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/validation/DataValidation", request, TestContext.Current.CancellationToken);

		// Assert
		await Verify(response)
			.IgnoreMembers("Content-Length", "traceId");
	}

	[Fact]
	public async Task PostDataAnnotations_WithStringTooShort_ReturnsBadRequest()
	{
		// Arrange
		RequestModel request = new()
		{
			RequiredString = "test",
			RequiredInt = 1,
			StringLength = "ab", // Too short (min length 3)
			StringMin = "test",
			ListMin = ["test"],
			StringMax = "test",
			ListMax = ["test"],
			RangeInt = 15,
			RangeDateTime = new DateTime(2000, 1, 1),
			StringPattern = "ABC123",
			StringEmail = "test@example.com",
			StringPhoneNumber = "1234567890",
			StringUrl = "https://example.com",
			Compare1 = "match",
			Compare2 = "match",
			StringCreditCard = "4111111111111111",
			IntMin = 10,
			IntMax = 50,
			IntRange = 50,
			DoubleMin = 1.0,
			DoubleMax = 50.0,
			DoubleRange = 50.0,
			ListStringMinCount = ["x"],
			ListStringMaxCount = [],
			ListStringRangeCount = ["x"],
			ListIntMinCount = [1],
			ListIntMaxCount = [],
			ListIntRangeCount = [1],
			CustomValidatedProperty = "ok-test",
			ThrowsCustomValidationProperty = "test",
			NestedObject = new NestedObjectModel
			{
				RequiredString = "test",
				RequiredInt = 1,
				StringLength = "test",
				StringMin = "test",
				ListMin = ["test"],
				StringMax = "test",
				ListMax = ["test"],
				RangeInt = 15,
				RangeDateTime = new DateTime(2000, 1, 1),
				StringPattern = "ABC123",
				StringEmail = "test@example.com",
				StringPhoneNumber = "1234567890",
				StringUrl = "https://example.com",
				Compare1 = "match",
				Compare2 = "match",
				StringCreditCard = "4111111111111111",
				IntMin = 10,
				IntMax = 50,
				IntRange = 50,
				DoubleMin = 1.0,
				DoubleMax = 50.0,
				DoubleRange = 50.0,
				ListStringMinCount = ["x"],
				ListStringMaxCount = [],
				ListStringRangeCount = ["x"],
				ListIntMinCount = [1],
				ListIntMaxCount = [],
				ListIntRangeCount = [1],
				CustomValidatedProperty = "ok-test",
				CustomValidationWithDefaultMessage = "test",
				CustomValidationWithoutDefaultMessage = "test",
				CustomValidationWithoutDefaultMessageSetManually = "test",
				CustomValidationWithDefaultMessageOverrideMessage = "test"
			},
			CustomValidationWithDefaultMessage = "test",
			CustomValidationWithoutDefaultMessage = "test",
			CustomValidationWithoutDefaultMessageSetManually = "test",
			CustomValidationWithDefaultMessageOverrideMessage = "test"
		};

		// Act
		HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/validation/DataValidation", request, TestContext.Current.CancellationToken);

		// Assert
		await Verify(response)
			.IgnoreMembers("Content-Length", "traceId");
	}

	[Fact]
	public async Task PostDataAnnotations_WithInvalidRange_ReturnsBadRequest()
	{
		// Arrange
		RequestModel request = new()
		{
			RequiredString = "test",
			RequiredInt = 1,
			StringLength = "test",
			StringMin = "test",
			ListMin = ["test"],
			StringMax = "test",
			ListMax = ["test"],
			RangeInt = 5, // Out of range (min 10, max 25)
			RangeDateTime = new DateTime(2000, 1, 1),
			StringPattern = "ABC123",
			StringEmail = "test@example.com",
			StringPhoneNumber = "1234567890",
			StringUrl = "https://example.com",
			Compare1 = "match",
			Compare2 = "match",
			StringCreditCard = "4111111111111111",
			IntMin = 10,
			IntMax = 50,
			IntRange = 50,
			DoubleMin = 1.0,
			DoubleMax = 50.0,
			DoubleRange = 50.0,
			ListStringMinCount = ["x"],
			ListStringMaxCount = [],
			ListStringRangeCount = ["x"],
			ListIntMinCount = [1],
			ListIntMaxCount = [],
			ListIntRangeCount = [1],
			CustomValidatedProperty = "ok-test",
			ThrowsCustomValidationProperty = "test",
			NestedObject = new NestedObjectModel
			{
				RequiredString = "test",
				RequiredInt = 1,
				StringLength = "test",
				StringMin = "test",
				ListMin = ["test"],
				StringMax = "test",
				ListMax = ["test"],
				RangeInt = 15,
				RangeDateTime = new DateTime(2000, 1, 1),
				StringPattern = "ABC123",
				StringEmail = "test@example.com",
				StringPhoneNumber = "1234567890",
				StringUrl = "https://example.com",
				Compare1 = "match",
				Compare2 = "match",
				StringCreditCard = "4111111111111111",
				IntMin = 10,
				IntMax = 50,
				IntRange = 50,
				DoubleMin = 1.0,
				DoubleMax = 50.0,
				DoubleRange = 50.0,
				ListStringMinCount = ["x"],
				ListStringMaxCount = [],
				ListStringRangeCount = ["x"],
				ListIntMinCount = [1],
				ListIntMaxCount = [],
				ListIntRangeCount = [1],
				CustomValidatedProperty = "ok-test",
				CustomValidationWithDefaultMessage = "test",
				CustomValidationWithoutDefaultMessage = "test",
				CustomValidationWithoutDefaultMessageSetManually = "test",
				CustomValidationWithDefaultMessageOverrideMessage = "test"
			},
			CustomValidationWithDefaultMessage = "test",
			CustomValidationWithoutDefaultMessage = "test",
			CustomValidationWithoutDefaultMessageSetManually = "test",
			CustomValidationWithDefaultMessageOverrideMessage = "test"
		};

		// Act
		HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/validation/DataValidation", request, TestContext.Current.CancellationToken);

		// Assert
		await Verify(response)
			.IgnoreMembers("Content-Length", "traceId");
	}

	[Fact]
	public async Task PostDataAnnotations_WithInvalidEmail_ReturnsBadRequest()
	{
		// Arrange
		RequestModel request = new()
		{
			RequiredString = "test",
			RequiredInt = 1,
			StringLength = "test",
			StringMin = "test",
			ListMin = ["test"],
			StringMax = "test",
			ListMax = ["test"],
			RangeInt = 15,
			RangeDateTime = new DateTime(2000, 1, 1),
			StringPattern = "ABC123",
			StringEmail = "not-an-email", // Invalid email
			StringPhoneNumber = "1234567890",
			StringUrl = "https://example.com",
			Compare1 = "match",
			Compare2 = "match",
			StringCreditCard = "4111111111111111",
			IntMin = 10,
			IntMax = 50,
			IntRange = 50,
			DoubleMin = 1.0,
			DoubleMax = 50.0,
			DoubleRange = 50.0,
			ListStringMinCount = ["x"],
			ListStringMaxCount = [],
			ListStringRangeCount = ["x"],
			ListIntMinCount = [1],
			ListIntMaxCount = [],
			ListIntRangeCount = [1],
			CustomValidatedProperty = "ok-test",
			ThrowsCustomValidationProperty = "test",
			NestedObject = new NestedObjectModel
			{
				RequiredString = "test",
				RequiredInt = 1,
				StringLength = "test",
				StringMin = "test",
				ListMin = ["test"],
				StringMax = "test",
				ListMax = ["test"],
				RangeInt = 15,
				RangeDateTime = new DateTime(2000, 1, 1),
				StringPattern = "ABC123",
				StringEmail = "test@example.com",
				StringPhoneNumber = "1234567890",
				StringUrl = "https://example.com",
				Compare1 = "match",
				Compare2 = "match",
				StringCreditCard = "4111111111111111",
				IntMin = 10,
				IntMax = 50,
				IntRange = 50,
				DoubleMin = 1.0,
				DoubleMax = 50.0,
				DoubleRange = 50.0,
				ListStringMinCount = ["x"],
				ListStringMaxCount = [],
				ListStringRangeCount = ["x"],
				ListIntMinCount = [1],
				ListIntMaxCount = [],
				ListIntRangeCount = [1],
				CustomValidatedProperty = "ok-test",
				CustomValidationWithDefaultMessage = "test",
				CustomValidationWithoutDefaultMessage = "test",
				CustomValidationWithoutDefaultMessageSetManually = "test",
				CustomValidationWithDefaultMessageOverrideMessage = "test"
			},
			CustomValidationWithDefaultMessage = "test",
			CustomValidationWithoutDefaultMessage = "test",
			CustomValidationWithoutDefaultMessageSetManually = "test",
			CustomValidationWithDefaultMessageOverrideMessage = "test"
		};

		// Act
		HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/validation/DataValidation", request, TestContext.Current.CancellationToken);

		// Assert
		await Verify(response)
			.IgnoreMembers("Content-Length", "traceId");
	}
}
