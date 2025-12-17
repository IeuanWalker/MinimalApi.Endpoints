using System.Net;
using System.Net.Http.Json;
using ListOfFiles = ExampleApi.Endpoints.FileHandling.PostListOfFiles;

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
		HttpResponseMessage response = await _client.PostAsync("/api/v1/FileHandling/ListOfFiles", content);

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.OK);
		ListOfFiles.ResponseModel[]? result = await response.Content.ReadFromJsonAsync<ListOfFiles.ResponseModel[]>();
		result.ShouldNotBeNull();
		result.Length.ShouldBe(3);

		result[0].FileName.ShouldBe("document1.pdf");
		result[0].Size.ShouldBe(18); // "First file content"

		result[1].FileName.ShouldBe("document2.txt");
		result[1].Size.ShouldBe(19); // "Second file content"

		result[2].FileName.ShouldBe("image.png");
		result[2].Size.ShouldBe(18); // "Third file content"
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
		HttpResponseMessage response = await _client.PostAsync("/api/v1/FileHandling/ListOfFiles", content);

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.OK);
		ListOfFiles.ResponseModel[]? result = await response.Content.ReadFromJsonAsync<ListOfFiles.ResponseModel[]>();
		result.ShouldNotBeNull();
		result.Length.ShouldBe(1);
		result[0].FileName.ShouldBe("single.txt");
		result[0].Size.ShouldBe(11); // "Single file"
	}

	[Fact]
	public async Task PostListOfFiles_WithEmptyCollection_ReturnsEmptyArray()
	{
		// Arrange
		using MultipartFormDataContent content = new();
		// Add a dummy field to make it a valid multipart request
		content.Add(new StringContent(""), "dummy");

		// Act
		HttpResponseMessage response = await _client.PostAsync("/api/v1/FileHandling/ListOfFiles", content);

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.OK);
		ListOfFiles.ResponseModel[]? result = await response.Content.ReadFromJsonAsync<ListOfFiles.ResponseModel[]>();
		result.ShouldNotBeNull();
		result.ShouldBeEmpty();
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
		HttpResponseMessage response = await _client.PostAsync("/api/v1/FileHandling/ListOfFiles", content);

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.OK);
		ListOfFiles.ResponseModel[]? result = await response.Content.ReadFromJsonAsync<ListOfFiles.ResponseModel[]>();
		result.ShouldNotBeNull();
		result.Length.ShouldBe(2);
		result[0].PropertyName.ShouldBe("property1");
		result[1].PropertyName.ShouldBe("property2");
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
		HttpResponseMessage response = await _client.PostAsync("/api/v1/FileHandling/ListOfFiles", content);

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.OK);
		ListOfFiles.ResponseModel[]? result = await response.Content.ReadFromJsonAsync<ListOfFiles.ResponseModel[]>();
		result.ShouldNotBeNull();
		result.Length.ShouldBe(2);
		result[0].Size.ShouldBe(0);
		result[1].Size.ShouldBe(0);
	}
}
