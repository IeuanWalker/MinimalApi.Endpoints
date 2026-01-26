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

	[Fact]
	public void IsInEnum_IntValue_NegativeValue_Invalid()
	{
		// Arrange
		IntModelValidator validator = new();
		IntModel model = new()
		{
			Value = -1
		};

		// Act
		ValidationResult result = validator.Validate(model);

		// Assert
		result.IsValid.ShouldBeFalse();
		result.Errors.Single().ErrorMessage.ShouldBe("Value must be a valid value of enum TestEnum.");
	}

	[Fact]
	public void IsInEnum_IntValue_ZeroBoundaryValue_Valid()
	{
		// Arrange
		IntModelValidator validator = new();
		IntModel model = new()
		{
			Value = 0 // TestEnum.Zero
		};

		// Act
		ValidationResult result = validator.Validate(model);

		// Assert
		result.IsValid.ShouldBeTrue();
	}

	[Fact]
	public void IsInEnum_IntValue_MaxEnumBoundaryValue_Valid()
	{
		// Arrange
		IntModelValidator validator = new();
		IntModel model = new()
		{
			Value = 1 // TestEnum.One (max value in TestEnum)
		};

		// Act
		ValidationResult result = validator.Validate(model);

		// Assert
		result.IsValid.ShouldBeTrue();
	}

	[Fact]
	public void IsInEnum_IntValue_ValueJustOutsideMaxBoundary_Invalid()
	{
		// Arrange
		IntModelValidator validator = new();
		IntModel model = new()
		{
			Value = 2 // One past the max enum value
		};

		// Act
		ValidationResult result = validator.Validate(model);

		// Assert
		result.IsValid.ShouldBeFalse();
	}

	[Fact]
	public void IsInEnum_NullableIntValue_NegativeValue_Invalid()
	{
		// Arrange
		NullableIntModelValidator validator = new();
		NullableIntModel model = new()
		{
			Value = -100
		};

		// Act
		ValidationResult result = validator.Validate(model);

		// Assert
		result.IsValid.ShouldBeFalse();
		result.Errors.Single().ErrorMessage.ShouldBe("Value must be empty or a valid value of enum TestEnum.");
	}

	[Fact]
	public void IsInEnum_NullableIntValue_ZeroValue_Valid()
	{
		// Arrange
		NullableIntModelValidator validator = new();
		NullableIntModel model = new()
		{
			Value = 0
		};

		// Act
		ValidationResult result = validator.Validate(model);

		// Assert
		result.IsValid.ShouldBeTrue();
	}

	[Fact]
	public void IsInEnum_IntValue_MaxIntValue_Invalid()
	{
		// Arrange
		IntModelValidator validator = new();
		IntModel model = new()
		{
			Value = int.MaxValue
		};

		// Act
		ValidationResult result = validator.Validate(model);

		// Assert
		result.IsValid.ShouldBeFalse();
	}

	[Fact]
	public void IsInEnum_IntValue_MinIntValue_Invalid()
	{
		// Arrange
		IntModelValidator validator = new();
		IntModel model = new()
		{
			Value = int.MinValue
		};

		// Act
		ValidationResult result = validator.Validate(model);

		// Assert
		result.IsValid.ShouldBeFalse();
	}

	[Fact]
	public void IsInEnum_WithGapsInEnumValues_ValidatesCorrectly()
	{
		// Arrange
		GappedEnumModelValidator validator = new();
		GappedEnumModel validModel1 = new() { Value = 0 };
		GappedEnumModel validModel2 = new() { Value = 10 };
		GappedEnumModel validModel3 = new() { Value = 100 };
		GappedEnumModel invalidModel = new() { Value = 5 }; // Between gaps

		// Act
		ValidationResult result1 = validator.Validate(validModel1);
		ValidationResult result2 = validator.Validate(validModel2);
		ValidationResult result3 = validator.Validate(validModel3);
		ValidationResult resultInvalid = validator.Validate(invalidModel);

		// Assert
		result1.IsValid.ShouldBeTrue();
		result2.IsValid.ShouldBeTrue();
		result3.IsValid.ShouldBeTrue();
		resultInvalid.IsValid.ShouldBeFalse();
	}

	[Fact]
	public void IsInEnum_NonEnumType_ThrowsWithCorrectMessage()
	{
		// Arrange + Act
		ArgumentException exception = Should.Throw<ArgumentException>(() => new NonEnumValidator());

		// Assert
		exception.Message.ShouldContain("IsInEnum can only be used with enum types.");
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

		enum GappedEnum
		{
			Zero = 0,
			Ten = 10,
			Hundred = 100
		}

		class GappedEnumModel
		{
			public int Value { get; set; }
		}

		class GappedEnumModelValidator : AbstractValidator<GappedEnumModel>
		{
			public GappedEnumModelValidator()
			{
				RuleFor(x => x.Value).IsInEnum(typeof(GappedEnum));
			}
		}
	}
