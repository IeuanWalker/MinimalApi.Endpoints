namespace IeuanWalker.MinimalApi.Endpoints.Generator.Helpers;

static class EndpointGeneratorHelper
{
	internal static void ToEndpoint(this IndentedTextBuilder builder, EndpointInfo endpoint, int routeNumber)
	{
		string uniqueRootName = WithNameHelpers.GenerateWithName(endpoint.Verb, endpoint.Pattern, routeNumber);

		builder.AppendLine($"// {endpoint.Verb.ToString().ToUpper()}: {endpoint.Pattern}");

		builder.AppendLine($"RouteHandlerBuilder {uniqueRootName} = app");
		builder.IncreaseIndent();
		builder.AppendLine($".{endpoint.Verb.ToMap()}(\"{endpoint.Pattern}\", async (");

		builder.IncreaseIndent();

		if (endpoint.HasRequest)
		{
			builder.AppendLine($"{endpoint.GetBindingType()}global::{endpoint.RequestType} request,");
		}
		builder.AppendLine($"[FromServices] global::{endpoint.ClassName} endpoint,");
		builder.Append($"CancellationToken ct) => await endpoint.HandleAsync({(endpoint.HasRequest ? "request, " : string.Empty)}ct))");

		builder.DecreaseIndent();

		if (endpoint.WithTags is null)
		{
			builder.GenerateAndAddTags(endpoint.Pattern);
		}

		if (endpoint.WithName is null)
		{
			builder.AppendLine();
			builder.Append($".WithName(\"{uniqueRootName}\")");
		}

		if (endpoint.FluentValidationClass is not null)
		{
			builder.AppendLine();
			builder.AppendLine(".DisableValidation()");
			builder.AppendLine($".AddEndpointFilter<FluentValidationFilter<global::{endpoint.RequestType}>>()");
			builder.Append(".ProducesValidationProblem()");
		}

		builder.AppendLine(";");
		builder.DecreaseIndent();
		builder.AppendLine();

		// Configure the endpoint
		builder.AppendLine($"global::{endpoint.ClassName}.Configure({uniqueRootName});");
	}

	static string GetBindingType(this EndpointInfo endpoint)
	{
		if (!endpoint.HasRequest)
		{
			return string.Empty;
		}

		if (endpoint.RequestBindingType is null)
		{
			return string.Empty;
		}

		return $"[{endpoint.RequestBindingType.Value.RequestBindingType.ConvertFromRequestBindingType()}{(endpoint.RequestBindingType.Value.Name is not null ? $"(Name = \"{endpoint.RequestBindingType.Value.Name}\")" : string.Empty)}] ";
	}
}
