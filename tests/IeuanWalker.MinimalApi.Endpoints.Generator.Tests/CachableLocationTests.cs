using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Shouldly;

namespace IeuanWalker.MinimalApi.Endpoints.Generator.Tests;

public sealed class CachableLocationTests
{
	[Fact]
	public void FromLocation_WithValidLocation_CreatesInstance()
	{
		// Arrange
		string filePath = "C:\\test\\file.cs";
		LinePositionSpan lineSpan = new(
			new LinePosition(5, 10),
			new LinePosition(5, 20));
		Location location = Location.Create(filePath, default, lineSpan);

		// Act
		CachableLocation cachableLocation = CachableLocation.FromLocation(location);

		// Assert - Verify by converting back to Location
		Location reconstructed = cachableLocation.ToLocation();
		FileLinePositionSpan reconstructedSpan = reconstructed.GetLineSpan();
		reconstructedSpan.Path.ShouldBe(filePath);
		reconstructedSpan.StartLinePosition.Line.ShouldBe(5);
		reconstructedSpan.StartLinePosition.Character.ShouldBe(10);
		reconstructedSpan.EndLinePosition.Line.ShouldBe(5);
		reconstructedSpan.EndLinePosition.Character.ShouldBe(20);
	}

	[Fact]
	public void FromLocation_WithNullLocation_ThrowsArgumentNullException()
	{
		// Act & Assert
		Should.Throw<ArgumentNullException>(() => CachableLocation.FromLocation(null!));
	}

	[Fact]
	public void FromLocation_WithLocationWithoutSourceTree_UsesEmptyFilePath()
	{
		// Arrange
		LinePositionSpan lineSpan = new(
			new LinePosition(1, 0),
			new LinePosition(2, 5));
		Location location = Location.Create(string.Empty, default, lineSpan);

		// Act
		CachableLocation cachableLocation = CachableLocation.FromLocation(location);

		// Assert
		Location reconstructed = cachableLocation.ToLocation();
		FileLinePositionSpan reconstructedSpan = reconstructed.GetLineSpan();
		reconstructedSpan.Path.ShouldBe(string.Empty);
		reconstructedSpan.StartLinePosition.Line.ShouldBe(1);
		reconstructedSpan.StartLinePosition.Character.ShouldBe(0);
		reconstructedSpan.EndLinePosition.Line.ShouldBe(2);
		reconstructedSpan.EndLinePosition.Character.ShouldBe(5);
	}

	[Fact]
	public void FromLocation_WithMultiLineLocation_PreservesAllLinePositions()
	{
		// Arrange
		string filePath = "/unix/path/file.cs";
		LinePositionSpan lineSpan = new(
			new LinePosition(10, 5),
			new LinePosition(15, 30));
		Location location = Location.Create(filePath, default, lineSpan);

		// Act
		CachableLocation cachableLocation = CachableLocation.FromLocation(location);

		// Assert
		Location reconstructed = cachableLocation.ToLocation();
		FileLinePositionSpan reconstructedSpan = reconstructed.GetLineSpan();
		reconstructedSpan.Path.ShouldBe(filePath);
		reconstructedSpan.StartLinePosition.Line.ShouldBe(10);
		reconstructedSpan.StartLinePosition.Character.ShouldBe(5);
		reconstructedSpan.EndLinePosition.Line.ShouldBe(15);
		reconstructedSpan.EndLinePosition.Character.ShouldBe(30);
	}

