using IeuanWalker.MinimalApi.Endpoints.Validation;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.Routing;

namespace IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers;

partial class ValidationDocumentTransformer
{
	static void DiscoverManualValidationRules(OpenApiDocumentTransformerContext context, Dictionary<Type, (List<ValidationRule> rules, bool appendRulesToPropertyDescription)> allValidationRules)
	{
		// Iterate through all endpoints to find WithValidation metadata
		if (context.ApplicationServices.GetService(typeof(EndpointDataSource)) is not EndpointDataSource endpointDataSource)
		{
			return;
		}

		foreach (IValidationMetadata validationMetadata in endpointDataSource.Endpoints
			.OfType<RouteEndpoint>()
			.SelectMany(endpoint => endpoint.Metadata.GetOrderedMetadata<IValidationMetadata>()))
		{
			// Get the configuration and request type through the interface (no reflection needed)
			IValidationConfiguration config = validationMetadata.Configuration;
			Type requestType = validationMetadata.RequestType;

			(List<ValidationRule> rules, bool appendRulesToPropertyDescription) = allValidationRules.TryGetValue(requestType, out (List<ValidationRule> rules, bool appendRulesToPropertyDescription) value)
				? value
				: ([], true);

			// Get AppendRulesToPropertyDescription setting directly from the interface
			appendRulesToPropertyDescription = config.AppendRulesToPropertyDescription;

			// Get rules directly from the interface
			IReadOnlyList<ValidationRule> manualRules = config.Rules;
			if (manualRules.Count > 0)
			{
				// Remove auto-discovered rules for properties that have manual rules
				HashSet<string> manualPropertyNames = [.. manualRules.Select(r => r.PropertyName).Distinct()];
				rules.RemoveAll(r => manualPropertyNames.Contains(r.PropertyName));
				rules.AddRange(manualRules);
			}

			// Get operations directly from the interface and apply them
			IReadOnlyDictionary<string, IReadOnlyList<ValidationRuleOperation>> operationsByProperty = config.OperationsByProperty;
			if (operationsByProperty.Count > 0)
			{
				// Apply operations for each property
				foreach (KeyValuePair<string, IReadOnlyList<ValidationRuleOperation>> kvp in operationsByProperty)
				{
					string propertyName = kvp.Key;
					IReadOnlyList<ValidationRuleOperation> operations = kvp.Value;

					// Get all rules for this property
					List<ValidationRule> propertyRules = [.. rules.Where(r => r.PropertyName == propertyName)];

					// Apply all operations in order
					foreach (ValidationRuleOperation operation in operations)
					{
						operation.Apply(propertyRules);
					}

					// Replace the rules for this property with the modified ones
					rules.RemoveAll(r => r.PropertyName == propertyName);
					rules.AddRange(propertyRules);
				}
			}

			allValidationRules[requestType] = (rules, appendRulesToPropertyDescription);
		}
	}
}
