# C22 custom pipe specifications — mutation and acceptance evidence

## Frozen technical subject

- Commit: `5d78bdb5e2c0dc345012e68b51ea6e571dee449b`
- Tree: `4af31758ea68c4143d25336d950eb774f8e07e0d`
- Parent: `4c7930387b3786a4ff9a62547bb5d15aaee88f03`
- Branch: `test/servicebus-xunit4-mtp2-a-plus-v2`

The technical delta changes no file below `src/`, no project file, no package lock file, and no
solution or repository build contract. It replaces the fully mapped inherited fixture, raises the
already materialized Unit/Architecture floor from 793 to 796, and qualifies one adjacent product
namespace import whose former relative form became ambiguous below the new `Configuration` test
namespace.

## Source-owner correction and closure

The deleted `tests/ViciOne.ServiceBus.Tests/Middleware/Authentication_Specs.cs` was test-owned sample
code, not a product authentication feature. Its SHA-256 before deletion was
`07b027075bab73c08f38c984b503470174732bbb28e88dc27299267cf8607adc`.

Its two final R0 obligations, `OBL-R0-CORE-B-0317` and `OBL-R0-CORE-B-0318`, describe the public
custom `IPipeSpecification<T>` extension contract. They are mapped one-to-one to two ordinary
xUnit methods under the actual Abstractions owner `Middleware/Configuration`; the validation theory
has two execution rows, for three new execution cases in total. No inherited obligation is left in
the deleted file.

The final ledger SHA-256 is
`5b7134255db42f7fdb4276598b4790660059d006e13bd184db8fe5cf6bf51a66`.

## One-cause negative probes

Each probe was applied independently, executed through the native xUnit 4 / Microsoft Testing
Platform 2 project, observed red for its intended reason, and restored before the final build.

| Probe | Result |
|---|---|
| Skip `IPipeSpecification.Validate()` failure conversion in `Pipe.New` | 2 failed, 128 passed |
| Skip the custom specification while applying the configured specifications | 1 failed, 129 passed |
| Omit the following pipe segment from the test-owned routing filter | 1 failed, 129 passed |
| Accept an empty role collection while still rejecting null | 1 failed, 129 passed |
| Drift the compiled requirement variant from its embedded projection | 1 failed, 129 passed |

The metadata probe failed in
`RequirementCoverageProjectionTests.AbstractionsRequirements_MatchCompiledRequirementMetadata`
with both the unprojected compiled variant and the missing projected variant named explicitly.

## Final acceptance

The following final, unmutated gates passed:

- focused Abstractions project: 130 total, 130 passed, 0 failed, 0 skipped;
- Unit/Architecture Release build: 0 warnings, 0 errors;
- unfiltered Unit/Architecture profile with `--minimum-expected-tests 796`: 796 total, 796 passed,
  0 failed, 0 skipped;
- LocalIntegration Release build: 0 warnings, 0 errors;
- unfiltered LocalIntegration profile with `--minimum-expected-tests 3`: 3 total, 3 passed,
  0 failed, 0 skipped;
- formatter verification for both touched C# files: exit code 0;
- JSON parsing, requirement/disposition cardinality, forbidden-test-pattern scan, and
  `git diff --check`: passed.

## Bound file hashes

| File | SHA-256 |
|---|---|
| `tests2/ViciOne.ServiceBus.Abstractions.Tests/Middleware/Configuration/PipeSpecificationTests.cs` | `ad93b83d06fb855c35987437eee0c8e0a8764e1417d3ae259f681be75908d94b` |
| `tests2/ViciOne.ServiceBus.Abstractions.Tests/Middleware/ExceptionFilters/ExceptionSpecificationTests.cs` | `11c2fdb510f28737624730a5a1a498c25f359d88bd52ce24ed0a48b5fb5a9394` |
| `tests2/ViciOne.ServiceBus.Abstractions.Tests/Requirements/AbstractionsRequirements.json` | `0992103a4f3f23e249782b34885b68e5650b5d07893178a5bb6e0ec6ebdb9ace` |
| `evidence/native-tests/custom-pipe-specifications/INHERITED_BEHAVIOR_DISPOSITION.json` | `3412302f8573b921d15342aa5433ac6c1828a646113e3e34997cc4e4d715bb41` |
