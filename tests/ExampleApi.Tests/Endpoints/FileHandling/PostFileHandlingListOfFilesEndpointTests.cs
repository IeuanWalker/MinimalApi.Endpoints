using ExampleApi.Endpoints.FileHandling.PostListOfFiles;
using Microsoft.AspNetCore.Http;

namespace ExampleApi.Tests.Endpoints.FileHandling;

public class PostFileHandlingListOfFilesEndpointTests
{
	[Fact]
	public async Task Handle_WithMultipleFiles_ReturnsAllFileDetails()
	{
		// Arrange
		IFormFile file1 = CreateMockFormFile("document.pdf", "file1", 1024);
		IFormFile file2 = CreateMockFormFile("image.png", "file2", 2048);
		IFormFile file3 = CreateMockFormFile("data.json", "file3", 512);

		IFormFileCollection formFileCollection = CreateFormFileCollection(file1, file2, file3);

		PostFileHandlingListOfFilesEndpoint endpoint = new();

		// Act
		IEnumerable<ResponseModel> result = await endpoint.Handle(formFileCollection, CancellationToken.None);

		// Assert
		await Verify(result);
	}

	[Fact]
	public async Task Handle_WithSingleFile_ReturnsSingleFileDetails()
	{
		// Arrange
		IFormFile file = CreateMockFormFile("test.txt", "uploadedFile", 256);
		IFormFileCollection formFileCollection = CreateFormFileCollection(file);

		PostFileHandlingListOfFilesEndpoint endpoint = new();

		// Act
		IEnumerable<ResponseModel> result = await endpoint.Handle(formFileCollection, CancellationToken.None);

		// Assert
		await Verify(result);
	}

	[Fact]
	public async Task Handle_WithEmptyCollection_ReturnsEmptyResult()
	{
		// Arrange
		IFormFileCollection formFileCollection = CreateFormFileCollection();

		PostFileHandlingListOfFilesEndpoint endpoint = new();

		// Act
		IEnumerable<ResponseModel> result = await endpoint.Handle(formFileCollection, CancellationToken.None);

		// Assert
		result.ShouldBeEmpty();
	}

	[Fact]
	public async Task Handle_WithZeroLengthFile_ReturnsZeroSize()
	{
		// Arrange
		IFormFile file = CreateMockFormFile("empty.txt", "emptyFile", 0);
		IFormFileCollection formFileCollection = CreateFormFileCollection(file);

		PostFileHandlingListOfFilesEndpoint endpoint = new();

		// Act
		IEnumerable<ResponseModel> result = await endpoint.Handle(formFileCollection, CancellationToken.None);

		// Assert
		await Verify(result);
	}

	[Fact]
	public async Task Handle_WithLargeFile_ReturnsCorrectSize()
	{
		// Arrange
		int largeFileSize = int.MaxValue;
		IFormFile file = CreateMockFormFile("largefile.bin", "largeFile", largeFileSize);
		IFormFileCollection formFileCollection = CreateFormFileCollection(file);

		PostFileHandlingListOfFilesEndpoint endpoint = new();

		// Act
		IEnumerable<ResponseModel> result = await endpoint.Handle(formFileCollection, CancellationToken.None);

		// Assert
		await Verify(result);
	}

	static IFormFile CreateMockFormFile(string fileName, string name, long length)
	{
		IFormFile formFile = Substitute.For<IFormFile>();
		formFile.FileName.Returns(fileName);
		formFile.Name.Returns(name);
		formFile.Length.Returns(length);
		return formFile;
	}

	static IFormFileCollection CreateFormFileCollection(params IFormFile[] files)
	{
		IFormFileCollection collection = Substitute.For<IFormFileCollection>();
		collection.Count.Returns(files.Length);
		collection.GetEnumerator().Returns(_ => ((IEnumerable<IFormFile>)files).GetEnumerator());
		return collection;
	}
}
