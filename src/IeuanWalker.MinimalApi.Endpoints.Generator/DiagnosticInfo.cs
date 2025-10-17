using Microsoft.CodeAnalysis;

namespace IeuanWalker.MinimalApi.Endpoints.Generator;

/// <summary>
/// Represents diagnostic information to be reported during source generation.
/// This is an immutable value type that can be cached by the incremental generator.
/// </summary>
public sealed class DiagnosticInfo
{
	public DiagnosticInfo(
		string id,
		string title,
		string messageFormat,
		string category,
		DiagnosticSeverity severity,
		CachableLocation location,
		params object[] messageArgs)
	{
		Id = id;
		Title = title;
		MessageFormat = messageFormat;
		Category = category;
		Severity = severity;
		Location = location;
		MessageArgs = messageArgs;
	}

	public string Id { get; }
	public string Title { get; }
	public string MessageFormat { get; }
	public string Category { get; }
	public DiagnosticSeverity Severity { get; }
	public CachableLocation Location { get; }
	public object[] MessageArgs { get; }
}
