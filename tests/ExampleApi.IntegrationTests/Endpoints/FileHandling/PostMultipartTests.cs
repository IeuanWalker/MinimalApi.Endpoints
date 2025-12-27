using System.Net;
using System.Net.Http.Json;
using Multipart = ExampleApi.Endpoints.FileHandling.PostMultipart;

namespace ExampleApi.IntegrationTests.Endpoints.FileHandling;

/// <summary>
/// Integration tests for PostFileHandlingMultipartEndpoint
/// </summary>
public class PostMultipartTests : IClassFixture<ExampleApiWebApplicationFactory>
{
	readonly HttpClient _client;

	public PostMultipartTests(ExampleApiWebApplicationFactory factory)
	{
		_client = factory.CreateClient();
	}

	[Fact]
	public async Task PostMultipart_WithAllFileTypes_ReturnsCompleteResponse()
	{
		// Arrange
		using MultipartFormDataContent content = CreateMultipartContent(
			someData: "Test Data",
			singleFileName: "single.pdf",
			singleFileContent: "Single file content",
			readOnlyList1Files: [("list1_file1.txt", "List1 File 1"), ("list1_file2.txt", "List1 File 2")],
			readOnlyList2Files: [("list2_file1.doc", "List2 File 1")],
			fileCollectionFiles: [("collection1.png", "Collection 1"), ("collection2.jpg", "Collection 2")]
		);

		// Act
		HttpResponseMessage response = await _client.PostAsync("/api/v1/FileHandling/Multipart", content);

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.OK);
		Multipart.ResponseModel? result = await response.Content.ReadFromJsonAsync<Multipart.ResponseModel>();
		result.ShouldNotBeNull();

		result.SomeData.ShouldBe("Test Data");
		result.TotalFileCount.ShouldBe(6);

		result.SingleFile.FileName.ShouldBe("single.pdf");
		result.SingleFile.PropertyName.ShouldBe("SingleFile");
		result.SingleFile.Size.ShouldBe(19); // "Single file content"

		result.ReadOnlyList1.Count.ShouldBe(2);
		result.ReadOnlyList1[0].FileName.ShouldBe("list1_file1.txt");
		result.ReadOnlyList1[1].FileName.ShouldBe("list1_file2.txt");

		result.ReadOnlyList2.Count.ShouldBe(1);
		result.ReadOnlyList2[0].FileName.ShouldBe("list2_file1.doc");

