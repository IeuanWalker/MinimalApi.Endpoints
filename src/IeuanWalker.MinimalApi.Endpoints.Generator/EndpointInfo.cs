using IeuanWalker.MinimalApi.Endpoints.Generator.Helpers;
using Microsoft.CodeAnalysis;

namespace IeuanWalker.MinimalApi.Endpoints.Generator;

public class TypeInfo
{
	public TypeInfo(string typeName, Location location, List<DiagnosticInfo> diagnostics)
	{
		TypeName = typeName;
		Location = location;
		Diagnostics = diagnostics;
	}

	public string TypeName { get; }
	public Location Location { get; }
	public List<DiagnosticInfo> Diagnostics { get; }
}

public sealed class ValidatorInfo : TypeInfo
{
	public ValidatorInfo(string typeName, string validatedTypeName, Location location, List<DiagnosticInfo> diagnostics)
		: base(typeName, location, diagnostics)
	{
		ValidatedTypeName = validatedTypeName;
	}

	public string ValidatedTypeName { get; }
}

public sealed class EndpointGroupInfo : TypeInfo
{
	public EndpointGroupInfo(string typeName, string pattern, string? withName, string? withTags, Location location, List<DiagnosticInfo> diagnostics)
		: base(typeName, location, diagnostics)
	{
		Pattern = pattern;
		WithName = withName;
		WithTags = withTags;
	}

	public string Pattern { get; set; }
	public string? WithName { get; }
	public string? WithTags { get; }
}

public sealed class EndpointInfo : TypeInfo
{
	public EndpointInfo(
		string typeName,
		HttpVerb httpVerb,
		string routePattern,
		string? withName,
		string? withTags,
		string? group,
		string? requestType,
		(RequestBindingTypeEnum requestType, string? name)? requestBindingType,
		bool disableValidation,
		string? responseType,
		Location location,
		List<DiagnosticInfo> diagnostics)
		: base(typeName, location, diagnostics)
	{
		HttpVerb = httpVerb;
		RoutePattern = routePattern;
		WithName = withName;
		WithTags = withTags;
		Group = group;
		RequestType = requestType;
		RequestBindingType = requestBindingType;
		DisableValidation = disableValidation;
		ResponseType = responseType;
	}

	public HttpVerb? HttpVerb { get; }
	public string? RoutePattern { get; }
	public string? WithName { get; }
	public string? WithTags { get; }
	public string? Group { get; }
	public string? RequestType { get; }
	public (RequestBindingTypeEnum requestType, string? name)? RequestBindingType { get; }
	public string? ResponseType { get; }
	public bool DisableValidation { get; }

	public string GetSafeClassName()
	{
		// Use the full class name to ensure uniqueness, then sanitize it
		return TypeName.Replace(".", "").Replace("<", "").Replace(">", "").Replace(",", "").Replace("`", "");
	}
}

public enum HttpVerb
{
	Get,
	Post,
	Put,
	Patch,
	Delete
}
