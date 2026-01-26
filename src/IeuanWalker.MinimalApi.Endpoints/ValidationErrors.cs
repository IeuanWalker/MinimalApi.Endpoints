using System.Linq.Expressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace IeuanWalker.MinimalApi.Endpoints;

/// <summary>
/// Collects validation errors and formats them as HTTP problem details.
/// </summary>
/// <typeparam name="T">The request model type used for property expressions.</typeparam>
public sealed class ValidationErrors<T>
{
	readonly Dictionary<string, List<string>> _errors = [];

	/// <summary>
	/// Returns true when at least one error has been added.
	/// </summary>
	public bool HasErrors()
	{
		return _errors.Count != 0;
	}

	/// <summary>
	/// Adds one or more error messages for a property expression.
	/// </summary>
	/// <param name="property">The property expression to build the error key.</param>
	/// <param name="messages">One or more error messages.</param>
	/// <returns>The same <see cref="ValidationErrors{T}"/> instance for chaining.</returns>
	/// <exception cref="ArgumentException">Thrown when no messages are provided or the expression is invalid.</exception>
	public ValidationErrors<T> Add(Expression<Func<T, object>> property, params string[] messages)
	{
		ValidateMessages(messages);
		string name = GetPropertyPath(property.Body);
		return Add(name, messages);
	}

	/// <summary>
	/// Adds one or more error messages for a specific key.
	/// </summary>
	/// <param name="key">The error key to associate with the messages.</param>
	/// <param name="messages">One or more error messages.</param>
	/// <returns>The same <see cref="ValidationErrors{T}"/> instance for chaining.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="key"/> is null or whitespace.</exception>
	/// <exception cref="ArgumentException">Thrown when no messages are provided.</exception>
	public ValidationErrors<T> Add(string key, params string[] messages)
	{
		ArgumentNullException.ThrowIfNullOrWhiteSpace(key, nameof(key));
		ValidateMessages(messages);
		if (!_errors.TryGetValue(key, out List<string>? list))
		{
			list = [];
			_errors[key] = list;
		}
		list.AddRange(messages);
		return this;
	}

	/// <summary>
	/// Creates <see cref="HttpValidationProblemDetails"/> from the collected errors.
	/// </summary>
	/// <returns>A validation problem details object.</returns>
	public HttpValidationProblemDetails ToProblemDetails()
	{
		Dictionary<string, string[]> errorDict = _errors.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToArray());
		return new HttpValidationProblemDetails(errorDict);
	}

	/// <summary>
	/// Returns a problem response containing the validation errors.
	/// </summary>
	/// <returns>A <see cref="ProblemHttpResult"/> with validation details.</returns>
	public ProblemHttpResult ToProblemResponse()
	{
		return TypedResults.Problem(ToProblemDetails());
	}

	static string GetPropertyPath(Expression expression)
	{
		Stack<string> members = new();
		while (expression is MemberExpression memberExpr)
		{
			members.Push(memberExpr.Member.Name);
			expression = memberExpr.Expression!;
		}
		if (expression is ParameterExpression)
		{
			return string.Join(".", members);
		}

		if (expression is UnaryExpression unary && unary.Operand is MemberExpression)
		{
			return GetPropertyPath(unary.Operand);
		}

		if (expression is MethodCallExpression call && call.Method.Name == "get_Item")
		{
			if (call.Object is MemberExpression memberObject)
			{
				string basePath = GetPropertyPath(memberObject);
				string indexedPath = $"{basePath}[{call.Arguments[0]}]";
				if (members.Count == 0)
				{
					return indexedPath;
				}

				return $"{indexedPath}.{string.Join(".", members)}";
			}

			throw new ArgumentException("Indexer expressions are only supported on direct member access, e.g. x => x.Items[0]");
		}

		throw new ArgumentException("Expression must select a (possibly nested) property, e.g. x => x.Prop1.Prop2");
	}

	static void ValidateMessages(string[] messages)
	{
		if (messages.Length == 0)
		{
			throw new ArgumentException("At least one validation message must be provided.", nameof(messages));
		}
	}
}
