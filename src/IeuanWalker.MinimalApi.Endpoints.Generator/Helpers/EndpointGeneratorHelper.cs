namespace IeuanWalker.MinimalApi.Endpoints.Generator.Helpers;

internal static class EndpointGeneratorHelper
{
    internal static void ToEndpoint(this IndentedTextBuilder builder, EndpointInfo endpoint, int routeNumber)
    {
        string uniqueRootName = WithNameHelpers.GenerateWithName(endpoint.Verb, endpoint.Pattern, routeNumber);

        builder.AppendLine($"// {endpoint.Verb.ToString().ToUpper()}: {endpoint.Pattern}");

        builder.AppendLine($"RouteHandlerBuilder {uniqueRootName} = app");
        builder.IncreaseIndent();
        builder.AppendLine($".{endpoint.Verb.ToMap()}(\"{endpoint.Pattern}\", async ({(endpoint.HasRequest ? $"[AsParameters] global::{endpoint.RequestType} request, " : string.Empty)}[FromServices] global::{endpoint.ClassName} endpoint, CancellationToken ct) =>");
        using(builder.AppendBlock(false))
        {
            builder.AppendLine($"{(endpoint.HasResponse ? "return " : string.Empty)}await endpoint.HandleAsync({(endpoint.HasRequest ? "request, " : string.Empty)}ct);");
        }
        builder.Append(")");
        if(endpoint.WithTags is null)
        {
            builder.GenerateAndAddTags(endpoint.Pattern);
        }

        if(endpoint.WithName is null)
        {
            builder.AppendLine();
            builder.Append($".WithName(\"{uniqueRootName}\")");
        }
        builder.AppendLine(";");
        builder.DecreaseIndent();
        builder.AppendLine();

        // Configure the endpoint
        builder.AppendLine($"global::{endpoint.ClassName}.Configure({uniqueRootName});");
    }
}
