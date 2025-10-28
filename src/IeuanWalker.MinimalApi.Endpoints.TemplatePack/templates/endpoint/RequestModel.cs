#if (validator)
using IeuanWalker.MinimalApi.Endpoints;

namespace NamespaceToReplace;

public sealed class RequestModel
{
	// TODO: Add request properties
	// Example:
	// public int Id { get; set; }
	// public string Name { get; set; } = string.Empty;
}

sealed class RequestModelValidator : Validator<RequestModel>
{
	public RequestModelValidator()
	{
		// TODO: Add validation rules
		// Example:
		// RuleFor(x => x.Name)
		//     .NotEmpty()
		//  .MinimumLength(1)
		//     .MaximumLength(200);
	}
}
#else
namespace NamespaceToReplace;

public sealed class RequestModel
{
	// TODO: Add request properties
	// Example:
	// public int Id { get; set; }
	// public string Name { get; set; } = string.Empty;
}
#endif
