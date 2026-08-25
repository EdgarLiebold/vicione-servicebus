# C35 validation

## Final stationary state

| Subject | SHA-256 |
|---|---|
| `src/ViciOne.ServiceBus/InMemoryTransport/InMemoryTransport/InMemoryReceiveTransport.cs` | `3f64737aa497c41f6359514f90cc3390e243dcd76503c9a57839f15c9e1e6c05` |
| `tests2/ViciOne.ServiceBus.Tests/InMemoryTransport/InMemoryReceiveEndpointConcurrencyTests.cs` | `5f8abda718bef50ff82712d5456fb61b14ba89e94a30212d3b2a924c2469363a` |
| `tests2/ViciOne.ServiceBus.Tests/Requirements/CoreRequirements.json` | `73884fb6dd7a3f7d9d2d1b1b6d62970bce72971ec43e82dea9b53daf4cbc6198` |

## Executed gates

| Gate | Result |
|---|---|
| focused Release project build after restoring the mutation | PASS — 0 warnings, 0 errors |
| focused native MTP class | PASS — 2/2, 0 failed, 0 skipped |
| serial `ViciOne.ServiceBus.Tests.Unit.slnx` Release build | PASS — 0 warnings, 0 errors |
| unfiltered UnitArchitecture native MTP profile | PASS — 1468/1468, 0 failed, 0 skipped |
| JSON syntax and passive requirement projection | PASS as part of the unfiltered profile |
| forbidden timing/test-smell scan | PASS |
| mutation-residue scan | PASS |
| `git diff --check` | PASS |
| empty inherited-test directory scan | PASS — none |

The old `tests/ViciOne.ServiceBus.Tests/Threading_Specs.cs` fixture was deleted only after both
ordinary replacements, the mutation rejection and the restored focused pass. No commit or push was
performed.
