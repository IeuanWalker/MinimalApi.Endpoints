using System.ComponentModel;
using FluentValidation;
using IeuanWalker.MinimalApi.Endpoints;

namespace ExampleApi.Endpoints.Enum.PostFromBody;

public class RequestModel
{
	public PriorityEnum PlainEnum { get; set; }
	public PriorityEnum? NullableEnum { get; set; }
	public string EnumAsString { get; set; } = string.Empty;
	public string NullableEnumAsString { get; set; } = string.Empty;
	public required string EnumAsInt { get; set; }
	public required string NullableEnumAsInt { get; set; }
}

public enum PriorityEnum
{
	[Description("Low priority task")]
	Low = 0,
	[Description("Medium priority task")]
	Medium = 1,
	[Description("High priority task")]
	High = 2,
	[Description("Critical priority task requiring immediate attention")]
	Critical = 3
}

public class RequestModelValidator : Validator<RequestModel>
{
	public RequestModelValidator()
	{
		RuleFor(x => x.EnumAsString)
			.IsEnumName(typeof(PriorityEnum), caseSensitive: false);

		RuleFor(x => x.NullableEnumAsString)
			.IsEnumName(typeof(PriorityEnum), caseSensitive: false);

		RuleFor(x => x.EnumAsInt)
			.IsEnumName(typeof(PriorityEnum), caseSensitive: false);

		RuleFor(x => x.NullableEnumAsInt)
			.IsEnumName(typeof(PriorityEnum), caseSensitive: false);
	}
}
