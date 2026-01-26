using System.Linq.Expressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace IeuanWalker.MinimalApi.Endpoints;

public sealed class ValidationErrors<T>
{
	readonly Dictionary<string, List<string>> _errors = [];

	public bool HasErrors()
	{
		return _errors.Count != 0;
	}

	public ValidationErrors<T> Add(Expression<Func<T, object>> property, params string[] messages)
	{
		string name = GetPropertyPath(property.Body);
		return Add(name, messages);
	}

	public ValidationErrors<T> Add(string key, params string[] messages)
	{
		if (!_errors.TryGetValue(key, out List<string>? list))
		{
			list = [];
			_errors[key] = list;
		}
		list.AddRange(messages);
		return this;
	}

	public HttpValidationProblemDetails ToProblemDetails()
	{
		Dictionary<string, string[]> errorDict = _errors.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToArray());
		return new HttpValidationProblemDetails(errorDict);
	}

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
			members.Push($"{((MemberExpression)call.Object!).Member.Name}[{call.Arguments[0]}]");
			if (call.Object is MemberExpression)
			{
				return string.Join(".", members);
			}
		}

		throw new ArgumentException("Expression must select a (possibly nested) property, e.g. x => x.Prop1.Prop2");
	}
}

//ProblemHttpResult

//	catch(InvalidUprnException)
//		{
//			return new ValidationErrors<RequestModel>()
//				.Add(x => x.Uprn!, "Invalid UPRN")
//				.ToProblemResponse();
//		}
//		catch(InvalidEastingNorthingException)
//		{
//	return new ValidationErrors<RequestModel>()
//		.Add(x => x.Easting!, "Invalid easting")
//		.Add(x => x.Northing!, "Invalid northing")
//		.ToProblemResponse();
//}
