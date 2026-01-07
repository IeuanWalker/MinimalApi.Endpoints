using System.Diagnostics.CodeAnalysis;

namespace ExampleApi.Endpoints.Validation.PostWithValidation;

[ExcludeFromCodeCoverage]
public class RequestModel
{
	public string StringMin { get; set; } = string.Empty;
	public string StringMax { get; set; } = string.Empty;
	public string StringRange { get; set; } = string.Empty;
	public string StringPattern { get; set; } = string.Empty;

	public int IntMin { get; set; }
	public int IntMax { get; set; }
	public int IntRange { get; set; }

	public double DoubleMin { get; set; }
	public double DoubleMax { get; set; }
	public double DoubleRange { get; set; }

	public List<string> ListStringMinCount { get; set; } = [];
	public List<string> ListStringMaxCount { get; set; } = [];
	public List<string> ListStringRangeCount { get; set; } = [];

	public List<int> ListIntMinCount { get; set; } = [];
	public List<int> ListIntMaxCount { get; set; } = [];
	public List<int> ListIntRangeCount { get; set; } = [];

	public required string AllRules { get; set; }

	public required NestedObjectModel NestedObject { get; set; }
	public List<NestedObjectModel>? ListNestedObject { get; set; }
}

[ExcludeFromCodeCoverage]
public class NestedObjectModel
{
	public string StringMin { get; set; } = string.Empty;
	public string StringMax { get; set; } = string.Empty;
	public string StringRange { get; set; } = string.Empty;
	public string StringPattern { get; set; } = string.Empty;

	public int IntMin { get; set; }
	public int IntMax { get; set; }
	public int IntRange { get; set; }

	public double DoubleMin { get; set; }
	public double DoubleMax { get; set; }
	public double DoubleRange { get; set; }

	public List<string> ListStringMinCount { get; set; } = [];
	public List<string> ListStringMaxCount { get; set; } = [];
	public List<string> ListStringRangeCount { get; set; } = [];

	public List<int> ListIntMinCount { get; set; } = [];
	public List<int> ListIntMaxCount { get; set; } = [];
	public List<int> ListIntRangeCount { get; set; } = [];
}