	[Fact]
	public void ToLocation_ReconstructsLocationWithSameLineSpan()
	{
		// Arrange
		string filePath = "test.cs";
		LinePositionSpan originalLineSpan = new(
			new LinePosition(3, 7),
			new LinePosition(3, 15));
		Location originalLocation = Location.Create(filePath, default, originalLineSpan);
		CachableLocation cachableLocation = CachableLocation.FromLocation(originalLocation);

		// Act
		Location reconstructedLocation = cachableLocation.ToLocation();

		// Assert
		reconstructedLocation.GetLineSpan().Path.ShouldBe(filePath);
		reconstructedLocation.GetLineSpan().StartLinePosition.Line.ShouldBe(3);
		reconstructedLocation.GetLineSpan().StartLinePosition.Character.ShouldBe(7);
		reconstructedLocation.GetLineSpan().EndLinePosition.Line.ShouldBe(3);
		reconstructedLocation.GetLineSpan().EndLinePosition.Character.ShouldBe(15);
	}

	[Fact]
	public void Equals_WithSameValues_ReturnsTrue()
	{
		// Arrange
		string filePath = "file.cs";
		LinePositionSpan lineSpan = new(
			new LinePosition(1, 2),
			new LinePosition(3, 4));
		Location location = Location.Create(filePath, default, lineSpan);

		CachableLocation location1 = CachableLocation.FromLocation(location);
		CachableLocation location2 = CachableLocation.FromLocation(location);

		// Act & Assert
		location1.Equals(location2).ShouldBeTrue();
		(location1 == location2).ShouldBeTrue();
		(location1 != location2).ShouldBeFalse();
	}

	[Fact]
	public void Equals_WithDifferentFilePath_ReturnsFalse()
	{
		// Arrange
		LinePositionSpan lineSpan = new(
			new LinePosition(1, 2),
			new LinePosition(3, 4));
		Location location1 = Location.Create("file1.cs", default, lineSpan);
		Location location2 = Location.Create("file2.cs", default, lineSpan);

		CachableLocation cachable1 = CachableLocation.FromLocation(location1);
		CachableLocation cachable2 = CachableLocation.FromLocation(location2);

		// Act & Assert
		cachable1.Equals(cachable2).ShouldBeFalse();
		(cachable1 == cachable2).ShouldBeFalse();
		(cachable1 != cachable2).ShouldBeTrue();
	}

	[Fact]
	public void Equals_WithDifferentStartLine_ReturnsFalse()
	{
		// Arrange
		string filePath = "file.cs";
		LinePositionSpan lineSpan1 = new(
			new LinePosition(1, 2),
			new LinePosition(3, 4));
		LinePositionSpan lineSpan2 = new(
			new LinePosition(2, 2),
			new LinePosition(3, 4));
		Location location1 = Location.Create(filePath, default, lineSpan1);
		Location location2 = Location.Create(filePath, default, lineSpan2);

		CachableLocation cachable1 = CachableLocation.FromLocation(location1);
		CachableLocation cachable2 = CachableLocation.FromLocation(location2);

		// Act & Assert
		cachable1.Equals(cachable2).ShouldBeFalse();
	}

	[Fact]
	public void Equals_WithDifferentStartCharacter_ReturnsFalse()
	{
		// Arrange
		string filePath = "file.cs";
		LinePositionSpan lineSpan1 = new(
			new LinePosition(1, 2),
			new LinePosition(3, 4));
		LinePositionSpan lineSpan2 = new(
			new LinePosition(1, 3),
			new LinePosition(3, 4));
		Location location1 = Location.Create(filePath, default, lineSpan1);
		Location location2 = Location.Create(filePath, default, lineSpan2);

		CachableLocation cachable1 = CachableLocation.FromLocation(location1);
		CachableLocation cachable2 = CachableLocation.FromLocation(location2);

		// Act & Assert
		cachable1.Equals(cachable2).ShouldBeFalse();
	}

	[Fact]
	public void Equals_WithDifferentEndLine_ReturnsFalse()
	{
		// Arrange
		string filePath = "file.cs";
		LinePositionSpan lineSpan1 = new(
			new LinePosition(1, 2),
			new LinePosition(3, 4));
		LinePositionSpan lineSpan2 = new(
			new LinePosition(1, 2),
			new LinePosition(4, 4));
		Location location1 = Location.Create(filePath, default, lineSpan1);
		Location location2 = Location.Create(filePath, default, lineSpan2);

		CachableLocation cachable1 = CachableLocation.FromLocation(location1);
		CachableLocation cachable2 = CachableLocation.FromLocation(location2);

		// Act & Assert
		cachable1.Equals(cachable2).ShouldBeFalse();
	}

