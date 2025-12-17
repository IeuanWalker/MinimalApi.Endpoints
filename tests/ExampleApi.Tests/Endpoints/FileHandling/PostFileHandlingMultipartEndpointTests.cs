using ExampleApi.Endpoints.FileHandling.PostMultipart;
using Microsoft.AspNetCore.Http;

namespace ExampleApi.Tests.Endpoints.FileHandling;

public class PostFileHandlingMultipartEndpointTests
{
	[Fact]
	public void Constructor_WithNullHttpContextAccessor_ThrowsArgumentNullException()
	{
		// Act & Assert
		Should.Throw<ArgumentNullException>(() => new PostFileHandlingMultipartEndpoint(null!));
	}

	[Fact]
	public async Task Handle_WithValidRequest_ReturnsCorrectResponse()
	{
		// Arrange
		IFormFile singleFile = CreateMockFormFile("single.pdf", "SingleFile", 1024);
		IFormFile listFile1 = CreateMockFormFile("list1.txt", "ReadOnlyList1", 512);
		IFormFile listFile2 = CreateMockFormFile("list2.txt", "ReadOnlyList1", 256);
		IFormFile optionalFile = CreateMockFormFile("optional.doc", "ReadOnlyList2", 128);
		IFormFile collectionFile = CreateMockFormFile("collection.png", "FileCollectionList", 2048);

		IFormFileCollection fileCollection = CreateFormFileCollection(collectionFile);

		RequestModel request = new()
		{
			SomeData = "Test Data",
			SingleFile = singleFile,
			ReadOnlyList1 = [listFile1, listFile2],
			ReadOnlyList2 = [optionalFile],
			FileCollectionList = fileCollection
		};

		IHttpContextAccessor httpContextAccessor = CreateMockHttpContextAccessor(5);
		PostFileHandlingMultipartEndpoint endpoint = new(httpContextAccessor);

		// Act
		ResponseModel result = await endpoint.Handle(request, CancellationToken.None);

		// Assert
		result.SomeData.ShouldBe("Test Data");
		result.TotalFileCount.ShouldBe(5);

		result.SingleFile.FileName.ShouldBe("single.pdf");
		result.SingleFile.PropertyName.ShouldBe("SingleFile");
		result.SingleFile.Size.ShouldBe(1024);

		result.ReadOnlyList1.Count.ShouldBe(2);
		result.ReadOnlyList1[0].FileName.ShouldBe("list1.txt");
		result.ReadOnlyList1[1].FileName.ShouldBe("list2.txt");

		result.ReadOnlyList2.Count.ShouldBe(1);
		result.ReadOnlyList2[0].FileName.ShouldBe("optional.doc");

		result.FileCollectionList.Count.ShouldBe(1);
		result.FileCollectionList[0].FileName.ShouldBe("collection.png");
	}

	[Fact]
	public async Task Handle_WithEmptyOptionalList_ReturnsEmptyList()
	{
		// Arrange
		IFormFile singleFile = CreateMockFormFile("single.pdf", "SingleFile", 1024);
		IFormFile listFile = CreateMockFormFile("list.txt", "ReadOnlyList1", 512);
		IFormFileCollection fileCollection = CreateFormFileCollection();

		RequestModel request = new()
		{
			SomeData = "Test Data",
			SingleFile = singleFile,
			ReadOnlyList1 = [listFile],
			ReadOnlyList2 = [],
			FileCollectionList = fileCollection
		};

		IHttpContextAccessor httpContextAccessor = CreateMockHttpContextAccessor(2);
		PostFileHandlingMultipartEndpoint endpoint = new(httpContextAccessor);

		// Act
		ResponseModel result = await endpoint.Handle(request, CancellationToken.None);

		// Assert
		result.ReadOnlyList2.ShouldBeEmpty();
		result.FileCollectionList.ShouldBeEmpty();
	}

	[Fact]
	public async Task Handle_WithMultipleFilesInCollection_MapsAllFiles()
	{
		// Arrange
		IFormFile singleFile = CreateMockFormFile("single.pdf", "SingleFile", 100);
		IFormFile listFile = CreateMockFormFile("list.txt", "ReadOnlyList1", 200);
		IFormFile collectionFile1 = CreateMockFormFile("col1.png", "FileCollectionList", 300);
		IFormFile collectionFile2 = CreateMockFormFile("col2.jpg", "FileCollectionList", 400);
		IFormFile collectionFile3 = CreateMockFormFile("col3.gif", "FileCollectionList", 500);

		IFormFileCollection fileCollection = CreateFormFileCollection(collectionFile1, collectionFile2, collectionFile3);

		RequestModel request = new()
		{
			SomeData = "Multiple Files Test",
			SingleFile = singleFile,
			ReadOnlyList1 = [listFile],
			ReadOnlyList2 = [],
			FileCollectionList = fileCollection
		};

		IHttpContextAccessor httpContextAccessor = CreateMockHttpContextAccessor(5);
		PostFileHandlingMultipartEndpoint endpoint = new(httpContextAccessor);

		// Act
		ResponseModel result = await endpoint.Handle(request, CancellationToken.None);

		// Assert
		result.FileCollectionList.Count.ShouldBe(3);
		result.FileCollectionList[0].FileName.ShouldBe("col1.png");
		result.FileCollectionList[0].Size.ShouldBe(300);
		result.FileCollectionList[1].FileName.ShouldBe("col2.jpg");
		result.FileCollectionList[1].Size.ShouldBe(400);
		result.FileCollectionList[2].FileName.ShouldBe("col3.gif");
		result.FileCollectionList[2].Size.ShouldBe(500);
	}

	[Fact]
	public async Task Handle_PreservesPropertyNames_InMappedFiles()
	{
		// Arrange
		IFormFile singleFile = CreateMockFormFile("file.pdf", "CustomSingleName", 100);
		IFormFile listFile = CreateMockFormFile("file.txt", "CustomListName", 200);
		IFormFileCollection fileCollection = CreateFormFileCollection();

		RequestModel request = new()
		{
			SomeData = "Property Name Test",
			SingleFile = singleFile,
			ReadOnlyList1 = [listFile],
			ReadOnlyList2 = [],
			FileCollectionList = fileCollection
		};

		IHttpContextAccessor httpContextAccessor = CreateMockHttpContextAccessor(2);
		PostFileHandlingMultipartEndpoint endpoint = new(httpContextAccessor);

		// Act
		ResponseModel result = await endpoint.Handle(request, CancellationToken.None);

		// Assert
		result.SingleFile.PropertyName.ShouldBe("CustomSingleName");
		result.ReadOnlyList1[0].PropertyName.ShouldBe("CustomListName");
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

	static IHttpContextAccessor CreateMockHttpContextAccessor(int totalFileCount)
	{
		IFormFileCollection formFiles = Substitute.For<IFormFileCollection>();
		formFiles.Count.Returns(totalFileCount);

		IFormCollection formCollection = Substitute.For<IFormCollection>();
		formCollection.Files.Returns(formFiles);

		HttpRequest httpRequest = Substitute.For<HttpRequest>();
		httpRequest.Form.Returns(formCollection);

		HttpContext httpContext = Substitute.For<HttpContext>();
		httpContext.Request.Returns(httpRequest);

		IHttpContextAccessor httpContextAccessor = Substitute.For<IHttpContextAccessor>();
		httpContextAccessor.HttpContext.Returns(httpContext);

		return httpContextAccessor;
	}
}
