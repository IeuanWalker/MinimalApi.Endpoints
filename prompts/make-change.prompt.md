title: make-change
description: A reusable prompt template for requesting code changes or fixes in the repository.

template:
  - placeholders:
      - files: A list of target file paths or a glob (e.g., src/Handlers/*.cs)
      - change_description: A clear description of the requested change
      - tests_required: yes/no (indicate whether tests should be added)
      - run_ci: yes/no (indicate whether to run CI after changes)

  - instructions: |
      You are a repository-aware assistant. Produce a minimal, idiomatic C# change that implements the requested change_description in the listed files.
      - If code is changed, produce a unified diff or file patches with context.
      - If tests_required is "yes", include new or updated test files.
      - If run_ci is "yes", invoke the ci-run skill after the changes are prepared.

expected_output:
  - format: "patch" (unified diff) or "file set" (new/updated files with contents)
  - include: brief rationale, tests added/updated, and any follow-up manual steps.

example:
  files: src/Endpoints/ProductsEndpoint.cs
  change_description: Fix null reference when product image is missing and return default URL
  tests_required: yes
  run_ci: yes
