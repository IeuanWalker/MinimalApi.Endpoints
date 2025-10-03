using System;
using Microsoft.CodeAnalysis;

namespace IeuanWalker.MinimalApi.Endpoints.Generator;

/// <summary>
/// Represents extracted type information from syntax analysis.
/// This is an immutable value type that can be cached by the incremental generator.
/// </summary>
sealed class EndpointTypeInfo : IEquatable<EndpointTypeInfo>
{
	public EndpointTypeInfo(
		string typeName,
		bool isAbstract,
		bool isValidator,
		bool isEndpoint,
		string? validatedTypeName,
		string? httpVerb,
		string? routePattern,
		string? withName,
		string? withTags,
		string? groupTypeName,
		string? groupPattern,
		string? requestTypeName,
		string? requestBindingType,
		string? requestBindingName,
		bool disableValidation,
		string? responseTypeName,
		string[] interfaceNames,
		Location location,
		DiagnosticInfo[] diagnostics)
	{
		TypeName = typeName;
		IsAbstract = isAbstract;
		IsValidator = isValidator;
		IsEndpoint = isEndpoint;
		ValidatedTypeName = validatedTypeName;
		HttpVerb = httpVerb;
		RoutePattern = routePattern;
		WithName = withName;
		WithTags = withTags;
		GroupTypeName = groupTypeName;
		GroupPattern = groupPattern;
		RequestTypeName = requestTypeName;
		RequestBindingType = requestBindingType;
		RequestBindingName = requestBindingName;
		DisableValidation = disableValidation;
		ResponseTypeName = responseTypeName;
		InterfaceNames = interfaceNames;
		Location = location;
		Diagnostics = diagnostics;
	}

	public string TypeName { get; }
	public bool IsAbstract { get; }
	public bool IsValidator { get; }
	public bool IsEndpoint { get; }
	public string? ValidatedTypeName { get; }
	public string? HttpVerb { get; }
	public string? RoutePattern { get; }
	public string? WithName { get; }
	public string? WithTags { get; }
	public string? GroupTypeName { get; }
	public string? GroupPattern { get; }
	public string? RequestTypeName { get; }
	public string? RequestBindingType { get; }
	public string? RequestBindingName { get; }
	public bool DisableValidation { get; }
	public string? ResponseTypeName { get; }
	public string[] InterfaceNames { get; }
	public Location Location { get; }
	public DiagnosticInfo[] Diagnostics { get; }

	public bool Equals(EndpointTypeInfo? other)
	{
		if (other is null)
		{
			return false;
		}

		if (ReferenceEquals(this, other))
		{
			return true;
		}

		return TypeName == other.TypeName &&
			   IsAbstract == other.IsAbstract &&
			   IsValidator == other.IsValidator &&
			   IsEndpoint == other.IsEndpoint &&
			   ValidatedTypeName == other.ValidatedTypeName &&
			   HttpVerb == other.HttpVerb &&
			   RoutePattern == other.RoutePattern &&
			   WithName == other.WithName &&
			   WithTags == other.WithTags &&
			   GroupTypeName == other.GroupTypeName &&
			   GroupPattern == other.GroupPattern &&
			   RequestTypeName == other.RequestTypeName &&
			   RequestBindingType == other.RequestBindingType &&
			   RequestBindingName == other.RequestBindingName &&
			   DisableValidation == other.DisableValidation &&
			   ResponseTypeName == other.ResponseTypeName &&
			   InterfaceNamesEquals(other.InterfaceNames) &&
			   DiagnosticsEquals(other.Diagnostics);
	}

	bool InterfaceNamesEquals(string[] other)
	{
		if (InterfaceNames.Length != other.Length)
		{
			return false;
		}

		for (int i = 0; i < InterfaceNames.Length; i++)
		{
			if (InterfaceNames[i] != other[i])
			{
				return false;
			}
		}
		return true;
	}

	bool DiagnosticsEquals(DiagnosticInfo[] other)
	{
		if (Diagnostics.Length != other.Length)
		{
			return false;
		}

		for (int i = 0; i < Diagnostics.Length; i++)
		{
			if (!Diagnostics[i].Equals(other[i]))
			{
				return false;
			}
		}
		return true;
	}

	public override bool Equals(object? obj) => Equals(obj as EndpointTypeInfo);

	public override int GetHashCode()
	{
		unchecked
		{
			int hashCode = TypeName.GetHashCode();
			hashCode = (hashCode * 397) ^ IsAbstract.GetHashCode();
			hashCode = (hashCode * 397) ^ IsValidator.GetHashCode();
			hashCode = (hashCode * 397) ^ IsEndpoint.GetHashCode();
			hashCode = (hashCode * 397) ^ (ValidatedTypeName?.GetHashCode() ?? 0);
			hashCode = (hashCode * 397) ^ (HttpVerb?.GetHashCode() ?? 0);
			hashCode = (hashCode * 397) ^ (RoutePattern?.GetHashCode() ?? 0);
			hashCode = (hashCode * 397) ^ (WithName?.GetHashCode() ?? 0);
			hashCode = (hashCode * 397) ^ (WithTags?.GetHashCode() ?? 0);
			hashCode = (hashCode * 397) ^ (GroupTypeName?.GetHashCode() ?? 0);
			hashCode = (hashCode * 397) ^ (GroupPattern?.GetHashCode() ?? 0);
			hashCode = (hashCode * 397) ^ (RequestTypeName?.GetHashCode() ?? 0);
			hashCode = (hashCode * 397) ^ (RequestBindingType?.GetHashCode() ?? 0);
			hashCode = (hashCode * 397) ^ (RequestBindingName?.GetHashCode() ?? 0);
			hashCode = (hashCode * 397) ^ DisableValidation.GetHashCode();
			hashCode = (hashCode * 397) ^ (ResponseTypeName?.GetHashCode() ?? 0);
			return hashCode;
		}
	}
}
