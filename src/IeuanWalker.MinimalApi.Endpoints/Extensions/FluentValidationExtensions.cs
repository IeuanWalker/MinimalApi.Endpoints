using FluentValidation;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace IeuanWalker.MinimalApi.Endpoints;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public static class FluentValidationEnumExtensions
{
	public static IRuleBuilderOptions<T, int> IsInEnum<T>(this IRuleBuilder<T, int> ruleBuilder, Type enumType)
	{
		if (!enumType.IsEnum)
		{
			throw new ArgumentException("IsInEnum can only be used with enum types.", nameof(enumType));
		}

		return ruleBuilder
			.Must(value => Enum.IsDefined(enumType, value))
			.WithMessage("{PropertyName} must be a valid value of enum " + enumType.Name + ".");
	}

	public static IRuleBuilderOptions<T, int?> IsInEnum<T>(this IRuleBuilder<T, int?> ruleBuilder, Type enumType)
	{
		if (!enumType.IsEnum)
		{
			throw new ArgumentException("IsInEnum can only be used with enum types.", nameof(enumType));
		}

		return ruleBuilder
			.Must(value => !value.HasValue || Enum.IsDefined(enumType, value.Value))
			.WithMessage("{PropertyName} must be empty or a valid value of enum " + enumType.Name + ".");
	}
}
