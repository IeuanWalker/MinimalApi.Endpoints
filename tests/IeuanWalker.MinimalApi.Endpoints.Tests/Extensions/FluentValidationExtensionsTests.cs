using FluentValidation;
using FluentValidation.Results;

namespace IeuanWalker.MinimalApi.Endpoints.Tests.Extensions;

public class FluentValidationExtensionsTests
{
	[Fact]
	public void IsInEnum_NonEnumType_ThrowsArgumentException()
	{
		// Arrange + Act + Assert
		Should.Throw<ArgumentException>(() => new NonEnumValidator())
			.ParamName.ShouldBe("enumType");
	}

	[Fact]
	public void IsInEnum_NonEnumTypeNullable_ThrowsArgumentException()
	{
		// Arrange + Act + Assert
		Should.Throw<ArgumentException>(() => new NonEnumNullableValidator())
			.ParamName.ShouldBe("enumType");
	}

	[Fact]
	public void IsInEnum_IntValue_ValidAndInvalid()
	{
		// Arrange
		IntModelValidator validator = new();
		IntModel valid = new()
		{
			Value = (int)TestEnum.One
		};
		IntModel invalid = new()
		{
			Value = 999
		};

		// Act
		ValidationResult resultValid = validator.Validate(valid);
		ValidationResult resultInvalid = validator.Validate(invalid);

		// Assert
		resultValid.IsValid.ShouldBeTrue();
		resultInvalid.IsValid.ShouldBeFalse();
		resultInvalid.Errors.Single().ErrorMessage.ShouldBe("Value must be a valid value of enum TestEnum.");
	}

	[Fact]
	public void IsInEnum_NullableIntValue_AllCases()
	{
		// Arrange
		NullableIntModelValidator validator = new();
		NullableIntModel nullModel = new()
		{
			Value = null
		};
		NullableIntModel validModel = new()
		{
			Value = (int)TestEnum.Zero
		};
		NullableIntModel invalidModel = new()
		{
			Value = 1234
		};

		// Act & Assert
		ValidationResult resultValid1 = validator.Validate(nullModel);
		ValidationResult resultValid2 = validator.Validate(validModel);
		ValidationResult invalidResult = validator.Validate(invalidModel);

		// Assert
		resultValid1.IsValid.ShouldBeTrue();
		resultValid2.IsValid.ShouldBeTrue();
		invalidResult.IsValid.ShouldBeFalse();
		invalidResult.Errors.Single().ErrorMessage.ShouldBe("Value must be empty or a valid value of enum TestEnum.");
	}


	enum TestEnum
	{
		Zero = 0,
		One = 1
	}

	class IntModel
	{
		public int Value { get; set; }
	}

	class NullableIntModel
	{
		public int? Value { get; set; }
	}

	class NonEnumValidator : AbstractValidator<IntModel>
	{
		public NonEnumValidator()
		{
			// Passing a non-enum type should throw in the extension
			RuleFor(x => x.Value).IsInEnum(typeof(string));
		}
	}
	class NonEnumNullableValidator : AbstractValidator<NullableIntModel>
	{
		public NonEnumNullableValidator()
		{
			// Passing a non-enum type should throw in the extension
			RuleFor(x => x.Value).IsInEnum(typeof(string));
		}
	}

	class IntModelValidator : AbstractValidator<IntModel>
	{
		public IntModelValidator()
		{
			RuleFor(x => x.Value).IsInEnum(typeof(TestEnum));
		}
	}

	class NullableIntModelValidator : AbstractValidator<NullableIntModel>
	{
		public NullableIntModelValidator()
		{
			RuleFor(x => x.Value).IsInEnum(typeof(TestEnum));
		}
	}
}
