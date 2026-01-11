using IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers;
using IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers.RequestPropertyEnhancer.Validation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;

// Alias to avoid ambiguity with old namespace
using RPE = IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers.RequestPropertyEnhancer;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace IeuanWalker.MinimalApi.Endpoints;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public static class OpenApiExtensions
{
	extension(OpenApiOptions source)
	{
		/// <summary>
		/// <para>This enhances OpenAPI documentation for request properties, it does a number of things - </para>
		/// <para>- Sets the appropriate types</para>
		/// <para>- Adds in all the enum information</para>
		/// <para>- Add validation logic (fluent validation, data annotation and manual rules)</para>
		/// <para>// TODO: Here is the wiki guide for more info - </para>
		/// </summary>
		/// <param name="autoDocumentFluentValidation"></param>
		/// <param name="appendRulesToPropertyDescription"></param>
		/// <returns></returns>
		public OpenApiOptions EnhanceRequestProperties(bool autoDocumentFluentValidation = true, bool appendRulesToPropertyDescription = true)
		{
			// Add type transformer first to ensure all property types are correctly documented
			source.AddDocumentTransformer<RPE.TypeDocumentTransformer>();

			// Add enum transformer second so enum schemas are enriched before validation processing
			source.AddDocumentTransformer<RPE.EnumSchemaTransformer>();

			// Add unified validation transformer that handles both FluentValidation and WithValidation
			source.AddDocumentTransformer((document, context, ct) =>
			{
				RPE.ValidationDocumentTransformer transformer = new()
				{
					AutoDocumentFluentValidation = autoDocumentFluentValidation,
					AppendRulesToPropertyDescription = appendRulesToPropertyDescription
				};
				return transformer.TransformAsync(document, context, ct);
			});

			// Reorder so nullable is last
			source.AddDocumentTransformer<RPE.NullableSchemaReorderTransformer>();

			// Add cleanup transformer as the absolute final step to remove unused component schemas
			// This removes schemas that are no longer referenced after aggressive inlining and unwrapping
			source.AddDocumentTransformer<RPE.UnusedComponentsCleanupTransformer>();

			return source;
		}

		/// <summary>
		/// Adds security scheme documentation to the OpenAPI document with default configuration.
		/// This will automatically document all registered authentication schemes.
		/// </summary>
		/// <example>
		/// Simple usage with defaults:
		/// <code>
		/// builder.Services.AddOpenApi(options =>
		/// {
		///     options.AddAuthenticationSchemes();
		/// });
		/// </code>
		/// </example>
		public OpenApiOptions AddAuthenticationSchemes()
		{
			return source.AddAuthenticationSchemes(null);
		}

		/// <summary>
		/// Adds security scheme documentation to the OpenAPI document with custom configuration.
		/// This will automatically document all registered authentication schemes with the specified options.
		/// </summary>
		/// <param name="options">The OpenAPI options (passed via AddOpenApi lambda).</param>
		/// <param name="configure">Action to customize security scheme options.</param>
		/// <returns>The OpenAPI options for method chaining.</returns>
		/// <example>
		/// Custom configuration example:
		/// <code>
		/// builder.Services.AddOpenApi(options =>
		/// {
		///     options.AddAuthenticationSchemes(o =>
		///     {
		///         o.BearerFormat = "My Custom JWT";
		///         o.ApiKeyHeaderName = "X-Custom-Key";
		///         o.OAuth2AuthorizationUrl = new Uri("https://auth.example.com/authorize");
		///         o.OAuth2Scopes = new Dictionary&lt;string, string&gt;
		///         {
		///             { "read", "Read access" },
		///             { "write", "Write access" }
		///         };
		///         o.OpenIdConnectUrl = new Uri("https://auth.example.com/.well-known/openid-configuration");
		///     });
		/// });
		/// </code>
		/// </example>
		public OpenApiOptions AddAuthenticationSchemes(Action<SecuritySchemeTransformerOptions>? configure)
		{
			SecuritySchemeTransformerOptions securityOptions = new();
			configure?.Invoke(securityOptions);

			source.AddDocumentTransformer(async (document, context, ct) =>
			{
				IAuthenticationSchemeProvider? authProvider = context.ApplicationServices.GetService<IAuthenticationSchemeProvider>()
					?? throw new InvalidOperationException("Authentication services are not registered. Add authentication to your application using 'builder.Services.AddAuthentication()' before calling 'AddSecuritySchemeTransformer()'.");

				SecuritySchemeTransformer transformer = new(authProvider, securityOptions);
				await transformer.TransformAsync(document, context, ct);
			});

			return source;
		}

		/// <summary>
		/// Adds authorization policy documentation to the OpenAPI document.
		/// This will automatically document all authorization policies applied to endpoints,
		/// extracting and displaying their requirements in the operation descriptions.
		/// </summary>
		/// <example>
		/// Usage with OpenAPI options:
		/// <code>
		/// builder.Services.AddOpenApi(options =>
		/// {
		///     options.AddAuthorizationPoliciesAndRequirements();
		/// });
		/// </code>
		/// </example>
		public OpenApiOptions AddAuthorizationPoliciesAndRequirements()
		{
			source.AddOperationTransformer<AuthorizationPoliciesAndRequirementsOperationTransformer>();
			return source;
		}
	}

	extension(RouteHandlerBuilder source)
	{
		public RouteHandlerBuilder WithResponse(int statusCode, string description, string? contentType = null, params string[] additionalContentTypes)
		{
			source.WithResponse(null, statusCode, description, contentType, additionalContentTypes);

			return source;
		}

		public RouteHandlerBuilder WithResponse<T>(int statusCode, string description, string? contentType = null, params string[] additionalContentTypes)
		{
			source.WithResponse(typeof(T), statusCode, description, contentType, additionalContentTypes);

			return source;
		}

		RouteHandlerBuilder WithResponse(Type? responseType, int statusCode, string description, string? contentType = null, params string[] additionalContentTypes)
		{
			if (responseType is not null && string.IsNullOrEmpty(contentType))
			{
				contentType = "application/json";
			}


			if (contentType is null)
			{
				source.WithMetadata(new ProducesResponseTypeMetadata(statusCode, responseType ?? typeof(void)));
			}
			else
			{
				string[] contentTypes = new string[additionalContentTypes.Length + 1];
				contentTypes[0] = contentType;
				additionalContentTypes.CopyTo(contentTypes, 1);

				source.WithMetadata(new ProducesResponseTypeMetadata(statusCode, responseType ?? typeof(void), contentTypes));
			}

			source.AddOpenApiOperationTransformer((operation, _, _) =>
			{
				if (operation.Responses is not null)
				{
					IOpenApiResponse? response = operation.Responses[statusCode.ToString()];
					response?.Description = description;
				}

				return Task.CompletedTask;
			});

			return source;
		}

		/// <summary>
		/// Adds validation rules to the endpoint for OpenAPI schema documentation.
		/// This enables declarative validation constraints that appear in the generated OpenAPI specification.
		/// Note: This does NOT perform runtime validation - it only updates the OpenAPI documentation.
		/// </summary>
		/// <typeparam name="TRequest">The type of the request model to validate</typeparam>
		/// <param name="builder">The route handler builder</param>
		/// <param name="configure">Action to configure validation rules</param>
		/// <returns>The route handler builder for method chaining</returns>
		/// <example>
		/// <code>
		/// app.MapPost("/todos", async (TodoRequest request) => { ... })
		///    .WithValidation&lt;TodoRequest&gt;(config =>
		///    {
		///        config.Property(x => x.Title)
		///            .Required()
		///            .MinLength(1)
		///            .MaxLength(200);
		///
		///        config.Property(x => x.Email)
		///            .Email();
		///    });
		/// </code>
		/// </example>
		public RouteHandlerBuilder WithValidationRules<TRequest>(Action<ValidationConfigurationBuilder<TRequest>> configure)
		{
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(configure);

			ValidationConfigurationBuilder<TRequest> configBuilder = new();
			configure(configBuilder);
			ValidationConfiguration<TRequest> configuration = configBuilder.Build();

			// Store validation configuration as endpoint metadata for OpenAPI generation
			source.WithMetadata(new ValidationMetadata<TRequest>(configuration));

			return source;
		}
	}
}
