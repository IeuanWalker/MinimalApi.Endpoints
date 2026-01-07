using IeuanWalker.MinimalApi.Endpoints.Validation;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.Routing;

namespace IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers.RequestPropertyEnhancer;

partial class ValidationDocumentTransformer
{
	static void DiscoverManualValidationRules(OpenApiDocumentTransformerContext context, Dictionary<Type, (List<ValidationRule> rules, bool appendRulesToPropertyDescription)> allValidationRules)
	{
		if (context.ApplicationServices.GetService(typeof(EndpointDataSource)) is not EndpointDataSource endpointDataSource)
		{
			return;
		}

		foreach (IValidationMetadata validationMetadata in endpointDataSource.Endpoints
			.OfType<RouteEndpoint>()
			.SelectMany(endpoint => endpoint.Metadata.GetOrderedMetadata<IValidationMetadata>()))
		{
			IValidationConfiguration config = validationMetadata.Configuration;
			Type requestType = validationMetadata.RequestType;

			(List<ValidationRule> rules, bool appendRulesToPropertyDescription) = allValidationRules.TryGetValue(requestType, out (List<ValidationRule> rules, bool appendRulesToPropertyDescription) value)
				? value
				: ([], true);

			appendRulesToPropertyDescription = config.AppendRulesToPropertyDescription;

			IReadOnlyList<ValidationRule> manualRules = config.Rules;
			if (manualRules.Count > 0)
			{
				HashSet<string> manualPropertyNames = [.. manualRules.Select(r => r.PropertyName).Distinct()];
				rules.RemoveAll(r => manualPropertyNames.Contains(r.PropertyName));
				rules.AddRange(manualRules);
			}

			IReadOnlyDictionary<string, IReadOnlyList<ValidationRuleOperation>> operationsByProperty = config.OperationsByProperty;
			if (operationsByProperty.Count > 0)
			{
				foreach (KeyValuePair<string, IReadOnlyList<ValidationRuleOperation>> kvp in operationsByProperty)
				{
					string propertyName = kvp.Key;
					IReadOnlyList<ValidationRuleOperation> operations = kvp.Value;

					List<ValidationRule> propertyRules = [.. rules.Where(r => r.PropertyName == propertyName)];

					foreach (ValidationRuleOperation operation in operations)
					{
						operation.Apply(propertyRules);
					}

					rules.RemoveAll(r => r.PropertyName == propertyName);
					rules.AddRange(propertyRules);
				}
			}

			allValidationRules[requestType] = (rules, appendRulesToPropertyDescription);
		}
	}
}
