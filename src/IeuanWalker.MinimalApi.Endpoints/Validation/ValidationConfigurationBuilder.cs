using System.Linq.Expressions;
using System.Reflection;

namespace IeuanWalker.MinimalApi.Endpoints.Validation;

/// <summary>
/// Fluent builder for configuring validation rules for a request type
/// </summary>
public sealed class ValidationConfigurationBuilder<TRequest>
{
	readonly List<object> _propertyBuilders = [];
	bool _appendRulesToPropertyDescription = true;

	/// <summary>
	/// Configures validation rules for a property
	/// </summary>
	public PropertyValidationBuilder<TRequest, TProperty> Property<TProperty>(Expression<Func<TRequest, TProperty>> propertySelector)
	{
		string propertyName = GetPropertyName(propertySelector);

		PropertyValidationBuilder<TRequest, TProperty> builder = new(propertyName);

		_propertyBuilders.Add(builder);

		return builder;
	}

	/// <summary>
	/// Configures whether validation rules should be listed in the property description field in OpenAPI documentation
	/// </summary>
	/// <param name="appendRules">True to list validation rules in description (default), false to omit them</param>
	public ValidationConfigurationBuilder<TRequest> AppendRulesToPropertyDescription(bool appendRules)
	{
		_appendRulesToPropertyDescription = appendRules;

		return this;
	}

	internal ValidationConfiguration<TRequest> Build()
	{
		List<ValidationRule> allRules = [];

		foreach (object builder in _propertyBuilders)
		{
			// Use reflection to call Build() on each property builder
			MethodInfo? buildMethod = builder.GetType().GetMethod("Build", BindingFlags.Instance | BindingFlags.NonPublic);

			if (buildMethod is not null && buildMethod.Invoke(builder, null) is IEnumerable<ValidationRule> rules)
			{
				allRules.AddRange(rules);
			}
		}

		return new ValidationConfiguration<TRequest>(allRules, _appendRulesToPropertyDescription);
	}

	static string GetPropertyName<TProperty>(Expression<Func<TRequest, TProperty>> expression) => expression.Body switch
	{
		MemberExpression member => member.Member.Name,
		_ => throw new ArgumentException("Expression must be a property selector", nameof(expression))
	};
}
