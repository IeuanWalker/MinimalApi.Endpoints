using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.Routing;

namespace IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers;

partial class ValidationDocumentTransformer
{
	static void DiscoverManualValidationRules(OpenApiDocumentTransformerContext context, Dictionary<Type, List<Validation.ValidationRule>> allValidationRules, Dictionary<Type, bool> listRulesInDescription)
	{
		// Iterate through all endpoints to find WithValidation metadata
		if (context.ApplicationServices.GetService(typeof(EndpointDataSource)) is not EndpointDataSource endpointDataSource)
		{
			return;
		}

		foreach (Endpoint operation in endpointDataSource.Endpoints)
		{
			if (operation is RouteEndpoint routeEndpoint)
			{
				// Check for validation metadata
				IReadOnlyList<object> metadataItems = routeEndpoint.Metadata.GetOrderedMetadata<object>();
				foreach (object metadata in metadataItems)
				{
					// Use reflection to check if this is a ValidationMetadata<T>
					Type metadataType = metadata.GetType();
					if (metadataType.IsGenericType && metadataType.GetGenericTypeDefinition().Name.Contains("ValidationMetadata"))
					{
						// Extract the configuration and request type
						PropertyInfo? configProp = metadataType.GetProperty("Configuration");
						if (configProp?.GetValue(metadata) is object config)
						{
							Type requestType = metadataType.GetGenericArguments()[0];

							// Extract ListRulesInDescription setting from the configuration object
							PropertyInfo? listRulesInDescriptionProp = config.GetType().GetProperty("ListRulesInDescription");
							if (listRulesInDescriptionProp?.GetValue(config) is bool listInDesc)
							{
								listRulesInDescription[requestType] = listInDesc;
							}

							// Extract rules from the configuration object
							PropertyInfo? rulesProp = config.GetType().GetProperty("Rules");
							if (rulesProp?.GetValue(config) is IEnumerable<Validation.ValidationRule> manualRules)
							{
								// Manual rules override auto-discovered rules per property
								if (!allValidationRules.TryGetValue(requestType, out List<Validation.ValidationRule>? value))
								{
									value = [];
									allValidationRules[requestType] = value;
								}

								// Remove auto-discovered rules for properties that have manual rules
								HashSet<string> manualPropertyNames = [.. manualRules.Select(r => r.PropertyName).Distinct()];
								value.RemoveAll(r => manualPropertyNames.Contains(r.PropertyName));
								value.AddRange(manualRules);
							}
						}
					}
				}
			}
		}
	}

}
