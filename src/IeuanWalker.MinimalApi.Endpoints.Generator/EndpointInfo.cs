using IeuanWalker.MinimalApi.Endpoints.Generator.Helpers;
using Microsoft.CodeAnalysis;

namespace IeuanWalker.MinimalApi.Endpoints.Generator;

class EndpointInfo
{
	public EndpointInfo(
		string className,
		EndpointType type,
		HttpVerb verb,
		string pattern,
		string? withName,
		string? withTags,
		ITypeSymbol? requestType,
		(RequestBindingTypeEnum RequestBindingType, string? Name)? requestBindingType,
		INamedTypeSymbol? fluentValidationClass,
		ITypeSymbol? responseType)
	{
		ClassName = className;
		Type = type;
		Verb = verb;
		Pattern = pattern;
		WithName = withName;
		WithTags = withTags;
		RequestType = requestType;
		RequestBindingType = requestBindingType;
		FluentValidationClass = fluentValidationClass;
		ResponseType = responseType;
	}

	public string ClassName { get; set; }
	public EndpointType Type { get; set; }
	public ITypeSymbol? RequestType { get; set; }
	public (RequestBindingTypeEnum RequestBindingType, string? Name)? RequestBindingType { get; set; }
	public INamedTypeSymbol? FluentValidationClass { get; set; }
	public ITypeSymbol? ResponseType { get; set; }
	public string? WithName { get; set; }
	public string? WithTags { get; set; }
	public HttpVerb Verb { get; set; }
	public string Pattern { get; set; }
	public bool HasRequest
	{
		get => Type switch
		{
			EndpointType.WithRequestAndResponse => true,
			EndpointType.WithoutResponse => true,
			EndpointType.WithoutRequest => false,
			EndpointType.WithoutRequestOrResponse => false,
			_ => false
		};
	}
	public bool HasResponse
	{
		get => Type switch
		{
			EndpointType.WithRequestAndResponse => true,
			EndpointType.WithoutResponse => false,
			EndpointType.WithoutRequest => true,
			EndpointType.WithoutRequestOrResponse => false,
			_ => false
		};
	}

	public string GetSafeClassName()
	{
		// Use the full class name to ensure uniqueness, then sanitize it
		return ClassName.Replace(".", "").Replace("<", "").Replace(">", "").Replace(",", "").Replace("`", "");
	}
}

enum EndpointType
{
	WithRequestAndResponse,
	WithoutRequest,
	WithoutResponse,
	WithoutRequestOrResponse
}

enum HttpVerb
{
	Get,
	Post,
	Put,
	Patch,
	Delete
}
