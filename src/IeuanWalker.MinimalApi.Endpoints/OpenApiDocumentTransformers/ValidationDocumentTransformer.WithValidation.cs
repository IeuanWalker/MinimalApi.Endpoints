using System.Reflection;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.Routing;

namespace IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers;

partial class ValidationDocumentTransformer
{
	static void DiscoverManualValidationRules(OpenApiDocumentTransformerContext context, Dictionary<Type, (List<Validation.ValidationRule> rules, bool appendRulesToPropertyDescription)> allValidationRules)
	{
		// Iterate through all endpoints to find WithValidation metadata
		if (context.ApplicationServices.GetService(typeof(EndpointDataSource)) is not EndpointDataSource endpointDataSource)
		{
			return;
		}

		foreach (object metadata in endpointDataSource.Endpoints
			.Where(x => x is RouteEndpoint)
			.SelectMany(x => x.Metadata.GetOrderedMetadata<object>()))
		{
			// Use reflection to check if this is a ValidationMetadata<T>
			Type metadataType = metadata.GetType();
			if (!metadataType.IsGenericType || !metadataType.GetGenericTypeDefinition().Name.Contains("ValidationMetadata"))
			{
				continue;
			}

			// Extract the configuration and request type
			PropertyInfo? configProp = metadataType.GetProperty("Configuration");
			if (configProp?.GetValue(metadata) is not object config)
			{
				continue;
			}

			Type requestType = metadataType.GetGenericArguments()[0];
			if (!allValidationRules.TryGetValue(requestType, out (List<Validation.ValidationRule> rules, bool appendRulesToPropertyDescription) value))
			{
				allValidationRules.Add(requestType, ([], true));
			}

			(List<Validation.ValidationRule> rules, bool appendRulesToPropertyDescription) = allValidationRules[requestType];

			// Extract AppendRulesToPropertyDescription setting from the configuration object
			// TODO: Avoid strings
			PropertyInfo? appendRulesToPropertyDescriptionProperty = config.GetType().GetProperty("AppendRulesToPropertyDescription");
			if (appendRulesToPropertyDescriptionProperty?.GetValue(config) is bool listInDesc)
			{
				appendRulesToPropertyDescription = listInDesc;
			}

			allValidationRules[requestType] = (rules, appendRulesToPropertyDescription);

			// Extract rules from the configuration object
			PropertyInfo? rulesProp = config.GetType().GetProperty("Rules");
			if (rulesProp?.GetValue(config) is not IEnumerable<Validation.ValidationRule> manualRules)
			{
				continue;
			}

			// Remove auto-discovered rules for properties that have manual rules
			HashSet<string> manualPropertyNames = [.. manualRules.Select(r => r.PropertyName).Distinct()];
			rules.RemoveAll(r => manualPropertyNames.Contains(r.PropertyName));
			rules.AddRange(manualRules);

			allValidationRules[requestType] = (rules, appendRulesToPropertyDescription);
		}
	}
}
