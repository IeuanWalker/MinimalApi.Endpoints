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
			if (!metadataType.IsGenericType || !metadataType.GetGenericTypeDefinition().Name.Contains(nameof(Validation.ValidationMetadata<>)))
			{
				continue;
			}

			// Extract the configuration and request type
			PropertyInfo? configProp = metadataType.GetProperty(nameof(Validation.ValidationMetadata<>.Configuration));
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
			PropertyInfo? appendRulesToPropertyDescriptionProperty = config.GetType().GetProperty(nameof(Validation.ValidationConfiguration<>.AppendRulesToPropertyDescription));
			if (appendRulesToPropertyDescriptionProperty?.GetValue(config) is bool listInDesc)
			{
				appendRulesToPropertyDescription = listInDesc;
			}

			allValidationRules[requestType] = (rules, appendRulesToPropertyDescription);

			// Extract rules from the configuration object
			PropertyInfo? rulesProp = config.GetType().GetProperty(nameof(Validation.ValidationConfiguration<>.Rules));
			if (rulesProp?.GetValue(config) is not IEnumerable<Validation.ValidationRule> manualRules)
			{
				continue;
			}

			// Remove auto-discovered rules for properties that have manual rules
			HashSet<string> manualPropertyNames = [.. manualRules.Select(r => r.PropertyName).Distinct()];
			rules.RemoveAll(r => manualPropertyNames.Contains(r.PropertyName));
			rules.AddRange(manualRules);

			allValidationRules[requestType] = (rules, appendRulesToPropertyDescription);

			// Extract and apply operations from the configuration object
			PropertyInfo? operationsProp = config.GetType().GetProperty(nameof(Validation.ValidationConfiguration<>.OperationsByProperty), BindingFlags.Instance | BindingFlags.NonPublic);
			if (operationsProp?.GetValue(config) is IReadOnlyDictionary<string, IReadOnlyList<Validation.ValidationRuleOperation>> operationsByProperty)
			{
				// Apply operations for each property
				foreach (KeyValuePair<string, IReadOnlyList<Validation.ValidationRuleOperation>> kvp in operationsByProperty)
				{
					string propertyName = kvp.Key;
					IReadOnlyList<Validation.ValidationRuleOperation> operations = kvp.Value;

					// Get all rules for this property
					List<Validation.ValidationRule> propertyRules = [.. rules.Where(r => r.PropertyName == propertyName)];

					// Apply all operations in order
					foreach (Validation.ValidationRuleOperation operation in operations)
					{
						operation.Apply(propertyRules);
					}

					// Replace the rules for this property with the modified ones
					rules.RemoveAll(r => r.PropertyName == propertyName);
					rules.AddRange(propertyRules);
				}

				allValidationRules[requestType] = (rules, appendRulesToPropertyDescription);
			}
		}
	}
}
