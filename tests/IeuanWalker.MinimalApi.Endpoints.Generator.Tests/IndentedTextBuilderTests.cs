namespace IeuanWalker.MinimalApi.Endpoints.Generator.Tests;

public sealed class IndentedTextBuilderTests : IDisposable
{
	readonly IndentedTextBuilder _builder;

	public IndentedTextBuilderTests()
	{
		_builder = new IndentedTextBuilder();
	}

	public void Dispose()
	{
		_builder.Dispose();
		GC.SuppressFinalize(this);
	}

	[Fact]
	public void Constructor_CreatesEmptyBuilder()
	{
		// Act & Assert
		_builder.ToString().ShouldBe(string.Empty);
	}

	[Fact]
	public void Append_AddsTextWithoutNewLine()
	{
		// Act
		_builder.Append("Hello");
		_builder.Append(" World");

		// Assert
		_builder.ToString().ShouldBe("Hello World");
	}

	[Fact]
	public void AppendLine_WithValue_AddsTextWithNewLine()
	{
		// Act
		_builder.AppendLine("Hello World");

		// Assert
		_builder.ToString().ShouldBe("Hello World\r\n");
	}

	[Fact]
	public void AppendLine_WithoutValue_AddsOnlyNewLine()
	{
		// Act
		_builder.AppendLine();

		// Assert
		_builder.ToString().ShouldBe("\r\n");
	}

	[Fact]
	public void IncreaseIndent_AddsIndentationToSubsequentLines()
	{
		// Act
		_builder.IncreaseIndent();
		_builder.AppendLine("Indented");
		_builder.AppendLine("Also Indented");

		// Assert
		_builder.ToString().ShouldBe("\tIndented\r\n\tAlso Indented\r\n");
	}

	[Fact]
	public void DecreaseIndent_RemovesIndentationFromSubsequentLines()
	{
		// Arrange
		_builder.IncreaseIndent();
		_builder.IncreaseIndent();
		_builder.AppendLine("Double Indented");

		// Act
		_builder.DecreaseIndent();
		_builder.AppendLine("Single Indented");

		// Assert
		_builder.ToString().ShouldBe("\t\tDouble Indented\r\n\tSingle Indented\r\n");
	}

	[Fact]
	public void MultipleIndentLevels_WorkCorrectly()
	{
		// Act
		_builder.AppendLine("No Indent");
		_builder.IncreaseIndent();
		_builder.AppendLine("Level 1");
		_builder.IncreaseIndent();
		_builder.AppendLine("Level 2");
		_builder.IncreaseIndent();
		_builder.AppendLine("Level 3");
		_builder.DecreaseIndent();
		_builder.AppendLine("Back to Level 2");
		_builder.DecreaseIndent();
		_builder.AppendLine("Back to Level 1");
		_builder.DecreaseIndent();
		_builder.AppendLine("Back to No Indent");

		// Assert
		const string expected = "No Indent\r\n" +
						  "\tLevel 1\r\n" +
						  "\t\tLevel 2\r\n" +
						  "\t\t\tLevel 3\r\n" +
						  "\t\tBack to Level 2\r\n" +
						  "\tBack to Level 1\r\n" +
						  "Back to No Indent\r\n";

		_builder.ToString().ShouldBe(expected);
	}

	[Fact]
	public void AppendBlock_WithDefaultParameters_CreatesBlockWithNewLineEnding()
	{
		// Act
		using Block block = _builder.AppendBlock();
		_builder.AppendLine("Inside block");

		// Assert - Check during block scope
		_builder.ToString().ShouldBe("{\r\n\tInside block\r\n");

		// Block will be closed when exiting using scope
	}

	[Fact]
	public void AppendBlock_AfterDispose_ClosesBlockWithNewLine()
	{
		// Act
		using (Block block = _builder.AppendBlock())
		{
			_builder.AppendLine("Inside block");
		}

		// Assert - Block should be closed after dispose
		_builder.ToString().ShouldBe("{\r\n\tInside block\r\n}\r\n");
	}

	[Fact]
	public void AppendBlock_WithEndWithNewLineFalse_ClosesBlockWithoutNewLine()
	{
		// Act
		using (Block block = _builder.AppendBlock(endWithNewLine: false))
		{
			_builder.AppendLine("Inside block");
		}

		// Assert
		_builder.ToString().ShouldBe("{\r\n\tInside block\r\n}");
	}

