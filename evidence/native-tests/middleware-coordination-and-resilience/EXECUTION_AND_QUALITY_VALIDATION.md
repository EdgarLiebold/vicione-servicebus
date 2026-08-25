# Execution and quality validation — middleware coordination and resilience

## Final subject

- native Abstractions owner: `tests2/ViciOne.ServiceBus.Abstractions.Tests`;
- native Core owner: `tests2/ViciOne.ServiceBus.Tests`;
- complete hermetic profile: `ViciOne.ServiceBus.Tests.Unit.slnx`;
- local infrastructure profile: `ViciOne.ServiceBus.Tests.LocalIntegration.slnx`;
- replaced inherited cohort: the 19 obligations in `INHERITED_BEHAVIOR_DISPOSITION.json` and the
  eleven deleted files under `tests/ViciOne.ServiceBus.Tests/Middleware`.

## Final results

| Gate | Result |
| --- | --- |
| Abstractions Release build with analyzers | exit 0; 0 warnings; 0 errors |
| Abstractions test project | 201/201 passed; 0 failed; 0 skipped |
| Core Release build with analyzers | exit 0; 0 warnings; 0 errors |
| Core test project | 643/643 passed; 0 failed; 0 skipped |
| UnitArchitecture Release build with analyzers | exit 0; 0 warnings; 0 errors |
| UnitArchitecture profile | 1235/1235 passed; 0 failed; 0 skipped |
| LocalIntegration Release build with analyzers | exit 0; 0 warnings; 0 errors |
| LocalIntegration profile | 3/3 passed; 0 failed; 0 skipped |
| Engineering solution Release build with analyzers | exit 0; 0 warnings; 0 errors |
| Nine retained source projects outside that solution | all 9 built; 0 warnings; 0 errors |
| Remaining inherited Core test project after eleven deletions | exit 0; 0 warnings; 0 errors |
| Corrected abandoned-fault fact stability run | 10/10 focused repetitions passed |
| Effective one-cause mutation probes | 7/7 rejected for the intended reason |

The UnitArchitecture and LocalIntegration rows are unfiltered post-deletion runs. The local profile
used `VICIONE_TESTS__Profile=LocalIntegration`; its first sandboxed invocation was rejected by the
operating-system socket boundary before any test ran, then the unchanged command passed with the
normal local named-pipe permission. No product or test condition was relaxed.

## Test defects corrected during the full gate

- cancellation accepts the standard `TaskCanceledException` subtype while still checking the exact
  token and canceled task state;
- cache capacity is asserted against its documented soft bound of `Capacity + BucketSize`, not an
  invented hard bound;
- cache cleanup tests use fixed input and advance virtual time before the operation whose timer they
  observe;
- removal observation waits for a fixed expected count rather than a concurrently moving target;
- rate-limit timer ownership is asserted against the configured virtual-time provider rather than
  scheduler timing;
- abandoned-fault observation is anchored by a real unobserved control failure rather than by the
  collection time of a `WeakReference`.

Each correction strengthened determinism or aligned an oracle with the product's documented
contract. None converted a real product failure into an accepted result.

Verdict: **PASS**.
