name: ci-run
description: Run repository CI checks (build, test, lint) and return a summarized result and logs.
inputs:
  - branch: the branch or ref to run checks against (default: the feature branch)
  - commands: list of commands to run (optional, default: dotnet restore; dotnet build; dotnet test)
outputs:
  - status: success | failure
  - summary: one-line summary of results
  - logs: link or inline excerpt of the CI/test output
permissions: Requires GitHub Actions or a runner with access. This skill does not change repository code by itself.
usage_examples:
  - Run default CI on branch "feature/add-health-check": returns status and logs.
  - Run custom commands: ["dotnet format --verify-no-changes", "dotnet test --filter Category!=Integration"]

notes:
  - The implementation of this skill (actual runner/integration) is beyond this markdown and will require wiring into GitHub Actions or external tooling and appropriate credentials.