	[Fact]
	public void Equals_WithDifferentEndCharacter_ReturnsFalse()
	{
		// Arrange
		string filePath = "file.cs";
		LinePositionSpan lineSpan1 = new(
			new LinePosition(1, 2),
			new LinePosition(3, 4));
		LinePositionSpan lineSpan2 = new(
			new LinePosition(1, 2),
			new LinePosition(3, 5));
		Location location1 = Location.Create(filePath, default, lineSpan1);
		Location location2 = Location.Create(filePath, default, lineSpan2);

		CachableLocation cachable1 = CachableLocation.FromLocation(location1);
		CachableLocation cachable2 = CachableLocation.FromLocation(location2);

		// Act & Assert
		cachable1.Equals(cachable2).ShouldBeFalse();
	}

	[Fact]
	public void Equals_WithObject_ComparesCorrectly()
	{
		// Arrange
		string filePath = "file.cs";
		LinePositionSpan lineSpan = new(
			new LinePosition(1, 2),
			new LinePosition(3, 4));
		Location location = Location.Create(filePath, default, lineSpan);

		CachableLocation cachable1 = CachableLocation.FromLocation(location);
		CachableLocation cachable2 = CachableLocation.FromLocation(location);
		object cachable2AsObject = cachable2;

		// Act & Assert
		cachable1.Equals(cachable2AsObject).ShouldBeTrue();
	}

	[Fact]
	public void Equals_WithNull_ReturnsFalse()
	{
		// Arrange
		string filePath = "file.cs";
		LinePositionSpan lineSpan = new(
			new LinePosition(1, 2),
			new LinePosition(3, 4));
		Location location = Location.Create(filePath, default, lineSpan);
		CachableLocation cachable = CachableLocation.FromLocation(location);

		// Act & Assert
		cachable.Equals(null).ShouldBeFalse();
	}

	[Fact]
	public void Equals_WithDifferentType_ReturnsFalse()
	{
		// Arrange
		string filePath = "file.cs";
		LinePositionSpan lineSpan = new(
			new LinePosition(1, 2),
			new LinePosition(3, 4));
		Location location = Location.Create(filePath, default, lineSpan);
		CachableLocation cachable = CachableLocation.FromLocation(location);
		object differentType = "string";

		// Act & Assert
		cachable.Equals(differentType).ShouldBeFalse();
	}

	[Fact]
	public void GetHashCode_WithSameValues_ReturnsSameHashCode()
	{
		// Arrange
		string filePath = "file.cs";
		LinePositionSpan lineSpan = new(
			new LinePosition(1, 2),
			new LinePosition(3, 4));
		Location location = Location.Create(filePath, default, lineSpan);

		CachableLocation cachable1 = CachableLocation.FromLocation(location);
		CachableLocation cachable2 = CachableLocation.FromLocation(location);

		// Act & Assert
		cachable1.GetHashCode().ShouldBe(cachable2.GetHashCode());
	}

	[Fact]
	public void GetHashCode_WithDifferentValues_ReturnsDifferentHashCode()
	{
		// Arrange
		LinePositionSpan lineSpan1 = new(
			new LinePosition(1, 2),
			new LinePosition(3, 4));
		LinePositionSpan lineSpan2 = new(
			new LinePosition(5, 6),
			new LinePosition(7, 8));
		Location location1 = Location.Create("file1.cs", default, lineSpan1);
		Location location2 = Location.Create("file2.cs", default, lineSpan2);

		CachableLocation cachable1 = CachableLocation.FromLocation(location1);
		CachableLocation cachable2 = CachableLocation.FromLocation(location2);

		// Act & Assert
		cachable1.GetHashCode().ShouldNotBe(cachable2.GetHashCode());
	}

