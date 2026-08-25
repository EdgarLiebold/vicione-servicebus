# C36 validation

## Final stationary state

| Subject | SHA-256 |
|---|---|
| `src/ViciOne.ServiceBus/InMemoryTransport/InMemoryTransport/InMemoryReceiveTransport.cs` | `3f64737aa497c41f6359514f90cc3390e243dcd76503c9a57839f15c9e1e6c05` |
| `tests2/ViciOne.ServiceBus.Tests/Introspection/BusProbeTests.cs` | `6c5a0a1c484a454f97c1933a9e3dd3251884037662e073298e0eb23844454ef0` |
| `tests2/ViciOne.ServiceBus.Tests/Requirements/CoreRequirements.json` | `ecf08ed911cf81804faccb3724e25bb04832fb366a7cf585c70f64158e2234cb` |

## Final gates

| Gate | Result |
|---|---|
| focused Release project build after mutation restoration | PASS — 0 warnings, 0 errors |
| focused native MTP class | PASS — 2/2, 0 failed, 0 skipped |
| serial `ViciOne.ServiceBus.Tests.Unit.slnx` Release build | PASS — 0 warnings, 0 errors |
| unfiltered UnitArchitecture native MTP profile | PASS — 1470/1470, 0 failed, 0 skipped |
| unfiltered LocalIntegration native MTP profile | PASS — 3/3, 0 failed, 0 skipped |
| complete serial `ViciOne.ServiceBus.Engineering.slnx` Release build | PASS — 0 warnings, 0 errors |
| JSON syntax and passive requirement projection | PASS as part of the unfiltered profile |
| JSON-free observation and forbidden timing scan | PASS |
| mutation-residue scan | PASS |
| `git diff --check` | PASS |
| empty inherited-test directory scan | PASS — none |

The inherited `tests/ViciOne.ServiceBus.Tests/Introspection_Specs.cs` fixture was deleted only after
the two replacements and the restored focused pass. No commit or push was performed.
