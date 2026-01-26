using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using FluentValidation;
using IeuanWalker.MinimalApi.Endpoints;
using Microsoft.AspNetCore.Mvc;

namespace ExampleApi.Endpoints.Enum.GetFromQuery;

[ExcludeFromCodeCoverage]
public class RequestModel
{
	[FromQuery]
	public PriorityEnum PlainEnum { get; set; }
	[FromQuery]
	public PriorityEnum? NullableEnum { get; set; }
	[FromQuery]
	public PriorityEnum PlainEnumWithoutFluentValidation { get; set; }
	[FromQuery]
	public PriorityEnum? NullableEnumWithoutFluentValidation { get; set; }
	[FromQuery]
	public string EnumAsString { get; set; } = string.Empty;
	[FromQuery]
	public string? NullableEnumAsString { get; set; }
	[FromQuery]
	public int EnumAsInt { get; set; }
	[FromQuery]
	public int? NullableEnumAsInt { get; set; }
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

[ExcludeFromCodeCoverage]
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
