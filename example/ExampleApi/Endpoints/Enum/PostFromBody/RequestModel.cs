using System.ComponentModel;
using FluentValidation;
using IeuanWalker.MinimalApi.Endpoints;

namespace ExampleApi.Endpoints.Enum.PostFromBody;

public class RequestModel
{
	public PriorityEnum PlainEnum { get; set; }
	public PriorityEnum? NullableEnum { get; set; }
	public PriorityEnum PlainEnumWithoutFluentValidation { get; set; }
	public PriorityEnum? NullableEnumWithoutFluentValidation { get; set; }
	public string EnumAsString { get; set; } = string.Empty;
	public string? NullableEnumAsString { get; set; }
	public int EnumAsInt { get; set; }
	public int? NullableEnumAsInt { get; set; }
	public TestEnum? NullableOnlyEnumTest { get; set; }
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

public enum TestEnum
{
	Test1,
	Test2
}

public class RequestModelValidator : Validator<RequestModel>
{
	public RequestModelValidator()
	{
		RuleFor(x => x.PlainEnum)
			.IsInEnum();

		RuleFor(x => x.NullableEnum)
			.IsInEnum();

		RuleFor(x => x.EnumAsString)
			.IsEnumName(typeof(PriorityEnum), caseSensitive: false);

		RuleFor(x => x.NullableEnumAsString)
			.IsEnumName(typeof(PriorityEnum), caseSensitive: false);

		RuleFor(x => x.EnumAsInt)
			.IsInEnum(typeof(PriorityEnum));

		RuleFor(x => x.NullableEnumAsInt)
			.IsInEnum(typeof(PriorityEnum));
	}
}
