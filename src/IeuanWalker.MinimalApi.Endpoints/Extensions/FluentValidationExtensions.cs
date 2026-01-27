using FluentValidation;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace IeuanWalker.MinimalApi.Endpoints;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public static class FluentValidationEnumExtensions
{
	/// <summary>
	/// Validates that the integer value is defined in the specified enum type.
	/// </summary>
	/// <typeparam name="T">The type being validated.</typeparam>
	/// <param name="ruleBuilder">The FluentValidation rule builder.</param>
	/// <param name="enumType">The enum type to validate against.</param>
	/// <returns>The rule builder options so additional rules can be chained.</returns>
	/// <exception cref="ArgumentException">Thrown when <paramref name="enumType"/> is not an enum type.</exception>
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

	/// <summary>
	/// Validates that the nullable integer value is either empty or defined in the specified enum type.
	/// </summary>
	/// <typeparam name="T">The type being validated.</typeparam>
	/// <param name="ruleBuilder">The FluentValidation rule builder.</param>
	/// <param name="enumType">The enum type to validate against.</param>
	/// <returns>The rule builder options so additional rules can be chained.</returns>
	/// <exception cref="ArgumentException">Thrown when <paramref name="enumType"/> is not an enum type.</exception>
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
