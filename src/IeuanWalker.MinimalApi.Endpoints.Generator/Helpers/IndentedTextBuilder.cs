using System.CodeDom.Compiler;

namespace IeuanWalker.MinimalApi.Endpoints.Generator.Helpers;

sealed class IndentedTextBuilder : IDisposable
{
	readonly StringWriter _output;
	readonly IndentedTextWriter _writer;

	public IndentedTextBuilder()
	{
		_output = new StringWriter();
		_writer = new IndentedTextWriter(_output);
	}

	public void Append(string value) => _writer.Write(value);

	public void AppendLine(string value) => _writer.WriteLine(value);

	public void AppendLine() => _writer.WriteLine();

	public void IncreaseIndent() => _writer.Indent++;

	public void DecreaseIndent() => _writer.Indent--;

	public override string ToString() => _output.ToString();

	public Block AppendBlock(bool endWithNewLine = true)
	{
		AppendLine("{");
		IncreaseIndent();

		return new(this, endWithNewLine);
	}

	public Block AppendBlock(string value, bool endWithNewLine = true)
	{
		AppendLine(value);
		return AppendBlock(endWithNewLine);
	}

	public void Dispose()
	{
		_output.Dispose();
		_writer.Dispose();
	}
}

struct Block(IndentedTextBuilder? builder, bool endWithNewLine = true) : IDisposable
{
	IndentedTextBuilder? _builder = builder;
	readonly bool _endWithNewLine = endWithNewLine;

	public void Dispose()
	{
		IndentedTextBuilder? builder1 = _builder;

		_builder = null;

		if (builder1 is not null)
		{
			builder1.DecreaseIndent();

			if (_endWithNewLine)
			{
				builder1.AppendLine("}");
			}
			else
			{
				builder1.Append("}");
			}
		}
	}
}