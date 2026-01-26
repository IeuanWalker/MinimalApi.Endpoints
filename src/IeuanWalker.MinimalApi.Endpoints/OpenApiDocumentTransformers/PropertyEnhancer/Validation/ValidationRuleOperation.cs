using System.Diagnostics.CodeAnalysis;

namespace IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers.PropertyEnhancer.Validation;

/// <summary>
/// Base class for validation rule operations that can be applied to a list of rules
/// </summary>
[ExcludeFromCodeCoverage]
abstract record ValidationRuleOperation
{
	/// <summary>
	/// Applies the operation to a list of validation rules
	/// </summary>
	/// <param name="rules">The list of rules to modify</param>
	public abstract void Apply(List<ValidationRule> rules);
}

/// <summary>
/// Operation to alter the error message of an existing validation rule
/// </summary>
sealed record AlterOperation : ValidationRuleOperation
{
	public string OldErrorMessage { get; init; }
	public string NewErrorMessage { get; init; }

	public AlterOperation(string oldErrorMessage, string newErrorMessage)
	{
		ArgumentNullException.ThrowIfNullOrWhiteSpace(oldErrorMessage, nameof(oldErrorMessage));
		ArgumentNullException.ThrowIfNullOrWhiteSpace(newErrorMessage, nameof(newErrorMessage));

		OldErrorMessage = oldErrorMessage;
		NewErrorMessage = newErrorMessage;
	}

	public override void Apply(List<ValidationRule> rules)
	{
		ValidationRule? rule = rules.FirstOrDefault(r => r.ErrorMessage == OldErrorMessage)
			?? throw new ArgumentException($"No validation rule exists with error message: '{OldErrorMessage}'");

		rule.ErrorMessage = NewErrorMessage;
	}
}

/// <summary>
/// Operation to remove a validation rule by its error message
/// </summary>
sealed record RemoveOperation : ValidationRuleOperation
{
	public string ErrorMessage { get; init; }

	public RemoveOperation(string errorMessage)
	{
		ArgumentNullException.ThrowIfNullOrWhiteSpace(errorMessage, nameof(errorMessage));

		ErrorMessage = errorMessage;
	}

	public override void Apply(List<ValidationRule> rules)
	{
		ValidationRule? ruleToRemove = rules.FirstOrDefault(r => r.ErrorMessage == ErrorMessage)
			?? throw new ArgumentException($"No validation rule exists with error message: '{ErrorMessage}'");

		rules.Remove(ruleToRemove);
	}
}

/// <summary>
/// Operation to remove all validation rules
/// </summary>
sealed record RemoveAllOperation : ValidationRuleOperation
{
	public override void Apply(List<ValidationRule> rules)
	{
		if (rules.Count == 0)
		{
			throw new InvalidOperationException("No validation rules exist to remove.");
		}

		rules.Clear();
	}
}
