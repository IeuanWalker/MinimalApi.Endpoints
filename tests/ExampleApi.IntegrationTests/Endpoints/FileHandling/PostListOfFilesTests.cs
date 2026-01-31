namespace ExampleApi.IntegrationTests.Endpoints.FileHandling;

/// <summary>
/// Integration tests for PostFileHandlingListOfFilesEndpoint
/// </summary>
public class PostListOfFilesTests : IClassFixture<ExampleApiWebApplicationFactory>
{
	readonly HttpClient _client;

	public PostListOfFilesTests(ExampleApiWebApplicationFactory factory)
	{
		_client = factory.CreateClient();
	}

	[Fact]
	public async Task PostListOfFiles_WithMultipleFiles_ReturnsAllFileDetails()
	{
		// Arrange
		using MultipartFormDataContent content = new();

		using MemoryStream file1Stream = new("First file content"u8.ToArray());
		using StreamContent file1Content = new(file1Stream);
		file1Content.Headers.ContentType = new("application/pdf");
		content.Add(file1Content, "files", "document1.pdf");

		using MemoryStream file2Stream = new("Second file content"u8.ToArray());
		using StreamContent file2Content = new(file2Stream);
		file2Content.Headers.ContentType = new("text/plain");
		content.Add(file2Content, "files", "document2.txt");

		using MemoryStream file3Stream = new("Third file content"u8.ToArray());
		using StreamContent file3Content = new(file3Stream);
		file3Content.Headers.ContentType = new("image/png");
		content.Add(file3Content, "files", "image.png");

		// Act
		HttpResponseMessage response = await _client.PostAsync("/api/v1/FileHandling/ListOfFiles", content, TestContext.Current.CancellationToken);

		// Assert
		await Verify(response)
			.IgnoreMember("Content-Length");
	}

	[Fact]
	public async Task PostListOfFiles_WithSingleFile_ReturnsSingleFileDetails()
	{
		// Arrange
		using MultipartFormDataContent content = new();
		using MemoryStream fileStream = new("Single file"u8.ToArray());
		using StreamContent fileContent = new(fileStream);
		fileContent.Headers.ContentType = new("text/plain");
		content.Add(fileContent, "file", "single.txt");

		// Act
		HttpResponseMessage response = await _client.PostAsync("/api/v1/FileHandling/ListOfFiles", content, TestContext.Current.CancellationToken);

		// Assert
		await Verify(response)
			.IgnoreMember("Content-Length");
	}

	[Fact]
	public async Task PostListOfFiles_WithEmptyCollection_ReturnsEmptyArray()
	{
		// Arrange
		using MultipartFormDataContent content = new();
		// Add a dummy field to make it a valid multipart request
		using StringContent stringContent = new("");
		content.Add(stringContent, "dummy");

		// Act
		HttpResponseMessage response = await _client.PostAsync("/api/v1/FileHandling/ListOfFiles", content, TestContext.Current.CancellationToken);

		// Assert
		await Verify(response)
			.IgnoreMember("Content-Length");
	}

	[Fact]
	public async Task PostListOfFiles_WithDifferentPropertyNames_PreservesPropertyNames()
	{
		// Arrange
		using MultipartFormDataContent content = new();

		using MemoryStream file1Stream = new("File 1"u8.ToArray());
		using StreamContent file1Content = new(file1Stream);
		content.Add(file1Content, "property1", "file1.txt");

		using MemoryStream file2Stream = new("File 2"u8.ToArray());
		using StreamContent file2Content = new(file2Stream);
		content.Add(file2Content, "property2", "file2.txt");

		// Act
		HttpResponseMessage response = await _client.PostAsync("/api/v1/FileHandling/ListOfFiles", content, TestContext.Current.CancellationToken);

		// Assert
		await Verify(response)
			.IgnoreMember("Content-Length");
	}

	[Fact]
	public async Task PostListOfFiles_WithEmptyFiles_ReturnsZeroSizes()
	{
		// Arrange
		using MultipartFormDataContent content = new();

		using MemoryStream file1Stream = new([]);
		using StreamContent file1Content = new(file1Stream);
		content.Add(file1Content, "files", "empty1.txt");

		using MemoryStream file2Stream = new([]);
		using StreamContent file2Content = new(file2Stream);
		content.Add(file2Content, "files", "empty2.txt");

		// Act
		HttpResponseMessage response = await _client.PostAsync("/api/v1/FileHandling/ListOfFiles", content, TestContext.Current.CancellationToken);

		// Assert
		await Verify(response)
			.IgnoreMember("Content-Length");
	}
}
