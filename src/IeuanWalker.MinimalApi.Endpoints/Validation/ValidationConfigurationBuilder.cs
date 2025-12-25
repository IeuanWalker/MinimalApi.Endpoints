using System.Linq.Expressions;

namespace IeuanWalker.MinimalApi.Endpoints.Validation;

/// <summary>
/// Fluent builder for configuring validation rules for a request type
/// </summary>
public sealed class ValidationConfigurationBuilder<TRequest>
{
	readonly List<object> _propertyBuilders = [];
	readonly List<Func<TRequest, Dictionary<string, string[]>>> _crossFieldValidators = [];

	/// <summary>
	/// Configures validation rules for a property
	/// </summary>
	public PropertyValidationBuilder<TRequest, TProperty> Property<TProperty>(
		Expression<Func<TRequest, TProperty>> propertySelector)
	{
		string propertyName = GetPropertyName(propertySelector);
		PropertyValidationBuilder<TRequest, TProperty> builder = new(propertyName);
		_propertyBuilders.Add(builder);
		return builder;
	}

	/// <summary>
	/// Adds a cross-field validator that validates relationships between properties
	/// </summary>
	public ValidationConfigurationBuilder<TRequest> CrossField(
		Func<TRequest, Dictionary<string, string[]>> validator)
	{
		_crossFieldValidators.Add(validator);
		return this;
	}

	internal ValidationConfiguration<TRequest> Build()
	{
		List<ValidationRule> allRules = [];

		foreach (object builder in _propertyBuilders)
		{
			// Use reflection to call Build() on each property builder
			System.Reflection.MethodInfo? buildMethod = builder.GetType().GetMethod("Build",
				System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

			if (buildMethod != null)
			{
				if (buildMethod.Invoke(builder, null) is IEnumerable<ValidationRule> rules)
				{
					allRules.AddRange(rules);
				}
			}
		}

		return new ValidationConfiguration<TRequest>(allRules, _crossFieldValidators);
	}

	static string GetPropertyName<TProperty>(Expression<Func<TRequest, TProperty>> expression)
	{
		return expression.Body switch
		{
			MemberExpression member => member.Member.Name,
			_ => throw new ArgumentException("Expression must be a property selector", nameof(expression))
		};
	}
}
