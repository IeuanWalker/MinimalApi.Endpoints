using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Validation;

namespace ExampleApi.Endpoints.Validation.PostDataAnnotationsFromBody;

#pragma warning disable ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
[ValidatableType]// Required for DataAnnotations validation
#pragma warning restore ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
public class RequestModel
{
	[MinLength(3)]
	public string StringMin { get; set; } = string.Empty;

	[MaxLength(50)]
	public string StringMax { get; set; } = string.Empty;

	[StringLength(50, MinimumLength = 3)]
	public string StringRange { get; set; } = string.Empty;

	[RegularExpression(@"^[a-zA-Z0-9]+$")]
	public string StringPattern { get; set; } = string.Empty;

	[Range(1, int.MaxValue)]
	public int IntMin { get; set; }

	[Range(int.MinValue, 100)]
	public int IntMax { get; set; }

	[Range(1, 100)]
	public int IntRange { get; set; }

	[Range(0.1, double.MaxValue)]
	public double DoubleMin { get; set; }

	[Range(double.MinValue, 99.9)]
	public double DoubleMax { get; set; }

	[Range(0.1, 99.9)]
	public double DoubleRange { get; set; }

	[MinLength(1)]
	public List<string> ListStringMinCount { get; set; } = [];

	[MaxLength(10)]
	public List<string> ListStringMaxCount { get; set; } = [];

	[MinLength(1)]
	[MaxLength(10)]
	public List<string> ListStringRangeCount { get; set; } = [];

	[MinLength(1)]
	public List<int> ListIntMinCount { get; set; } = [];

	[MaxLength(10)]
	public List<int> ListIntMaxCount { get; set; } = [];

	[MinLength(1)]
	[MaxLength(10)]
	public List<int> ListIntRangeCount { get; set; } = [];

	[Required]
	public required NestedObjectModel NestedObject { get; set; }

	public List<NestedObjectModel>? ListNestedObject { get; set; }
}

public class NestedObjectModel
{
	[MinLength(3)]
	public string StringMin { get; set; } = string.Empty;

	[MaxLength(50)]
	public string StringMax { get; set; } = string.Empty;

	[StringLength(50, MinimumLength = 3)]
	public string StringRange { get; set; } = string.Empty;

	[RegularExpression(@"^[a-zA-Z0-9]+$")]
	public string StringPattern { get; set; } = string.Empty;

	[Range(1, int.MaxValue)]
	public int IntMin { get; set; }

	[Range(int.MinValue, 100)]
	public int IntMax { get; set; }

	[Range(1, 100)]
	public int IntRange { get; set; }

	[Range(0.1, double.MaxValue)]
	public double DoubleMin { get; set; }

	[Range(double.MinValue, 99.9)]
	public double DoubleMax { get; set; }

	[Range(0.1, 99.9)]
	public double DoubleRange { get; set; }

	[MinLength(1)]
	public List<string> ListStringMinCount { get; set; } = [];

	[MaxLength(10)]
	public List<string> ListStringMaxCount { get; set; } = [];

	[MinLength(1)]
	[MaxLength(10)]
	public List<string> ListStringRangeCount { get; set; } = [];

	[MinLength(1)]
	public List<int> ListIntMinCount { get; set; } = [];

	[MaxLength(10)]
	public List<int> ListIntMaxCount { get; set; } = [];

	[MinLength(1)]
	[MaxLength(10)]
	public List<int> ListIntRangeCount { get; set; } = [];
}
