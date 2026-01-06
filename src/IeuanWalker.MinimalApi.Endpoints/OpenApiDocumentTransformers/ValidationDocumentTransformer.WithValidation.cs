using IeuanWalker.MinimalApi.Endpoints.Validation;
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
			// Check if this implements our non-generic IValidationMetadata interface
			if (metadata is not IValidationMetadata validationMetadata)
			{
				continue;
			}

			// Get the configuration and request type through the interface (no reflection needed)
			IValidationConfiguration config = validationMetadata.Configuration;
			Type requestType = validationMetadata.RequestType;

			if (!allValidationRules.TryGetValue(requestType, out (List<Validation.ValidationRule> rules, bool appendRulesToPropertyDescription) value))
			{
				allValidationRules.Add(requestType, ([], true));
			}

			(List<Validation.ValidationRule> rules, bool appendRulesToPropertyDescription) = allValidationRules[requestType];

			// Get AppendRulesToPropertyDescription setting directly from the interface
			appendRulesToPropertyDescription = config.AppendRulesToPropertyDescription;
			allValidationRules[requestType] = (rules, appendRulesToPropertyDescription);

			// Get rules directly from the interface
			IReadOnlyList<Validation.ValidationRule> manualRules = config.Rules;

			// Remove auto-discovered rules for properties that have manual rules
			HashSet<string> manualPropertyNames = [.. manualRules.Select(r => r.PropertyName).Distinct()];
			rules.RemoveAll(r => manualPropertyNames.Contains(r.PropertyName));
			rules.AddRange(manualRules);

			allValidationRules[requestType] = (rules, appendRulesToPropertyDescription);

			// Get operations directly from the interface and apply them
			IReadOnlyDictionary<string, IReadOnlyList<Validation.ValidationRuleOperation>> operationsByProperty = config.OperationsByProperty;
			if (operationsByProperty.Count == 0)
			{
				continue;
			}

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
