# Endpoint-name formatter acceptance evidence

## Frozen technical subject

- Commit: `c3ea128325a92e39453b572ec8729ed0b9ff6a8e`
- Tree: `09302a43efd4a8caa19f4c485455fd8ba1298038`
- Test source SHA-256: `40fad70790e6f98170c8b8eff2d2a55fb10905d78b727670c2df8c7c0ec013b0`
- Requirement projection SHA-256: `79f6c339b593ed47d4363b022f247f5ba94c25ce3c7d2d443d5d5ae130b41cc7`
- Inherited disposition SHA-256: `c0e9798f0eca563a21904bda69bce1c31d6e5dfc155ebc6b865e21e471e959b9`

The technical commit contains no product-source change. It adds nine ordinary xUnit methods with 17
materialized cases, nine passive requirement-projection rows, and 17 unique terminal inherited
dispositions. It removes only the old fixture whose complete 17-case behavior set is replaced.

Static review found no skip, conditional exclusion, retry, random input, implementation-derived
expected value, empty assertion, TODO, known-bug accommodation, or old TestFramework dependency.
String results are literal independent oracles. Negative cases require the exact product exception
type and its stable semantic reason.

## Positive acceptance runs

All commands used .NET SDK `10.0.302`, locked dependency graphs, Release configuration, and native
Microsoft Testing Platform execution.

| Check | Command/subject | Result |
|---|---|---|
| Bounded format/analyzers | `dotnet format tests2/ViciOne.ServiceBus.Tests/ViciOne.ServiceBus.Tests.csproj --no-restore --include tests2/ViciOne.ServiceBus.Tests/Configuration/EndpointNaming/EndpointNameFormatterTests.cs --verify-no-changes --verbosity minimal` | exit 0 |
| Unit locked restore | `dotnet restore ViciOne.ServiceBus.Tests.Unit.slnx --locked-mode` | exit 0; no lock change |
| Unit Release build | `dotnet build ViciOne.ServiceBus.Tests.Unit.slnx --configuration Release --no-restore --no-incremental --disable-build-servers --maxcpucount:1` | exit 0; 0 warnings; 0 errors |
| UnitArchitecture | unfiltered `dotnet test --solution ViciOne.ServiceBus.Tests.Unit.slnx ... --minimum-expected-tests 635` | 635 passed; 0 failed; 0 skipped |
| Local locked restore | `dotnet restore ViciOne.ServiceBus.Tests.LocalIntegration.slnx --locked-mode` | exit 0; no lock change |
| Local Release build | `dotnet build ViciOne.ServiceBus.Tests.LocalIntegration.slnx --configuration Release --no-restore --no-incremental --disable-build-servers --maxcpucount:1` | exit 0; 0 warnings; 0 errors |
| LocalIntegration | unfiltered native MTP run with `VICIONE_TESTS__Profile=LocalIntegration` and predeclared floor 3 | 3 passed; 0 failed; 0 skipped |

The accepted Unit build binlog has SHA-256
`44e4649291a080cd44a4ecbf6f713284b908b0eebde3e7ebfa9a35028172ed52`; the LocalIntegration
build binlog has SHA-256 `55d4177e2a3e8161a6fdd73fad045302a69cb0168bc4be2aed9a0ac5df31a042`.
An earlier Unit build was intentionally abandoned after a stale CLI MSBuild node stopped making
progress; it produced no verdict and is not acceptance evidence. The retry used a fresh build-server
state and one MSBuild node without changing source, configuration, tests, or gates.

## Product mutation attacks

Each mutation was applied separately to a detached worktree at the frozen technical commit. The
same complete 170-case `ViciOne.ServiceBus.Tests` project ran unfiltered after each one:

`dotnet test --project tests2/ViciOne.ServiceBus.Tests/ViciOne.ServiceBus.Tests.csproj --configuration Release --no-restore --minimum-expected-tests 170`

| Single product mutation | Required detector | Measured verdict |
|---|---|---|
| Replace snake/kebab word-boundary insertion with lowercase-only output | endpoint naming string contracts | exit 2; 11 relevant cases failed; 159 passed; 0 skipped |
| Format a generic consumer from the consumer type instead of its final message argument | `GenericConsumerName_UsesItsMessageType` | exit 2; exactly that case failed; 169 passed; 0 skipped |
| Remove `SanitizeName` from the composed instance identifier | `ConsumerEndpointDefinition_AppendsSanitizedInstanceId` | exit 2; exactly that case failed; 169 passed; 0 skipped |
| Make the empty consumer name bypass its `ConfigurationException` guard | consumer row of `SuffixOnlyEndpointType_IsRejected` | exit 2; exactly that case failed; 169 passed; 0 skipped |

After every run the mutated product line was restored with an exact reverse patch. The detached
worktree then had no tracked diff, proving that no mutant leaked into the accepted subject.

## Product-defect accommodation verdict

PASS for this cohort. The unchanged product satisfied every predeclared literal and exception
oracle. No expectation was relaxed, no failure was left open, and no product defect was worked
around in a test. This verdict is intentionally limited to the endpoint-name formatter cohort; the
repository-wide scope boundary and still-open product findings remain recorded in
`../RETROSPECTIVE_PRODUCT_DEFECT_ACCOMMODATION_AUDIT.md`.