		result.FileCollectionList.Count.ShouldBe(6); // IFormFileCollection binds all files
	}

	[Fact]
	public async Task PostMultipart_WithEmptyOptionalLists_ReturnsEmptyLists()
	{
		// Arrange
		using MultipartFormDataContent content = CreateMultipartContent(
			someData: "Minimal Data",
			singleFileName: "single.pdf",
			singleFileContent: "Content",
			readOnlyList1Files: [("required.txt", "Required")],
			readOnlyList2Files: [],
			fileCollectionFiles: []
		);

		// Act
		HttpResponseMessage response = await _client.PostAsync("/api/v1/FileHandling/Multipart", content);

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.OK);
		Multipart.ResponseModel? result = await response.Content.ReadFromJsonAsync<Multipart.ResponseModel>();
		result.ShouldNotBeNull();

		result.SomeData.ShouldBe("Minimal Data");
		result.ReadOnlyList2.ShouldBeEmpty();
	}

	[Fact]
	public async Task PostMultipart_WithLargeFiles_ReturnsCorrectSizes()
	{
		// Arrange
		byte[] largeContent = new byte[1024 * 50]; // 50KB
		Random.Shared.NextBytes(largeContent);

		using StringContent someDataContent = new("Large File Test");
		using MultipartFormDataContent content = new();
		content.Add(someDataContent, "SomeData");

		using MemoryStream singleStream = new(largeContent);
		using StreamContent singleContent = new(singleStream);
		content.Add(singleContent, "SingleFile", "large.bin");

		using MemoryStream list1Stream = new("Small file"u8.ToArray());
		using StreamContent list1Content = new(list1Stream);
		content.Add(list1Content, "ReadOnlyList1", "small.txt");

		// Act
		HttpResponseMessage response = await _client.PostAsync("/api/v1/FileHandling/Multipart", content);

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.OK);
		Multipart.ResponseModel? result = await response.Content.ReadFromJsonAsync<Multipart.ResponseModel>();
		result.ShouldNotBeNull();

		result.SingleFile.Size.ShouldBe(1024 * 50);
		result.ReadOnlyList1[0].Size.ShouldBe(10); // "Small file"
	}

	[Fact]
	public async Task PostMultipart_WithMultipleFilesInReadOnlyList1_MapsAllFiles()
	{
		// Arrange
		using MultipartFormDataContent content = CreateMultipartContent(
			someData: "Multiple List Files",
			singleFileName: "single.pdf",
			singleFileContent: "Single",
			readOnlyList1Files:
			[
				("file1.txt", "Content 1"),
				("file2.txt", "Content 2"),
				("file3.txt", "Content 3"),
				("file4.txt", "Content 4")
			],
			readOnlyList2Files: [],
			fileCollectionFiles: []
		);

		// Act
		HttpResponseMessage response = await _client.PostAsync("/api/v1/FileHandling/Multipart", content);

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.OK);
		Multipart.ResponseModel? result = await response.Content.ReadFromJsonAsync<Multipart.ResponseModel>();
		result.ShouldNotBeNull();

		result.ReadOnlyList1.Count.ShouldBe(4);
		result.ReadOnlyList1[0].FileName.ShouldBe("file1.txt");
		result.ReadOnlyList1[1].FileName.ShouldBe("file2.txt");
		result.ReadOnlyList1[2].FileName.ShouldBe("file3.txt");
		result.ReadOnlyList1[3].FileName.ShouldBe("file4.txt");
	}

	[Fact]
	public async Task PostMultipart_PreservesPropertyNames()
	{
		// Arrange
		using MultipartFormDataContent content = CreateMultipartContent(
			someData: "Property Name Test",
			singleFileName: "single.pdf",
			singleFileContent: "Content",
			readOnlyList1Files: [("list1.txt", "List1")],
			readOnlyList2Files: [("list2.txt", "List2")],
			fileCollectionFiles: []
		);

		// Act
		HttpResponseMessage response = await _client.PostAsync("/api/v1/FileHandling/Multipart", content);

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.OK);
		Multipart.ResponseModel? result = await response.Content.ReadFromJsonAsync<Multipart.ResponseModel>();
		result.ShouldNotBeNull();

		result.SingleFile.PropertyName.ShouldBe("SingleFile");
		result.ReadOnlyList1[0].PropertyName.ShouldBe("ReadOnlyList1");
		result.ReadOnlyList2[0].PropertyName.ShouldBe("ReadOnlyList2");
	}

	[Theory]
	[InlineData("Simple text")]
	[InlineData("Data with special chars: !@#$%")]
	[InlineData("Unicode: ??? ?? ???")]
	public async Task PostMultipart_WithVariousSomeDataValues_PreservesData(string someData)
	{
		// Arrange
		using MultipartFormDataContent content = CreateMultipartContent(
			someData: someData,
			singleFileName: "file.txt",
			singleFileContent: "Content",
			readOnlyList1Files: [("list.txt", "List")],
			readOnlyList2Files: [],
			fileCollectionFiles: []
		);

		// Act
		HttpResponseMessage response = await _client.PostAsync("/api/v1/FileHandling/Multipart", content);

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.OK);
		Multipart.ResponseModel? result = await response.Content.ReadFromJsonAsync<Multipart.ResponseModel>();
		result.ShouldNotBeNull();
		result.SomeData.ShouldBe(someData);
	}

	/// <summary>
	/// Helper method to create multipart form content with files
	/// </summary>
	static MultipartFormDataContent CreateMultipartContent(
		string someData,
		string singleFileName,
		string singleFileContent,
		(string fileName, string content)[] readOnlyList1Files,
		(string fileName, string content)[] readOnlyList2Files,
		(string fileName, string content)[] fileCollectionFiles)
	{
		StringContent someDataContent = new(someData);
		MultipartFormDataContent content = new()
		{
			// Add SomeData field
			{ someDataContent, "SomeData" }
		};

		// Add SingleFile
		MemoryStream singleStream = new(System.Text.Encoding.UTF8.GetBytes(singleFileContent));
		StreamContent singleStreamContent = new(singleStream);
		content.Add(singleStreamContent, "SingleFile", singleFileName);

		// Add ReadOnlyList1 files
		foreach ((string fileName, string fileContent) in readOnlyList1Files)
		{
			MemoryStream stream = new(System.Text.Encoding.UTF8.GetBytes(fileContent));
			StreamContent streamContent = new(stream);
			content.Add(streamContent, "ReadOnlyList1", fileName);
		}

		// Add ReadOnlyList2 files
		foreach ((string fileName, string fileContent) in readOnlyList2Files)
		{
			MemoryStream stream = new(System.Text.Encoding.UTF8.GetBytes(fileContent));
			StreamContent streamContent = new(stream);
			content.Add(streamContent, "ReadOnlyList2", fileName);
		}

		// Add FileCollectionList files
		foreach ((string fileName, string fileContent) in fileCollectionFiles)
		{
			MemoryStream stream = new(System.Text.Encoding.UTF8.GetBytes(fileContent));
			StreamContent streamContent = new(stream);
			content.Add(streamContent, "FileCollectionList", fileName);
		}

		return content;
	}
}