	[Fact]
	public void AppendBlock_WithValue_AddsValueBeforeOpeningBrace()
	{
		// Act
		using (Block block = _builder.AppendBlock("public class Test"))
		{
			_builder.AppendLine("// Class content");
		}

		// Assert
		_builder.ToString().ShouldBe("public class Test\r\n{\r\n\t// Class content\r\n}\r\n");
	}

	[Fact]
	public void AppendBlock_WithValueAndEndWithNewLineFalse_WorksCorrectly()
	{
		// Act
		using (Block block = _builder.AppendBlock("public void Method()", endWithNewLine: false))
		{
			_builder.AppendLine("// Method content");
		}

		// Assert
		_builder.ToString().ShouldBe("public void Method()\r\n{\r\n\t// Method content\r\n}");
	}

	[Fact]
	public void NestedBlocks_WorkCorrectly()
	{
		// Act
		using (Block outerBlock = _builder.AppendBlock("namespace Test"))
		{
			_builder.AppendLine();
#pragma warning disable IDE0063 // Use simple 'using' statement
			using (Block innerBlock = _builder.AppendBlock("public class MyClass"))
			{
				_builder.AppendLine("public int Id { get; set; }");
				_builder.AppendLine();
				using (Block methodBlock = _builder.AppendBlock("public void DoSomething()"))
				{
					_builder.AppendLine("// Method implementation");
				}
			}
#pragma warning restore IDE0063 // Use simple 'using' statement
		}

		// Assert
		const string expected = "namespace Test\r\n" +
						  "{\r\n" +
						  "\t\r\n" +
						  "\tpublic class MyClass\r\n" +
						  "\t{\r\n" +
						  "\t\tpublic int Id { get; set; }\r\n" +
						  "\t\t\r\n" +
						  "\t\tpublic void DoSomething()\r\n" +
						  "\t\t{\r\n" +
						  "\t\t\t// Method implementation\r\n" +
						  "\t\t}\r\n" +
						  "\t}\r\n" +
						  "}\r\n";

		_builder.ToString().ShouldBe(expected);
	}

	[Fact]
	public void Block_DoubleDispose_DoesNotThrow()
	{
		// Act & Assert - Should not throw
		Block block = _builder.AppendBlock();
		block.Dispose();
		block.Dispose(); // Second dispose should be safe
	}

	[Fact]
	public void Block_DisposeWithoutBuilder_DoesNotThrow()
	{
		// Arrange
		Block block = new(null);

		// Act & Assert - Should not throw
		block.Dispose();
	}

	[Fact]
	public void ToString_ReturnsCurrentContent()
	{
		// Arrange
		_builder.AppendLine("Line 1");
		_builder.Append("Line 2");

		// Act
		string result1 = _builder.ToString();
		_builder.AppendLine(" continued");
		string result2 = _builder.ToString();

		// Assert
		result1.ShouldBe("Line 1\r\nLine 2");
		result2.ShouldBe("Line 1\r\nLine 2 continued\r\n");
	}

	[Fact]
	public void MixedAppendOperations_WorkCorrectly()
	{
		// Act
		_builder.Append("Start");
		_builder.AppendLine(" of line");
		_builder.IncreaseIndent();
		_builder.Append("Indented");
		_builder.Append(" text");
		_builder.AppendLine();
		_builder.AppendLine("Another indented line");

		// Assert
		_builder.ToString().ShouldBe("Start of line\r\n\tIndented text\r\n\tAnother indented line\r\n");
	}

	[Fact]
	public void EmptyBlocks_WorkCorrectly()
	{
		// Act
		using (Block block = _builder.AppendBlock("Empty Block"))
		{
			// No content added
		}

		// Assert
		_builder.ToString().ShouldBe("Empty Block\r\n{\r\n}\r\n");
	}

	[Fact]
	public void Dispose_ReleasesResources()
	{
		// Arrange
		IndentedTextBuilder builder = new();
		builder.AppendLine("Test content");

		// Act
		builder.Dispose();

		// Assert - Should not throw when accessing ToString after dispose
		// Note: This tests that Dispose doesn't break the object immediately
		// The actual resource cleanup is internal
		builder.ToString().ShouldBe("Test content\r\n");
	}

	[Fact]
	public void AppendEmptyLine_AddsBlankLineWithoutIndentation()
	{
		// Act
		_builder.IncreaseIndent();
		_builder.AppendLine("Indented line");
		_builder.AppendEmptyLine();
		_builder.AppendLine("Another indented line");

		// Assert
		_builder.ToString().ShouldBe("\tIndented line\r\n\r\n\tAnother indented line\r\n");
	}

