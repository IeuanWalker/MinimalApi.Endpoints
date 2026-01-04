using Microsoft.AspNetCore.OpenApi;

namespace IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers;

partial class ValidationDocumentTransformer
{
	static void DiscoverDataAnnotationValidationRules(OpenApiDocumentTransformerContext context, Dictionary<Type, (List<Validation.ValidationRule> rules, bool appendRulesToPropertyDescription)> allValidationRules)
	{
		//throw new NotImplementedException();
	}
}
