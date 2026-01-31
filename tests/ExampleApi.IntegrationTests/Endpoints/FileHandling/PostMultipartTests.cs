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
		HttpResponseMessage response = await _client.PostAsync("/api/v1/FileHandling/Multipart", content, TestContext.Current.CancellationToken);

		// Assert
		await Verify(response)
			.IgnoreMember("Content-Length");
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
		HttpResponseMessage response = await _client.PostAsync("/api/v1/FileHandling/Multipart", content, TestContext.Current.CancellationToken);

		// Assert
		await Verify(response)
			.IgnoreMember("Content-Length");
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
		HttpResponseMessage response = await _client.PostAsync("/api/v1/FileHandling/Multipart", content, TestContext.Current.CancellationToken);

		// Assert
		await Verify(response)
			.IgnoreMember("Content-Length");
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
		HttpResponseMessage response = await _client.PostAsync("/api/v1/FileHandling/Multipart", content, TestContext.Current.CancellationToken);

		// Assert
		await Verify(response)
			.IgnoreMember("Content-Length");
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
		HttpResponseMessage response = await _client.PostAsync("/api/v1/FileHandling/Multipart", content, TestContext.Current.CancellationToken);

		// Assert
		await Verify(response);
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
		HttpResponseMessage response = await _client.PostAsync("/api/v1/FileHandling/Multipart", content, TestContext.Current.CancellationToken);

		// Assert
		await Verify(response)
			.IgnoreMember("Content-Length");
	}

	/// <summary>
	/// Helper method to create multipart form content with files
	/// </summary>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "MultipartFormDataContent takes ownership of added HttpContent and will dispose them when the returned content is disposed by the caller.")]
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
