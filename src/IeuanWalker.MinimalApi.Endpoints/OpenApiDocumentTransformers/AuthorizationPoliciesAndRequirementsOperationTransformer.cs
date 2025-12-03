using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers;

[ExcludeFromCodeCoverage]
sealed class AuthorizationPoliciesAndRequirementsOperationTransformer : IOpenApiOperationTransformer
{
	public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
	{
		IList<object>? endpointMetadata = context.Description.ActionDescriptor?.EndpointMetadata;
		if (endpointMetadata is null)
		{
			return Task.CompletedTask;
		}

		Dictionary<string, List<string>> policiesWithRequirements = [];

		List<AuthorizationPolicy> policyObjects = [.. endpointMetadata.OfType<AuthorizationPolicy>()];

		for (int i = 0; i < policyObjects.Count; i++)
		{
			AuthorizationPolicy policy = policyObjects[i];
			string policyName = $"Policy {i + 1}";

			List<string> requirements = [.. policy.Requirements
					.Select(requirement => requirement?.ToString() ?? string.Empty)
					.Where(requirementText => !string.IsNullOrEmpty(requirementText))
					.Select(requirementText =>
					{
						List<string> requirementTextSplit = requirementText.Split(':').ToList();

						if (requirementTextSplit.Count > 1)
						{
							requirementTextSplit.RemoveAt(0);
						}

						return string.Join(string.Empty, requirementTextSplit);
					})];

			if (requirements.Count > 0)
			{
				policiesWithRequirements[policyName] = requirements;
			}
		}

		if (policiesWithRequirements.Count == 0)
		{
			return Task.CompletedTask;
		}

		StringBuilder authDescription = new();

		if (policiesWithRequirements.Count == 1)
		{
			authDescription.AppendLine("**Authorization Requirements:**");

			foreach (KeyValuePair<string, List<string>> kvp in policiesWithRequirements)
			{
				foreach (string requirement in kvp.Value)
				{
					authDescription.AppendLine($"  - {requirement}");
				}
			}

			operation.Description = string.IsNullOrEmpty(operation.Description) ? authDescription.ToString() : $"{operation.Description}\n\n{authDescription}";

			return Task.CompletedTask;
		}

		// Build the description with hierarchical policy structure
		authDescription = new();
		authDescription.AppendLine("**Authorization Policies:**");

		foreach (KeyValuePair<string, List<string>> kvp in policiesWithRequirements)
		{
			authDescription.AppendLine($"- **{kvp.Key}**");
			foreach (string requirement in kvp.Value)
			{
				authDescription.AppendLine($"  - {requirement}");
			}
		}

		operation.Description = string.IsNullOrEmpty(operation.Description) ? authDescription.ToString() : $"{operation.Description}\n\n{authDescription}";
		return Task.CompletedTask;
	}
}