	[Fact]
	public void GetHashCode_WithEmptyFilePath_ReturnsConsistentHashCode()
	{
		// Arrange
		LinePositionSpan lineSpan = new(
			new LinePosition(0, 0),
			new LinePosition(1, 1));
		Location location = Location.Create(string.Empty, default, lineSpan);

		CachableLocation cachable1 = CachableLocation.FromLocation(location);
		CachableLocation cachable2 = CachableLocation.FromLocation(location);

		// Act & Assert
		cachable1.GetHashCode().ShouldBe(cachable2.GetHashCode());
	}

	[Fact]
	public void OperatorEquals_WithEqualValues_ReturnsTrue()
	{
		// Arrange
		string filePath = "file.cs";
		LinePositionSpan lineSpan = new(
			new LinePosition(1, 2),
			new LinePosition(3, 4));
		Location location = Location.Create(filePath, default, lineSpan);

		CachableLocation cachable1 = CachableLocation.FromLocation(location);
		CachableLocation cachable2 = CachableLocation.FromLocation(location);

		// Act & Assert
		(cachable1 == cachable2).ShouldBeTrue();
	}

	[Fact]
	public void OperatorNotEquals_WithDifferentValues_ReturnsTrue()
	{
		// Arrange
		LinePositionSpan lineSpan = new(
			new LinePosition(1, 2),
			new LinePosition(3, 4));
		Location location1 = Location.Create("file1.cs", default, lineSpan);
		Location location2 = Location.Create("file2.cs", default, lineSpan);

		CachableLocation cachable1 = CachableLocation.FromLocation(location1);
		CachableLocation cachable2 = CachableLocation.FromLocation(location2);

		// Act & Assert
		(cachable1 != cachable2).ShouldBeTrue();
	}

	[Fact]
	public void RoundTrip_Location_To_CachableLocation_To_Location_PreservesData()
	{
		// Arrange
		string filePath = "example.cs";
		LinePositionSpan originalLineSpan = new(
			new LinePosition(42, 13),
			new LinePosition(42, 26));
		Location originalLocation = Location.Create(filePath, default, originalLineSpan);

		// Act
		CachableLocation cachable = CachableLocation.FromLocation(originalLocation);
		Location reconstructed = cachable.ToLocation();

		// Assert
		FileLinePositionSpan originalSpan = originalLocation.GetLineSpan();
		FileLinePositionSpan reconstructedSpan = reconstructed.GetLineSpan();

		reconstructedSpan.Path.ShouldBe(originalSpan.Path);
		reconstructedSpan.StartLinePosition.ShouldBe(originalSpan.StartLinePosition);
		reconstructedSpan.EndLinePosition.ShouldBe(originalSpan.EndLinePosition);
	}

	[Theory]
	[InlineData(0, 0, 0, 0)]
	[InlineData(100, 50, 100, 75)]
	[InlineData(1000, 0, 1500, 0)]
	public void FromLocation_WithVariousLinePositions_PreservesCorrectly(int startLine, int startChar, int endLine, int endChar)
	{
		// Arrange
		LinePositionSpan lineSpan = new(
			new LinePosition(startLine, startChar),
			new LinePosition(endLine, endChar));
		Location location = Location.Create("test.cs", default, lineSpan);

		// Act
		CachableLocation cachable = CachableLocation.FromLocation(location);

		// Assert - Verify by converting back
		Location reconstructed = cachable.ToLocation();
		FileLinePositionSpan reconstructedSpan = reconstructed.GetLineSpan();
		reconstructedSpan.StartLinePosition.Line.ShouldBe(startLine);
		reconstructedSpan.StartLinePosition.Character.ShouldBe(startChar);
		reconstructedSpan.EndLinePosition.Line.ShouldBe(endLine);
		reconstructedSpan.EndLinePosition.Character.ShouldBe(endChar);
	}
}
