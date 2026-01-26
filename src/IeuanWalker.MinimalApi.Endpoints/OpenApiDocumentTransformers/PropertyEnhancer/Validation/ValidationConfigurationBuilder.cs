using System.Linq.Expressions;
using System.Reflection;

namespace IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers.PropertyEnhancer.Validation;

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
		Dictionary<string, IReadOnlyList<ValidationRuleOperation>> operationsByProperty = [];

		foreach (object builder in _propertyBuilders)
		{
			// Use reflection to get the property name
			FieldInfo? propertyNameField = builder.GetType().GetField("_propertyName", BindingFlags.Instance | BindingFlags.NonPublic);
			string? propertyName = propertyNameField?.GetValue(builder) as string;

			// Use reflection to call Build() on each property builder
			MethodInfo? buildMethod = builder.GetType().GetMethod("Build", BindingFlags.Instance | BindingFlags.NonPublic);

			if (buildMethod is not null && buildMethod.Invoke(builder, null) is IEnumerable<ValidationRule> rules)
			{
				allRules.AddRange(rules);
			}

			// Get operations from the property builder
			MethodInfo? getOperationsMethod = builder.GetType().GetMethod("GetOperations", BindingFlags.Instance | BindingFlags.NonPublic);
			if (getOperationsMethod is not null &&
				getOperationsMethod.Invoke(builder, null) is IReadOnlyList<ValidationRuleOperation> operations &&
				operations.Count > 0 &&
				propertyName is not null)
			{
				operationsByProperty[propertyName] = operations;
			}
		}

		return new ValidationConfiguration<TRequest>(allRules, _appendRulesToPropertyDescription, operationsByProperty);
	}

	static string GetPropertyName<TProperty>(Expression<Func<TRequest, TProperty>> expression)
	{
		return GetPropertyPath(expression.Body);
	}

	static string GetPropertyPath(Expression expr)
	{
		return expr switch
		{
			MemberExpression member => BuildMemberPath(member),
			MethodCallExpression methodCall when IsArrayIndexer(methodCall) => BuildArrayIndexerPath(methodCall),
			_ => throw new ArgumentException($"Expression must be a property selector (e.g., x => x.Property) or array indexer (e.g., x => x.Array[0]). Got: {expr.GetType().Name}")
		};
	}

	static string BuildMemberPath(MemberExpression member)
	{
		List<string> parts = [];
		Expression? current = member;

		// Walk up the expression tree collecting property names
		while (current is not null)
		{
			if (current is MemberExpression memberExpr)
			{
				parts.Insert(0, memberExpr.Member.Name);
				current = memberExpr.Expression;
			}
			else if (current is MethodCallExpression methodCall && IsArrayIndexer(methodCall))
			{
				// Hit an array indexer, continue building path through it
				string indexerPath = BuildArrayIndexerPath(methodCall);
				parts.Insert(0, indexerPath);
				break;
			}
			else
			{
				// Hit the parameter (e.g., x in x => x.Property)
				break;
			}
		}

		if (parts.Count == 0)
		{
			throw new ArgumentException("Expression must be a property selector");
		}

		return string.Join(".", parts);
	}

	static bool IsArrayIndexer(MethodCallExpression methodCall)
	{
		// Check if this is an array/list indexer call (e.g., list[0])
		return methodCall.Method.Name == "get_Item";
	}

	static string BuildArrayIndexerPath(MethodCallExpression methodCall)
	{
		// For array indexer like x.ListNestedObject[0].StringMin
		// We mark the path with [*] to indicate array item validation
		// e.g., "ListNestedObject[*]"

		Expression collectionExpr = methodCall.Object ?? throw new ArgumentException(
			$"Array indexer expression must have a collection object. Expression: {methodCall}");

		return GetPropertyPath(collectionExpr) + "[*]";
	}
}
