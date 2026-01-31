using ExampleApi.Endpoints.FileHandling.PostSingleFile;
using Microsoft.AspNetCore.Http;

namespace ExampleApi.Tests.Endpoints.FileHandling;

public class PostFileHandlingSingleFileEndpointTests
{
	[Fact]
	public async Task Handle_WithValidFile_ReturnsFileDetails()
	{
		// Arrange
		IFormFile file = CreateMockFormFile("document.pdf", "uploadedFile", 1024);
		PostFileHandlingSingleFileEndpoint endpoint = new();

		// Act
		ResponseModel result = await endpoint.Handle(file, CancellationToken.None);

		// Assert
		await Verify(result);
	}

	[Fact]
	public async Task Handle_WithZeroLengthFile_ReturnsZeroSize()
	{
		// Arrange
		IFormFile file = CreateMockFormFile("empty.txt", "emptyFile", 0);
		PostFileHandlingSingleFileEndpoint endpoint = new();

		// Act
		ResponseModel result = await endpoint.Handle(file, CancellationToken.None);

		// Assert
		await Verify(result);
	}

	[Fact]
	public async Task Handle_WithLargeFile_ReturnsCorrectSize()
	{
		// Arrange
		int largeFileSize = int.MaxValue;
		IFormFile file = CreateMockFormFile("largefile.bin", "largeFile", largeFileSize);
		PostFileHandlingSingleFileEndpoint endpoint = new();

		// Act
		ResponseModel result = await endpoint.Handle(file, CancellationToken.None);

		// Assert
		await Verify(result);
	}

	[Theory]
	[InlineData("file.pdf", "pdfFile", 500)]
	[InlineData("image.png", "imageUpload", 2048)]
	[InlineData("data.json", "jsonData", 128)]
	public async Task Handle_WithVariousFiles_ReturnsCorrectDetails(string fileName, string propertyName, int size)
	{
		// Arrange
		IFormFile file = CreateMockFormFile(fileName, propertyName, size);
		PostFileHandlingSingleFileEndpoint endpoint = new();

		// Act
		ResponseModel result = await endpoint.Handle(file, CancellationToken.None);

		// Assert
		await Verify(result)
			.UseParameters(fileName, propertyName, size);
	}

	static IFormFile CreateMockFormFile(string fileName, string name, long length)
	{
		IFormFile formFile = Substitute.For<IFormFile>();
		formFile.FileName.Returns(fileName);
		formFile.Name.Returns(name);
		formFile.Length.Returns(length);
		return formFile;
	}
}