	[Fact]
	public void AppendEmptyLine_WithoutIndentation_AddsBlankLine()
	{
		// Act
		_builder.AppendLine("First line");
		_builder.AppendEmptyLine();
		_builder.AppendLine("Third line");

		// Assert
		_builder.ToString().ShouldBe("First line\r\n\r\nThird line\r\n");
	}

	[Fact]
	public void AppendLine_WithIndentation_AddsIndentedBlankLine()
	{
		// Act
		_builder.IncreaseIndent();
		_builder.AppendLine("Indented line");
		_builder.AppendLine(); // This should add indented blank line
		_builder.AppendLine("Another indented line");

		// Assert
		_builder.ToString().ShouldBe("\tIndented line\r\n\t\r\n\tAnother indented line\r\n");
	}
}


public class BlockTests
{
	[Fact]
	public void Block_WithNullBuilder_DisposeDoesNotThrow()
	{
		// Arrange
		Block block = new(null);

		// Act & Assert - Should not throw
		Should.NotThrow(() => block.Dispose());
	}

	[Fact]
	public void Block_WithNullBuilderAndEndWithNewLineFalse_DisposeDoesNotThrow()
	{
		// Arrange
		Block block = new(null, false);

		// Act & Assert - Should not throw
		Should.NotThrow(() => block.Dispose());
	}

	[Fact]
	public void Block_MultipleDispose_DoesNotThrow()
	{
		// Arrange
		using IndentedTextBuilder builder = new();
		Block block = new(builder);

		// Act & Assert - Should not throw on multiple disposes
		Should.NotThrow(() =>
		{
			block.Dispose();
			block.Dispose();
			block.Dispose();
		});
	}

	[Fact]
	public void Block_WithBuilder_DisposesCorrectly()
	{
		// Arrange
		using IndentedTextBuilder builder = new();
		builder.AppendLine("{");
		builder.IncreaseIndent();
		builder.AppendLine("Content");

		// Act
		Block block = new(builder, true);
		block.Dispose();

		// Assert
		builder.ToString().ShouldBe("{\r\n\tContent\r\n}\r\n");
	}

	[Fact]
	public void Block_WithBuilderEndWithNewLineFalse_DisposesWithoutNewLine()
	{
		// Arrange
		using IndentedTextBuilder builder = new();
		builder.AppendLine("{");
		builder.IncreaseIndent();
		builder.AppendLine("Content");

		// Act
		Block block = new(builder, false);
		block.Dispose();

		// Assert
		builder.ToString().ShouldBe("{\r\n\tContent\r\n}");
	}

	[Fact]
	public void Block_DefaultConstructor_UsesDefaultValues()
	{
		// Arrange & Act
		Block block = new();

		// Assert - Should not throw when disposed
		Should.NotThrow(() => block.Dispose());
	}

	[Fact]
	public void Block_ConstructorWithOnlyBuilder_DefaultsEndWithNewLineToTrue()
	{
		// Arrange
		using IndentedTextBuilder builder = new();
		builder.AppendLine("{");
		builder.IncreaseIndent();
		builder.AppendLine("Content");

		// Act
		Block block = new(builder); // endWithNewLine should default to true
		block.Dispose();

		// Assert - Should end with newline by default
		builder.ToString().ShouldBe("{\r\n\tContent\r\n}\r\n");
	}

	[Fact]
	public void Block_AfterFirstDispose_SubsequentDisposesDoNothing()
	{
		// Arrange
		using IndentedTextBuilder builder = new();
		builder.AppendLine("{");
		builder.IncreaseIndent();
		builder.AppendLine("Content");

		Block block = new(builder, true);

		// Act
		block.Dispose(); // First dispose
		string afterFirstDispose = builder.ToString();

		block.Dispose(); // Second dispose
		string afterSecondDispose = builder.ToString();

		// Assert - Content should be the same after multiple disposes
		afterFirstDispose.ShouldBe("{\r\n\tContent\r\n}\r\n");
		afterSecondDispose.ShouldBe(afterFirstDispose);
	}

	[Fact]
	public void Block_HandlesIndentationCorrectly()
	{
		// Arrange
		using IndentedTextBuilder builder = new();
		builder.IncreaseIndent(); // Start with some indentation
		builder.AppendLine("{");
		builder.IncreaseIndent(); // This will be handled by the block

		// Act
		Block block = new(builder, true);
		builder.AppendLine("Nested content");
		block.Dispose();

		// Assert - Should decrease indent and close block at correct level
		builder.ToString().ShouldBe("\t{\r\n\t\tNested content\r\n\t}\r\n");
	}
}
