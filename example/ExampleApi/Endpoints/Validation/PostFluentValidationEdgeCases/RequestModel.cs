using System.Diagnostics.CodeAnalysis;
using FluentValidation;
using IeuanWalker.MinimalApi.Endpoints;

namespace ExampleApi.Endpoints.Validation.PostFluentValidationEdgeCases;

[ExcludeFromCodeCoverage]
public class RequestModel
{
	public required string Key { get; init; }
	public required string Language { get; set; }
	public long? Uprn { get; set; }
	public double? Easting { get; set; }
	public double? Northing { get; set; }
	public bool IsWelsh() => Language.Equals("cy-GB", StringComparison.InvariantCultureIgnoreCase);
}

[ExcludeFromCodeCoverage]
public class RequestValidator : Validator<RequestModel>
{
	public RequestValidator()
	{
		RuleFor(x => x.Language)
			.Must(value => value.Equals("en-GB") || value.Equals("cy-GB"))
			.WithMessage("Must be one of the following: en-GB, cy-GB");

		RuleFor(x => x.Uprn)
			.Must((request, uprn) =>
			{
				return uprn is not null;
			})
			.WithMessage("Must provide UPRN if required by chosen key");
	}
}
