# Work package A research

## Frozen input

- Product commit: `22877f6cbc485e7df098a971054805b142ccbd46`
- Product tree: `3db5b100a3afa754686687eac95cb0a03bd3f396`
- Assignment SHA-256: `b2089002f71ebadd9f7a575bbc4b9b8fd456436a73b53f4e63dd210586a362fa`
- Branch: `feature/servicebus-a-plus-api`
- Initial status: only the untracked, PO-owned `review/` directory

## Bounded target inventory

- 4,824 C# files below `src`, `tests`, `samples`, `benchmarks`, and `tools`.
- 64 product and test project files below `src` and `tests`.
- Root build policy does not yet enable nullable references, implicit usings, warning-as-error, style
  enforcement, or deterministic output.
- The root documentation warning exemption is global instead of project-local.
- The repository editor policy lacks the required file-scoped namespace and using-placement rules.
- `.testagent/` contains 39 tracked obligation and working-state files (approximately 1.2 MiB).
- The Roslyn static source/test pairing heuristic found 4,036 source files and 788 test files. It
  paired 1,259 source files and reported 2,777 without a direct namespace-qualified symbol reference.
  This inventory prioritizes later test analysis and is not line, branch, or behavioral coverage.

## Existing test conventions

- Native tests use xUnit 4 on Microsoft Testing Platform 2.
- Test execution is selected by `global.json`; .NET 10 commands therefore use named
  `--solution`/`--project` arguments and no argument separator.
- The hermetic acceptance profile is `ViciOne.ServiceBus.Tests.Unit.slnx` in Release configuration.
- Tests must have no skips, warnings, or secondary verdict mechanism.

## Acceptance checklist

- Central build and editor policy matches the work-package contract.
- All in-scope C# is formatted with enforced file-scoped namespace and using rules.
- No product source contains `#nullable disable`.
- Nullable is enabled or temporarily set to annotations with a package-B removal note.
- Compiler warnings are fixed; any retained pragma has a local technical justification.
- Product comments contain no assignment or test-process vocabulary.
- The complete `.testagent/` contents are preserved under the native-test evidence tree and the
  original directory no longer exists.
- Build commands and the minimum unit-test count are consistent in the three allowed documents.
- All three Release builds pass with warnings treated as errors, formatter verification is clean,
  and the unchanged unit profile passes identically three times.
