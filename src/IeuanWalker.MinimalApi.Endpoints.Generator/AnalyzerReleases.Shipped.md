; Shipped analyzer releases
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

## Release 1.0.0

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
MINAPI001 | HTTP Verb    | Error   | Has no HTTP verb configured in the Configure method
MINAPI002 | HTTP Verb    | Error   | Has multiple HTTP verbs configured in the Configure method
MINAPI003 | Map group    | Error   | Has no MapGroup configured in the Configure method
MINAPI004 | Map group    | Error   | Has multiple MapGroup calls configured in the Configure method
MINAPI005 | Map group    | Error   | Has multiple Group calls configured in the Configure method
MINAPI006 | Map group    | Warning | Unused endpoint group
MINAPI007 | Validation   | Error   | Has multiple validators for the same type
MINAPI008 | Validation   | Warning | Has validator but disabling validation
MINAPI009 | Validation   | Warning | Using FluentValidation abstract validator on request type
MINAPI010 | Request type | Error   | Has multiple request types configured in the Configure method

