# C31 validation

Date: 2026-08-25

## Accepted result

`BusControlHealthExtensions` now exposes task-returning APIs with the `Async` suffix, one standard
.NET `TimeProvider` execution path, complete `BusHealthResult` success values, typed timeout
diagnostics, exact caller cancellation and deterministic collection semantics. The EF Core outbox
caller and the three still-retained inherited kill-switch callers compile against the new API.

## Native gates

| Gate | Result |
|---|---|
| Focused health-wait cohort | 22/22 passed, 0 failed, 0 skipped |
| Complete Abstractions project | 235/235 passed, 0 failed, 0 skipped |
| Complete UnitArchitecture profile | 1428/1428 passed, 0 failed, 0 skipped |
| LocalIntegration profile | 3/3 passed, 0 failed, 0 skipped |
| Abstractions Release build | 0 warnings, 0 errors |
| EF Core outbox caller Release build | 0 warnings, 0 errors |
| Retained Core kill-switch caller Release build | 0 warnings, 0 errors |
| ActiveMQ caller Release build | 0 warnings, 0 errors |
| RabbitMQ caller Release build | 0 warnings, 0 errors |
| UnitArchitecture Release build | 0 warnings, 0 errors |
| Complete Engineering Release build | 0 warnings, 0 errors |
| Bounded Roslyn whitespace verification | exit code 0 |
| Independent one-cause mutations | 12/12 rejected |

## Final evidence hashes

| Artifact | SHA-256 |
|---|---|
| `abstractions-tests-granular-validation-final.binlog` | `15c56e6a42f2d10266cfcd62130d45ad2c3ea401bcfd1ede7f6fbbf4e5c5d156` |
| `unit-full-test-confirmation-dotnet-test.binlog` | `42611b4e7976b9b196182c3a6e186914356929e506b0aa83a809ae8479742f23` |
| `local-integration-build.binlog` | `739aca8c4e35b02134431ef893151f18d76a8b46646b007c6252da33685c1d8e` |
| `local-integration-test-dotnet-test.binlog` | `e6de4b20cf33f728e07b5081697ef7f4e734ed74b20a65d22f9b9ef0f6b2cd15` |
| `engineering-build.binlog` | `fbd02cda15a34ee8a05da434cb6bd5a73b6543c3004c6aded39ea1d367e05611` |
| `format-verify-unsandboxed.binlog` | `5a34678d64df65aded17bc288f6b63c1f9af1f477c800ef5dc98da3c28df9fe0` |

## Environmental diagnosis

One intermediate build ended after 5 minutes 2 seconds with no compiler warning or error. Its
binlog localized the wait to `_GetProjectReferenceTargetFrameworkProperties` while five orphaned
MSBuild nodes and one compiler server from interrupted earlier builds were still alive. The official
build-server shutdown could not reach those nodes. After terminating only those exact stale
processes, the identical build completed in 3.63 seconds. Subsequent bounded and full builds and
tests are green. This was an environmental process-lifetime fault, not a product or test defect.

## Static closure

- requirement JSON parses successfully;
- 22 test methods map to 22 exact health-wait variants;
- no C31 mutant token remains;
- no process-clock call or providerless polling delay remains in the owner;
- the old bus-control extension name has no remaining caller; similarly named health-check-service
  helpers are a different product boundary;
- bounded `git diff --check` is clean.

The larger kill-switch and hosted health state machine remains a separate source-owner cohort. Its
inherited fixtures were therefore mechanically updated but not deleted in C31.
