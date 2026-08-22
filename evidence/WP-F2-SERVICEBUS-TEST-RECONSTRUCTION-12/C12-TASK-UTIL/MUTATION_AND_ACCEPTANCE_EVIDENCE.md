# C12 TaskUtil mutation and acceptance evidence

Date: 2026-08-23

Technical commit: `cda8930eb1c4dce1b919ece16a8202263e94b7dc`
Technical tree: `5af7f28903f46a72b02068e732eb93d1fd0f9498`

## Scope

This source-owner cohort replaces `AwaitSemantics_Specs.cs` with ordinary xUnit 4 tests executed by
Microsoft Testing Platform 2. It closes all nine inherited ledger obligations owned by that fixture
or the corresponding previously uncovered product boundary. The replacement uses no inherited
TestFramework, fixed-time behavior assertion, polling, random input, skipped case, environment
resource, auxiliary runner, receipt, interceptor, or execution sentinel.

The accepted technical inputs have these SHA-256 hashes:

| Path | SHA-256 |
| --- | --- |
| `src/ViciOne.ServiceBus/Util/TaskUtil.cs` | `a3f7e38eb41bd959b4366b0d7ddc9fe581d4fe626dc025ce5d5b8b70ec74ba03` |
| `src/ViciOne.ServiceBus/Clients/ClientRequestHandle.cs` | `839a478769c78156311e2ba1e49e7775f4ee98581ff097fe3f2c49d0e3cfb786` |
| `tests2/ViciOne.ServiceBus.Tests/Util/TaskUtilTests.cs` | `e7fe02e52a38876677a230eedbbec50460da468d4ee6037a818f4ee55b60b07d` |
| `tests2/ViciOne.ServiceBus.Tests/Requirements/CoreRequirements.json` | `db1d2cda4ad6d022231a00f2925d2010ba99ecaea65c0c1106c912c4c3c990e8` |
| `evidence/native-tests/task-util/INHERITED_BEHAVIOR_DISPOSITION.json` | `ada4fe46ce736b5a46d8313df9da1be0d718e92bae30de1a33de76a6f83f530e` |

## Behavior and product corrections

- Completed and later-completing generic results are preserved. Explicit entry barriers prove that
  the synchronous caller is already inside `Await` before the test observes its blocked state.
- Generic and non-generic faults are rethrown as the exact original exception, never as an
  `AggregateException`.
- Caller cancellation terminates a pending wait with the exact caller token.
- Work that does not need the caller's synchronization context completes without posting to or
  replacing that context. Work that captured the context remains blocked until one explicitly
  observed queued continuation is pumped.
- Cached completed, Boolean, default, faulted, and canceled tasks expose their exact state and value.
  The implementation uses the direct .NET `Task.CompletedTask`, `Task.FromException<T>`, and
  `Task.FromCanceled<T>` factories where they express the contract completely.
- Completion-source creation always combines requested options with
  `RunContinuationsAsynchronously`. Cancellation registration, no-op registration, idempotent
  completion, and every public invalid-input boundary are executable contracts.
- The public spelling is normalized from `Cancelled<T>` to the .NET-standard `Canceled<T>` and its
  sole product caller is migrated atomically. No behavior is removed.
- The product assembly is verified not to reference desktop Windows framework assemblies. The
  synchronous wait remains platform-neutral and never pumps a caller context.
- The broader naming and source-owner normalization of vague inherited utility APIs remains a
  dedicated, explicit task in `TODO.md`; it is not mixed into this behavior cohort.

## Test-quality review

The new file contains 18 ordinary facts, 18 unique requirement rows, and 58 direct assertions.
Every thread exception is transferred through a task observed by the test. Every blocking worker
receives the test cancellation token, and every release or pump has cleanup in a `finally` block.
The central typed `OperationTimeout` is used only as a safety boundary for test completion; elapsed
time is never a behavior oracle. Static review found no delay, sleep, polling, random input,
wall-clock dependency, synchronous `Task.Wait`/`Result`, thread join, skip, assertion-free case,
shared mutable fixture, hidden external resource, or second configuration source.

## False-green attacks

Each accepted mutation changed one behavior, built successfully, failed the responsible native
test, and was then removed before the next mutation.

| Mutation | Observed verdict |
| --- | --- |
| Return the default generic value before the awaited task completes | 4 failed, 14 passed; completed-result and all three explicit blocking/context contracts rejected it |
| Read the generic task through `Result` and leak `AggregateException` | the first version survived and exposed an uncovered overload; after adding the generic assertion, 1 failed and 17 passed for the exact reason |
| Substitute a different canceled token for the caller token | 1 failed, 17 passed; exact token identity rejected it |
| Return a faulted `OperationCanceledException` task instead of a canceled task | 1 failed, 17 passed; canceled task state rejected it |
| Remove `RunContinuationsAsynchronously` from the generic completion-source factory | 1 failed, 17 passed; exact creation options rejected it |
| Complete the cancellation signal as canceled instead of successful | 1 failed, 17 passed; registration signal semantics rejected it |
| Replace the caller's synchronization context with null | 1 failed, 17 passed; context identity rejected it |
| Remove the completed-result requirement attribute | 1 failed, 215 passed; compiled requirement projection rejected it |

No surviving mutation is reported as killed. The initially surviving AggregateException mutation
was treated as a test defect, repaired, and rerun rather than explained away.

## Final acceptance

| Gate | Result |
| --- | --- |
| Locked UnitArchitecture restore, no cache, audit enabled | exit 0 |
| Bounded `dotnet format --verify-no-changes` for all changed C# files | exit 0 |
| Focused `TaskUtilTests` run | 18 total; 18 passed; 0 failed; 0 skipped |
| Complete core-owner run | 216 total; 216 passed; 0 failed; 0 skipped |
| UnitArchitecture Release build | exit 0; 0 warnings; 0 errors |
| Unfiltered UnitArchitecture run | 724 total; 724 passed; 0 failed; 0 skipped |
| Engineering Release build | exit 0; 0 warnings; 0 errors |
| JSON parsing, diff whitespace, forbidden timing/random/skip scan | pass |

An initial restore attempt could not retrieve NuGet's vulnerability index and emitted `NU1900`.
The audit was not disabled or suppressed. Direct checks confirmed both the official service index
and vulnerability endpoint, then a forced no-cache locked restore completed without warnings. The
final UnitArchitecture and Engineering builds both completed with zero warnings and zero errors.

The inherited file is removed only in this accepted state, after all nine owned ledger obligations
have terminal dispositions. Git history remains the archive of its original bytes.
