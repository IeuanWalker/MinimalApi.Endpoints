using IeuanWalker.MinimalApi.Endpoints.Generator.Extensions;
using Microsoft.CodeAnalysis;

namespace IeuanWalker.MinimalApi.Endpoints.Generator.Helpers;

static class EndpointGeneratorHelpers
{
	internal static void ToEndpoint(this IndentedTextBuilder builder, EndpointInfo endpoint, int routeNumber, List<ValidatorInfo> validators, (EndpointGroupInfo groupInfo, string groupName)? group)
	{
		string uniqueRootName = WithNameHelpers.GenerateWithName(endpoint.HttpVerb, $"{group?.groupInfo.Pattern ?? string.Empty}{endpoint.RoutePattern}", routeNumber).ToLowerFirstLetter();

		builder.AppendLine($"// {endpoint.HttpVerb.ToString().ToUpper()}: {group?.groupInfo?.Pattern ?? string.Empty}{endpoint.RoutePattern}");

		builder.AppendLine($"RouteHandlerBuilder {uniqueRootName} = {group?.groupName ?? "app"}");
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
			builder.Append($".WithName(\"{uniqueRootName}\")");
		}

		if (endpoint.RequestType is not null)
		{
			ValidatorInfo? requestValidator = validators.FirstOrDefault(x => x.ValidatedTypeName.Equals(endpoint.RequestType));

			if (requestValidator is not null)
			{
				builder.AppendLine();
				builder.AppendLine(".DisableValidation()");
				builder.AppendLine($".AddEndpointFilter<FluentValidationFilter<global::{endpoint.RequestType}>>()");
				builder.Append(".ProducesValidationProblem()");
			}
		}

		builder.AppendLine(";");
		builder.DecreaseIndent();
		builder.AppendLine();

		// Configure the endpoint
		builder.AppendLine($"global::{endpoint.TypeName}.Configure({uniqueRootName});");
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
