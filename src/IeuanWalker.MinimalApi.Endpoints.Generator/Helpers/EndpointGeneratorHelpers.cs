using IeuanWalker.MinimalApi.Endpoints.Generator.Extensions;

namespace IeuanWalker.MinimalApi.Endpoints.Generator.Helpers;

static class EndpointGeneratorHelpers
{
	internal static void ToEndpoint(this IndentedTextBuilder builder, EndpointInfo endpoint, int routeNumber, List<ValidatorInfo> validators, (EndpointGroupInfo groupInfo, string groupName)? group)
	{
		string routePattern = $"{group?.groupInfo.Pattern ?? string.Empty}{endpoint.RoutePattern}";
		string prefix = WithNameHelpers.GenerateNamePrefix(endpoint.HttpVerb, routePattern).ToLowerFirstLetter();
		string variableName = $"{prefix}_{routeNumber}";

		builder.AppendLine($"// {endpoint.HttpVerb.ToString().ToUpper()}: {group?.groupInfo?.Pattern ?? string.Empty}{endpoint.RoutePattern}");

		builder.AppendLine($"RouteHandlerBuilder {variableName} = {group?.groupName ?? "app"}");
		builder.IncreaseIndent();
		builder.AppendLine($".{endpoint.HttpVerb.ToMap()}(\"{endpoint.RoutePattern}\", async (");

		builder.IncreaseIndent();

		if (endpoint.RequestType is not null)
		{
			builder.AppendLine($"{endpoint.GetBindingType()}global::{endpoint.RequestType} request,");
		}
		builder.AppendLine($"[FromServices] global::{endpoint.TypeName} endpoint,");
		builder.Append($"CancellationToken ct) => await endpoint.Handle({(endpoint.RequestType is not null ? "request, " : string.Empty)}ct))");

		builder.DecreaseIndent();

		if (endpoint.WithTags is null && group?.groupInfo.WithTags is null)
		{
			builder.GenerateAndAddTags($"{group?.groupInfo.Pattern ?? string.Empty}{endpoint.RoutePattern}");
		}

		if (endpoint.WithName is null && group?.groupInfo.WithName is null)
		{
			builder.AppendLine();
			builder.Append($".WithName(\"{WithNameHelpers.GenerateWithName(prefix, endpoint.TypeName)}\")");
		}

		bool addDefaultSuccessResponse = false;
		if (endpoint.RequestType is not null && !endpoint.DisableValidation)
		{
			ValidatorInfo? requestValidator = validators.FirstOrDefault(x => x.ValidatedTypeName.Equals(endpoint.RequestType));

			if (requestValidator is not null)
			{
				builder.AppendLine();
				builder.AppendLine(".DisableValidation()");
				builder.AppendLine($".AddEndpointFilter<FluentValidationFilter<global::{endpoint.RequestType}>>()");
				builder.Append(".ProducesValidationProblem()");
				addDefaultSuccessResponse = endpoint.ResponseType is null;
			}
		}

		builder.AppendLine(";");
		builder.DecreaseIndent();
		builder.AppendEmptyLine();

		// Configure the endpoint
		builder.AppendLine($"global::{endpoint.TypeName}.Configure({variableName});");
		if (endpoint.ResponseIsNullable)
		{
			builder.AppendLine($"global::IeuanWalker.MinimalApi.Endpoints.OpenApiExtensions.WithNullableResponse({variableName});");
		}
		if (addDefaultSuccessResponse)
		{
			builder.AppendLine($"global::IeuanWalker.MinimalApi.Endpoints.OpenApiExtensions.WithDefaultSuccessResponse({variableName});");
		}
	}

	static string GetBindingType(this EndpointInfo endpoint)
	{
		if (endpoint.RequestType is null)
		{
			return string.Empty;
		}

		if (endpoint.RequestBindingType is null)
		{
			return string.Empty;
		}


		return $"[{endpoint.RequestBindingType.Value.requestType.ConvertFromRequestBindingType()}{(endpoint.RequestBindingType.Value.name is not null ? $"(Name = \"{endpoint.RequestBindingType.Value.name}\")" : string.Empty)}] ";
	}
}
