# C6 inherited Abstractions project retirement

## Subject

- technical commit: `8f2a095a21245cc8fc787c75cd8962887a307651`
- technical tree: `68f06e2dc3459d5b07b0e381515a316958a2c2c6`
- parent: `96d98fdf9c017b68c626aa16a0dad86f6723ea55`
- product source changes: none

## Semantic closure

The final R0 ledger contains 78 unique obligations owned by the inherited
`tests/ViciOne.ServiceBus.Abstractions.Tests` project. The active disposition accounts for
all 78 exactly once: 70 have native xUnit/MTP behavior replacements and eight are explicitly
non-product or non-executing. No source-owned obligation is missing or extra.

The deleted `NewId/texts.txt` and the embedded
`NewId/NewIdFormatters/ReferenceCorpus.json` both have SHA-256
`b88ff375e1b4de55130a79a74d3368287c69e4044a4b32106cc8dbc04ae23603`; the
549-value oracle is byte-identical.

The former `Usage/**` files were compile-only sample input, not tests. Their consumer, saga,
routing-slip activity, definitions, contracts, topology exclusion, publish, response, header, and
timestamp shapes are retained under `samples/OrderWorkflow/**`. The sample is source-structured,
non-packable, lockfile-bound, and a member of the Engineering solution. This closes
`OBL-R0-SML-0280` without keeping a false test project.

## Positive acceptance

| Check | Result |
|---|---|
| Sample locked restore | exit 0; one minimal project dependency |
| Sample Release build after final source review | exit 0; 0 warnings; 0 errors |
| UnitArchitecture Release build | exit 0; 0 warnings; 0 errors |
| Unfiltered xUnit 4 / MTP 2 UnitArchitecture run | exit 0; 670 total; 670 passed; 0 failed; 0 skipped |
| Sample and architecture whitespace verification | exit 0 for both projects |
| Solution XML, disposition JSON, and Git whitespace | valid / valid / clean |

The native test command was:

```bash
dotnet test --solution ViciOne.ServiceBus.Tests.Unit.slnx \
  --configuration Release --no-build --no-restore \
  --results-directory artifacts/test-results/unit \
  --minimum-expected-tests 670 --max-parallel-test-modules 1
```

## False-green rejection

Both mutations were applied separately to the technical commit and restored immediately afterwards.

| Mutation | Focused native result |
|---|---|
| Change the OrderWorkflow sample to `IsPackable=true` | exactly `EverySampleProject_IsNotPackable` failed; 1 total, 1 failed, exit 2 |
| Remove the OrderWorkflow project from `ViciOne.ServiceBus.Engineering.slnx` | exactly `EngineeringSolution_ContainsEverySampleProject` failed; 1 total, 1 failed, exit 2 |

After restoration, `git diff --exit-code` and `git diff --check` both passed. No mutation,
generated artifact, inherited runner, NUnit reference, or deleted-source copy remains in the tracked
technical tree.
