using IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers.PropertyEnhancer;
using Microsoft.OpenApi;

namespace IeuanWalker.MinimalApi.Endpoints.Tests.OpenApiDocumentTransformers.PropertyEnhancer;

public class OpenApi31FileSchemaTransformerTests
{
	[Fact]
	public async Task TransformAsync_OpenApi31_NormalizesRawFileSchemas()
	{
		OpenApiSchema singleFile = CreateBinarySchema();
		OpenApiSchema fileItem = CreateBinarySchema();
		OpenApiDocument document = CreateDocument(new OpenApiSchema
		{
			Type = JsonSchemaType.Object,
			Properties = new Dictionary<string, IOpenApiSchema>
			{
				["singleFile"] = singleFile,
				["files"] = new OpenApiSchema { Type = JsonSchemaType.Array, Items = fileItem }
			}
		});

		await new OpenApi31FileSchemaTransformer().TransformAsync(document, null!, CancellationToken.None);

		singleFile.Type.ShouldBeNull();
		singleFile.Format.ShouldBeNull();
		singleFile.ContentMediaType.ShouldBe("application/octet-stream");
		fileItem.Type.ShouldBeNull();
		fileItem.Format.ShouldBeNull();
		fileItem.ContentMediaType.ShouldBe("application/octet-stream");
	}

	[Fact]
	public async Task TransformAsync_OpenApi31_PreservesNullableRawFileSchema()
	{
		OpenApiSchema nullableFile = CreateBinarySchema(JsonSchemaType.String | JsonSchemaType.Null);
		OpenApiDocument document = CreateDocument(nullableFile);

		await new OpenApi31FileSchemaTransformer().TransformAsync(document, null!, CancellationToken.None);

		nullableFile.Type.ShouldBeNull();
		nullableFile.Format.ShouldBeNull();
		nullableFile.ContentMediaType.ShouldBeNull();
		IList<IOpenApiSchema> alternatives = nullableFile.OneOf.ShouldNotBeNull();
		alternatives.Count.ShouldBe(2);

		OpenApiSchema rawFile = alternatives[0].ShouldBeOfType<OpenApiSchema>();
		rawFile.ContentMediaType.ShouldBe("application/octet-stream");
		rawFile.Not.ShouldBeOfType<OpenApiSchema>().Type.ShouldBe(JsonSchemaType.Null);

		alternatives[1].ShouldBeOfType<OpenApiSchema>().Type.ShouldBe(JsonSchemaType.Null);
	}

	[Fact]
	public async Task TransformAsync_OpenApi30_PreservesLegacyBinarySchemas()
	{
		OpenApiSchema file = CreateBinarySchema();
		OpenApiDocument document = CreateDocument(file);

		await new OpenApi31FileSchemaTransformer(enabled: false).TransformAsync(document, null!, CancellationToken.None);

		file.Type.ShouldBe(JsonSchemaType.String);
		file.Format.ShouldBe("binary");
	}

	[Fact]
	public async Task TransformAsync_FileRequest_RemovesUrlEncodedContent()
	{
		OpenApiDocument document = CreateRequestDocument(CreateBinarySchema());

		await new OpenApi31FileSchemaTransformer().TransformAsync(document, null!, CancellationToken.None);

		OpenApiRequestBody requestBody = GetRequestBody(document);
		IDictionary<string, OpenApiMediaType> content = requestBody.Content.ShouldNotBeNull();
		content.ShouldContainKey("multipart/form-data");
		content.ShouldNotContainKey("application/x-www-form-urlencoded");
	}

	[Fact]
	public async Task TransformAsync_NonFileRequest_PreservesUrlEncodedContent()
	{
		OpenApiDocument document = CreateRequestDocument(new OpenApiSchema { Type = JsonSchemaType.String });

		await new OpenApi31FileSchemaTransformer().TransformAsync(document, null!, CancellationToken.None);

		OpenApiRequestBody requestBody = GetRequestBody(document);
		IDictionary<string, OpenApiMediaType> content = requestBody.Content.ShouldNotBeNull();
		content.ShouldContainKey("multipart/form-data");
		content.ShouldContainKey("application/x-www-form-urlencoded");
	}

	static OpenApiDocument CreateRequestDocument(IOpenApiSchema propertySchema)
	{
		OpenApiSchema requestSchema = new()
		{
			Type = JsonSchemaType.Object,
			Properties = new Dictionary<string, IOpenApiSchema> { ["value"] = propertySchema }
		};
		OpenApiDocument document = CreateDocument(requestSchema);
		OpenApiSchemaReference reference = new("request", document, null);
		document.Paths = new OpenApiPaths
		{
			["/test"] = new OpenApiPathItem
			{
				Operations = new Dictionary<HttpMethod, OpenApiOperation>
				{
					[HttpMethod.Post] = new OpenApiOperation
					{
						RequestBody = new OpenApiRequestBody
						{
							Content = new Dictionary<string, OpenApiMediaType>
							{
								["multipart/form-data"] = new() { Schema = reference },
								["application/x-www-form-urlencoded"] = new() { Schema = reference }
							}
						}
					}
				}
			}
		};
		return document;
	}

	static OpenApiDocument CreateDocument(IOpenApiSchema schema) => new()
	{
		Components = new OpenApiComponents
		{
			Schemas = new Dictionary<string, IOpenApiSchema> { ["request"] = schema }
		}
	};

	static OpenApiRequestBody GetRequestBody(OpenApiDocument document) =>
		document.Paths!["/test"].Operations![HttpMethod.Post].RequestBody.ShouldBeOfType<OpenApiRequestBody>();

	static OpenApiSchema CreateBinarySchema(JsonSchemaType type = JsonSchemaType.String) => new()
	{
		Type = type,
		Format = "binary"
	};
}
