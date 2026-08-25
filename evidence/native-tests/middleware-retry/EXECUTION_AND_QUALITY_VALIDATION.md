# Execution and quality validation — middleware retry

## Final subject

- native Abstractions owner: `tests2/ViciOne.ServiceBus.Abstractions.Tests`;
- native Core owner: `tests2/ViciOne.ServiceBus.Tests`;
- complete hermetic profile: `ViciOne.ServiceBus.Tests.Unit.slnx`;
- local infrastructure profile: `ViciOne.ServiceBus.Tests.LocalIntegration.slnx`;
- replaced inherited cohort: the 17 obligations in `INHERITED_BEHAVIOR_DISPOSITION.json`,
  `Retry_Specs.cs` and its seven exclusive support files.

## Final results

| Gate | Result |
| --- | --- |
| UnitArchitecture Release build with analyzers | exit 0; 0 warnings; 0 errors |
| Abstractions test project | 203/203 passed; 0 failed; 0 skipped |
| Core test project | 703/703 passed; 0 failed; 0 skipped |
| UnitArchitecture profile | 1297/1297 passed; 0 failed; 0 skipped |
| LocalIntegration Release build with analyzers | exit 0; 0 warnings; 0 errors |
| LocalIntegration profile | 3/3 passed; 0 failed; 0 skipped |
| Engineering solution Release build with analyzers | exit 0; 0 warnings; 0 errors |
| Remaining inherited Core test project after eight deletions | exit 0; 0 warnings; 0 errors |
| Bounded .NET whitespace format verification | exit 0 |
| Effective one-cause mutation probes | 9/9 rejected for the intended reason |

The UnitArchitecture and LocalIntegration rows are unfiltered post-deletion runs. The local profile
used `VICIONE_TESTS__Profile=LocalIntegration`. No product condition, profile, filter, skip or
expected count was relaxed. The 1297 floor was predeclared from the previous 1243 cases plus the 54
source-mirrored retry cases before the acceptance run.

Static review found no wall-clock delay, process-time oracle, skip, assertion-free case, blocking
wait, swallowed exception or test-owned product workaround in the new cohort. Every virtual-time
wait has a centrally configured operation timeout and xUnit cancellation token. The requirement
projection and all inherited-disposition files are valid JSON, every disposition target exists,
and `git diff --check` is clean.

The remaining inherited NUnit/VSTest project is compile-only migration evidence under the
repository's enforced MTP runner; it is not a second executable acceptance path. Its successful
post-deletion build proves that the eight removed files had no retained consumer.

Verdict: **PASS**.
