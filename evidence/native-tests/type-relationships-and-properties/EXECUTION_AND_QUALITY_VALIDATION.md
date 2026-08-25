# Execution and quality validation — type relationships and readable properties

## Final subject

- native Abstractions owner: `tests2/ViciOne.ServiceBus.Abstractions.Tests`;
- native Core owner: `tests2/ViciOne.ServiceBus.Tests`;
- complete hermetic profile: `ViciOne.ServiceBus.Tests.Unit.slnx`;
- local infrastructure profile: `ViciOne.ServiceBus.Tests.LocalIntegration.slnx`;
- retained product closure: all 22 `src/**/*.csproj` projects, including the nine retained projects
  not yet represented in `ViciOne.ServiceBus.Engineering.slnx`.

## Final results

| Gate | Result |
| --- | --- |
| Abstractions Release build | exit 0; 0 warnings; 0 errors |
| Abstractions test project | 191/191 passed; 0 failed; 0 skipped |
| Core Release build | exit 0; 0 warnings; 0 errors |
| Core test project | 618/618 passed; 0 failed; 0 skipped |
| UnitArchitecture profile | 1200/1200 passed; 0 failed; 0 skipped |
| LocalIntegration profile | 3/3 passed; 0 failed; 0 skipped |
| Engineering solution Release build | exit 0; 0 warnings; 0 errors |
| Nine retained source projects outside that solution | all 9 built; 0 warnings; 0 errors |
| Remaining inherited Core test project after four deletions | exit 0; 0 warnings; 0 errors |

The UnitArchitecture and LocalIntegration rows are the final post-deletion runs. No filtered or
skipped result is accepted as the cohort gate. The local profile ran with
`VICIONE_TESTS__Profile=LocalIntegration`; Microsoft Testing Platform required its normal local
named-pipe permission, but no product or test condition was relaxed.
