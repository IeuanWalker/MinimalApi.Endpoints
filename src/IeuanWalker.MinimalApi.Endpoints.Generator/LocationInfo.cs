using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace IeuanWalker.MinimalApi.Endpoints.Generator;

/// <summary>
/// A cache-friendly wrapper around Location that extracts and stores only the essential data.
/// This allows the incremental generator to cache diagnostics without holding references to syntax trees.
/// Based on: https://github.com/dotnet/roslyn/issues/62269
/// </summary>
public readonly struct LocationInfo : IEquatable<LocationInfo>
{
	public LocationInfo(Location location)
	{
		FilePath = location.SourceTree?.FilePath ?? string.Empty;
		StartLine = location.GetLineSpan().StartLinePosition.Line;
		StartCharacter = location.GetLineSpan().StartLinePosition.Character;
		EndLine = location.GetLineSpan().EndLinePosition.Line;
		EndCharacter = location.GetLineSpan().EndLinePosition.Character;
	}

	public string FilePath { get; }
	public int StartLine { get; }
	public int StartCharacter { get; }
	public int EndLine { get; }
	public int EndCharacter { get; }

	/// <summary>
	/// Reconstructs a Location from the stored information.
	/// </summary>
	public Location ToLocation()
	{
		LinePositionSpan lineSpan = new(
			new LinePosition(StartLine, StartCharacter),
			new LinePosition(EndLine, EndCharacter));

		return Location.Create(FilePath, default, lineSpan);
	}

	public bool Equals(LocationInfo other)
	{
		return FilePath == other.FilePath
			&& StartLine == other.StartLine
			&& StartCharacter == other.StartCharacter
			&& EndLine == other.EndLine
			&& EndCharacter == other.EndCharacter;
	}

	public override bool Equals(object? obj)
	{
		return obj is LocationInfo other && Equals(other);
	}

	public override int GetHashCode()
	{
		unchecked
		{
			int hash = 17;
			hash = hash * 23 + (FilePath?.GetHashCode() ?? 0);
			hash = hash * 23 + StartLine.GetHashCode();
			hash = hash * 23 + StartCharacter.GetHashCode();
			hash = hash * 23 + EndLine.GetHashCode();
			hash = hash * 23 + EndCharacter.GetHashCode();
			return hash;
		}
	}

	public static bool operator ==(LocationInfo left, LocationInfo right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(LocationInfo left, LocationInfo right)
	{
		return !left.Equals(right);
	}
}
