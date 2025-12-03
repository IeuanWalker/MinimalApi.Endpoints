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
			List<string> requirements = [];

			// Extract requirements from the policy
			foreach (IAuthorizationRequirement requirement in policy.Requirements)
			{
				string requirementText = requirement?.ToString() ?? string.Empty;

				if (string.IsNullOrEmpty(requirementText))
				{
					continue;
				}

				List<string> requirementTextSplit = requirementText.Split(':').ToList();
				requirementTextSplit.RemoveAt(0);

				requirements.Add(string.Join(string.Empty, requirementTextSplit));
			}

			if (requirements.Count > 0)
			{
				policiesWithRequirements[policyName] = requirements;
			}
		}


		if (policiesWithRequirements.Count == 0)
		{
			return Task.CompletedTask;
		}

		if (policiesWithRequirements.Count == 1)
		{
			StringBuilder authDescription = new();
			authDescription.AppendLine("**Authorization Requirements:**");

			foreach (KeyValuePair<string, List<string>> kvp in policiesWithRequirements)
			{
				foreach (string requirement in kvp.Value)
				{
					authDescription.AppendLine($"  - {requirement}");
				}
			}

			string authText = authDescription.ToString().TrimEnd();
			operation.Description = string.IsNullOrEmpty(operation.Description) ? authText : $"{operation.Description}\n\n{authText}";

			return Task.CompletedTask;
		}

		// Build the description with hierarchical policy structure
		if (policiesWithRequirements.Count > 0)
		{
			StringBuilder authDescription = new();
			authDescription.AppendLine("**Authorization Policies:**");

			foreach (KeyValuePair<string, List<string>> kvp in policiesWithRequirements)
			{
				authDescription.AppendLine($"- **{kvp.Key}**");
				foreach (string requirement in kvp.Value)
				{
					authDescription.AppendLine($"  - {requirement}");
				}
			}

			string authText = authDescription.ToString().TrimEnd();
			operation.Description = string.IsNullOrEmpty(operation.Description) ? authText : $"{operation.Description}\n\n{authText}";
		}

		return Task.CompletedTask;
	}
}
