using System;
using Microsoft.CodeAnalysis;

namespace IeuanWalker.MinimalApi.Endpoints.Generator;

/// <summary>
/// Represents diagnostic information to be reported during source generation.
/// This is an immutable value type that can be cached by the incremental generator.
/// </summary>
sealed class DiagnosticInfo : IEquatable<DiagnosticInfo>
{
	public DiagnosticInfo(
		string id,
		string title,
		string messageFormat,
		string category,
		DiagnosticSeverity severity,
		Location location,
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
	public Location Location { get; }
	public object[] MessageArgs { get; }

	public bool Equals(DiagnosticInfo? other)
	{
		if (other is null)
		{
			return false;
		}

		if (ReferenceEquals(this, other))
		{
			return true;
		}

		return Id == other.Id &&
			   Title == other.Title &&
			   MessageFormat == other.MessageFormat &&
			   Category == other.Category &&
			   Severity == other.Severity &&
			   MessageArgsEquals(other.MessageArgs);
	}

	bool MessageArgsEquals(object[] other)
	{
		if (MessageArgs.Length != other.Length)
		{
			return false;
		}

		for (int i = 0; i < MessageArgs.Length; i++)
		{
			if (!Equals(MessageArgs[i], other[i]))
			{
				return false;
			}
		}
		return true;
	}

	public override bool Equals(object? obj) => Equals(obj as DiagnosticInfo);

	public override int GetHashCode()
	{
		unchecked
		{
			int hashCode = Id.GetHashCode();
			hashCode = (hashCode * 397) ^ Title.GetHashCode();
			hashCode = (hashCode * 397) ^ MessageFormat.GetHashCode();
			hashCode = (hashCode * 397) ^ Category.GetHashCode();
			hashCode = (hashCode * 397) ^ Severity.GetHashCode();
			return hashCode;
		}
	}
}
