namespace ExampleApi.IntegrationTests.Endpoints.FileHandling;

/// <summary>
/// Integration tests for PostFileHandlingSingleFileEndpoint
/// </summary>
public class PostSingleFileTests : IClassFixture<ExampleApiWebApplicationFactory>
{
	readonly HttpClient _client;

	public PostSingleFileTests(ExampleApiWebApplicationFactory factory)
	{
		_client = factory.CreateClient();
	}

	[Fact]
	public async Task PostSingleFile_WithValidFile_ReturnsFileDetails()
	{
		// Arrange
		using MultipartFormDataContent content = new();
		using MemoryStream fileStream = new("Test file content"u8.ToArray());
		using StreamContent fileContent = new(fileStream);
		fileContent.Headers.ContentType = new("application/pdf");
		content.Add(fileContent, "request", "document.pdf");

		// Act
		HttpResponseMessage response = await _client.PostAsync("/api/v1/FileHandling/SingleFile", content, TestContext.Current.CancellationToken);

		// Assert
		await Verify(response)
			.IgnoreMember("Content-Length");
	}

	[Fact]
	public async Task PostSingleFile_WithEmptyFile_ReturnsZeroSize()
	{
		// Arrange
		using MultipartFormDataContent content = new();
		using MemoryStream fileStream = new([]);
		using StreamContent fileContent = new(fileStream);
		fileContent.Headers.ContentType = new("text/plain");
		content.Add(fileContent, "request", "empty.txt");

		// Act
		HttpResponseMessage response = await _client.PostAsync("/api/v1/FileHandling/SingleFile", content, TestContext.Current.CancellationToken);

		// Assert
		await Verify(response)
			.IgnoreMember("Content-Length");
	}

	[Theory]
	[InlineData("image.png", "image/png")]
	[InlineData("data.json", "application/json")]
	[InlineData("archive.zip", "application/zip")]
	public async Task PostSingleFile_WithVariousFileTypes_ReturnsCorrectFileName(string fileName, string contentType)
	{
		// Arrange
		using MultipartFormDataContent content = new();
		using MemoryStream fileStream = new("file data"u8.ToArray());
		using StreamContent fileContent = new(fileStream);
		fileContent.Headers.ContentType = new(contentType);
		content.Add(fileContent, "request", fileName);

		// Act
		HttpResponseMessage response = await _client.PostAsync("/api/v1/FileHandling/SingleFile", content, TestContext.Current.CancellationToken);

		// Assert
		await Verify(response)
			.IgnoreMember("Content-Length");
	}

	[Fact]
	public async Task PostSingleFile_WithLargeFile_ReturnsCorrectSize()
	{
		// Arrange
		byte[] largeData = new byte[1024 * 100]; // 100KB
		Random.Shared.NextBytes(largeData);

		using MultipartFormDataContent content = new();
		using MemoryStream fileStream = new(largeData);
		using StreamContent fileContent = new(fileStream);
		fileContent.Headers.ContentType = new("application/octet-stream");
		content.Add(fileContent, "request", "largefile.bin");

		// Act
		HttpResponseMessage response = await _client.PostAsync("/api/v1/FileHandling/SingleFile", content, TestContext.Current.CancellationToken);

		// Assert
		await Verify(response)
			.IgnoreMember("Content-Length");
	}
}
