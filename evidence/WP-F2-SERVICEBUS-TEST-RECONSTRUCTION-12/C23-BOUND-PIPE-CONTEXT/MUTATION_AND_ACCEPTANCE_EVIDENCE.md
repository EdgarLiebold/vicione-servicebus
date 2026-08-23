# C23 bound pipe context — mutation and acceptance evidence

## Frozen technical subject

- Commit: `20daede33f98425429fd429c3f44fadebf0921b0`
- Tree: `6802bb12a40725ce5072dafcc8b64219871e32c4`
- Parent: `5bd8575239620076e7ffd240b5a26f061e570467`
- Branch: `test/servicebus-xunit4-mtp2-a-plus-v2`

The technical delta changes no file below `src/`, no project or solution file, no package lock file,
and no repository build contract. It replaces one fully mapped inherited fixture and raises the
materialized Unit/Architecture floor from 796 to 797.

## Obligation closure

The deleted `tests/ViciOne.ServiceBus.Tests/Middleware/Bind_Specs.cs` had SHA-256
`4b299f121f98acfdb662fa8da0ab3ab2044d828a42296ee78d7ca64267f817d0`. Its only final R0
obligation, `OBL-R0-CORE-B-0319`, is mapped to one ordinary xUnit fact under the Core configuration
owner. The replacement awaits the public `UseBind` operation directly and asserts:

- the source receives the exact original input context;
- `ContextPipe`, the bound pipe, and the following outer segment observe the correct context;
- the bound context retains the exact left and right object identities and values;
- execution order is exactly `context`, `bound`, `next`.

The inherited completion source, five-second timeout, console output, and assertion-free success
criterion are not reproduced. The final ledger SHA-256 is
`5b7134255db42f7fdb4276598b4790660059d006e13bd184db8fe5cf6bf51a66`.

## One-cause negative probes

Each probe was applied independently, executed through the native xUnit 4 / Microsoft Testing
Platform 2 Core project, observed red for its intended reason, and restored before final acceptance.

| Probe | Result |
|---|---|
| Replace the configured bound output with an empty pipe | 1 failed, 281 passed |
| Remove the left context from `BindContextProxy` | 1 failed, 281 passed |
| Remove the right context from `BindContextProxy` | 1 failed, 281 passed |
| Skip the following outer pipe segment on the synchronous source path | 1 failed, 281 passed |
| Drift the compiled requirement variant from its embedded projection | 1 failed, 281 passed |

## Final acceptance

- focused Core project: 282 total, 282 passed, 0 failed, 0 skipped;
- Unit/Architecture Release build: 0 warnings, 0 errors;
- unfiltered Unit/Architecture profile with `--minimum-expected-tests 797`: 797 total, 797 passed,
  0 failed, 0 skipped;
- LocalIntegration Release build: 0 warnings, 0 errors;
- unfiltered LocalIntegration profile with `--minimum-expected-tests 3`: 3 total, 3 passed,
  0 failed, 0 skipped;
- formatter verification, JSON parsing, exact ledger/disposition cardinality, forbidden-pattern
  scan, and `git diff --check`: passed.

## Bound file hashes

| File | SHA-256 |
|---|---|
| `tests2/ViciOne.ServiceBus.Tests/Configuration/BindPipeSpecificationTests.cs` | `56e24bc6ddf2fe9b4af8d8b08b55ae6059dbb094f8b8f81e087780a7c26e88c0` |
| `tests2/ViciOne.ServiceBus.Tests/Requirements/CoreRequirements.json` | `6cacf1a45ed121b358485eef3d8d34b76feb58f4212f4b739beae14b348263fe` |
| `evidence/native-tests/bound-pipe-context/INHERITED_BEHAVIOR_DISPOSITION.json` | `cdf7d58500f7f5609161ca9c22f46645fa392eedfad78f718ae9001cb251236b` |
