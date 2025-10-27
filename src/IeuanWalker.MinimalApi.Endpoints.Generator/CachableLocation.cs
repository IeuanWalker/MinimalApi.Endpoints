using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace IeuanWalker.MinimalApi.Endpoints.Generator;

/// <summary>
/// A cache-friendly wrapper around Location that extracts and stores only the essential data.
/// This allows the incremental generator to cache diagnostics without holding references to syntax trees.
/// Based on: https://github.com/dotnet/roslyn/issues/62269, implementation from: https://gist.github.com/dferretti/9d41651178a847ccf56dc2c5f9ab788f
/// </summary>
public readonly struct CachableLocation : IEquatable<CachableLocation>
{
	readonly TextSpan _sourceSpan;
	readonly FileLinePositionSpan _fileLineSpan;

	CachableLocation(TextSpan sourceSpan, FileLinePositionSpan fileLineSpan)
	{
		_sourceSpan = sourceSpan;
		_fileLineSpan = fileLineSpan;
	}

	public static CachableLocation FromLocation(Location location)
	{
		if (location is null)
		{
			throw new ArgumentNullException(nameof(location));
		}

		return new(location.SourceSpan, location.GetLineSpan());
	}

	public Location ToLocation()
		=> Location.Create(_fileLineSpan.Path, _sourceSpan, _fileLineSpan.Span);

	public bool Equals(CachableLocation other)
		=> _sourceSpan.Equals(other._sourceSpan)
		&& _fileLineSpan.Equals(other._fileLineSpan);

	public override bool Equals(object obj) => obj is CachableLocation x && this.Equals(x);

	public static bool operator ==(CachableLocation left, CachableLocation right)
		=> left.Equals(right);

	public static bool operator !=(CachableLocation left, CachableLocation right)
		=> !left.Equals(right);

	public override int GetHashCode() => (37 * _sourceSpan.GetHashCode()) + _fileLineSpan.GetHashCode();
}
