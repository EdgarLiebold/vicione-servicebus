# Iteration 119 getter, terminal payload and factory-cleanup packet

Status: coherent corrective checkpoint verified; not final iteration119,
external acceptance, release acceptance or overall A+ completion.
The original hash-bound source-architecture order and autonomous goal stay active.
Checkpoint `de97b0608d23b693efad8eed0aef4f1a4c2946e6` and annotated tag
`servicebus-a-plus-iteration-119-cancellation-child-checkpoint-2026-09-15`
were independently verified on origin before these source/test edits.

## Scope and human source review

The author personally reads the complete RetryFilter, RetryPolicyExecution,
RedeliveryRetryExecution, RetryOperationState, both ConsumeContextRetryPolicy
variants, both consume-policy-context variants and BasePipeContext. All BasePipeContext
comments are rewritten manually from the actual implementation, including context-self
precedence, cache initialization, cancellation and payload-factory behavior. No executable
BasePipeContext behavior changes in this packet. The nullable cache behavior of its
two cache-taking constructor overloads is not represented as a fully modernized contract;
one currently rejects null while the cancellation-taking overload initializes lazily.
That foundation consistency review remains open. No comment/source/test generator.

The complete ConsumeContextRetryPolicyTests and RetryFilterTestFactory and all relevant
ownership test/helper additions are personally read. Existing immutable test closure
is preserved, not replaced by superficial line-hit tests or renamed legacy coverage.
Current bounded test scope: Ownership18 methods91 cases, RetryFilter33/76,
ConsumeContextRetryPolicy11/23; total62 methods190 cases.

## Findings corrected

1. Selected-token getter failure discarded by an exception filter. Both single/nested
   cases originally completed falsely. Guarded ShouldPropagate evaluates context/policy
   getters and payload ownership in an ordinary catch-body infrastructure path.
2. Projected terminal-payload lookup failure escaped unmarked. Initial/retry paths
   could consume an outer business budget and complete falsely. The lookup is now
   guarded on the input operation before business classification.
3. Consume-policy acquisition/projection/representation failure followed by cleanup
   failure lost the primary error in both consume adapters. Shared failed-factory
   cleanup preserves AggregateException `[primary, cleanup]` with exact identities.

Remaining exception filters inspect already captured framework CancellationToken
structs, not custom policy/context getters. Requested source/selected cancellation
and foreign-token business classification remain distinct. No new exported product
type, compatibility shim or feature deletion is introduced.

CLR filter exceptions are discarded and make the filter false; this explains the
first causal false-success defect. See the primary
[C# statement specification](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/statements)
and [user-filtered exception handling documentation](https://learn.microsoft.com/en-us/dotnet/standard/exceptions/using-user-filtered-exception-handlers).

## Executed causal evidence

| Run | Result | Distinguishing oracle |
| --- | --- | --- |
| Token getter unchanged product |85pass/2fail/0skip | Both new cases falsely complete |
| Corrected token getter |87pass/0fail/0skip | Same exact infrastructure failure, no third business attempt |
| Token-getter existing suite |76pass/0fail/0skip | Prior retry behavior retained |
| Complete corrected token-getter Core profile |3,796pass/0fail/0skip | Complete Core host, explicit all-source profile |
| Terminal lookup unchanged product |89pass/2fail/0skip | Initial/retry false success; compound positive guards pass |
| Consume factory unchanged product |18pass/5fail/0skip | Cleanup alone escapes instead of ordered aggregate |
| Corrected expanded ownership |91pass/0fail/0skip | Exact infrastructure/compound identities, budgets, cleanup, storage |
| Corrected consume factories |23pass/0fail/0skip | Both adapters, admission/projection/representation and registrations |
| Existing retry after both corrections |76pass/0fail/0skip | Existing lifecycle and business oracles retained |
| Restored complete Core profile |3,805pass/0fail/0skip | All current Core cases, explicit-source coverage |

The two compound terminal-payload cases are positive guards for the already-owned
aggregate; they are not counted as causal red failures. Seven of the nine payload/factory
additions fail causally before correction. A test-oracle error in the retry-path next-decision
count is caught and corrected by human review before its first execution; it is not
included in a causal red report. All accepted red failures are real false-success or
lost-primary product defects, not compile failures or invalid test fixtures.

Raw causal reports and SHA-256:

- `/private/tmp/vsb-iteration119-token-getter-causal-red/token-getter-causal-red.ctrf.json`:
  `e6b02b52d804fbdf4d69d9e7bfa1f70ac977770b7adc8107082d0394295368d6`.
- `/private/tmp/vsb-iteration119-terminal-payload-causal-red/terminal-payload-causal-red.ctrf.json`:
  `74ccb17ce0b879fbbb8f5b3e4fb35ebb30649fcaa677fd31858ed0a86d988e18`.
- `/private/tmp/vsb-iteration119-consume-factory-causal-red/consume-factory-causal-red.ctrf.json`:
  `0cd04672c064669b8d6def0a2917edd6248273eff0f3447897fe26d87124ae5f`.

## Controlled mutation evidence

The unsafe-token-filter counterchange compiles with zero warnings/errors and fails
exactly both new cases;85 others pass, zero skip. Restored RetryFilter SHA-256 is
`a42dc63936081b7258e978d89c32662b8bbc0bc0a6ecc5d16ca1d19ba8275b2c`
at that token-only correction. Raw counterchange report:
`/private/tmp/vsb-iteration119-mutant-unsafe-token-filter/mutant-unsafe-token-filter.ctrf.json`,
SHA-256 `3b11e4ae6eaf30f18f9300f183c519bd4df6859308438bfb79686c3a3945a86f`.
The separate unguarded-terminal counterchange compiles with zero warnings/errors and
fails exactly both non-compound terminal cases;89 others pass, zero skip. RetryFilter
is independently restored to `76c0171adb94c6f6da046f776d5d83e0e405e1ae91b569fc0959f2f2b52531d6`.
Raw `/private/tmp/vsb-iteration119-mutant-unguarded-terminal/mutant-unguarded-terminal.ctrf.json`,
SHA-256 `7821d7f21ef13a95950c447af717262d938f034a23c41b1ea19c3ba5ef3ccd16`.

The separate reversed-factory-identities counterchange compiles with zero warnings/errors
and fails all five exact ordering/identity cases; old18 pass, zero skip. RetryPolicyExecution
is independently restored to `8589a6d9fae74ad067a4a90d3bea451505cdf2abbbe2bc8dcca67538015814ba`.
Raw `/private/tmp/vsb-iteration119-mutant-reversed-factory-identities/mutant-reversed-factory-identities.ctrf.json`,
SHA-256 `1606eaa515b9485d9ae760a673c2e27d352e3bb622a9ee24803402b687ee46b6`.
Thus all three counterchanges are compilable, independently killed and byte-restored,
not compiler-failed mutants counted as test success. No mutant remains in checkpoint source.

## Internal review and remaining scope

The separate internal read-only follow-up fully rereads the three token-corrected
product files and the relevant new test/helper scope. It accepts the exact original
token-getter correction, then establishes the terminal-lookup High without executing
it; author execution subsequently reproduces and corrects it above. Author executions
are never attributed to the reviewer. This is not independent external red-team
acceptance. Its source freeze is explicitly released before subsequent source edits.

A further internal diagnostic reads only immutable checkpoint `de97` via git show,
not changing live inputs, and establishes three additional unexecuted High classes:

- Projected Enter/child BeginPolicy admission failures can escape without ownership.
- Repeated custom GetOrAddPayload inside Mark can replace the primary failure and replay.
- Repeated custom TryGetPayload inside Propagate after cleanup can replace primary
  or compound failure and replay.

Those are a separate next owned-scope packet. A live acquired scope with direct
context/state/caller references can avoid redundant custom callbacks; necessary
admission callbacks must be failure-atomic and associate failure with an active caller.
No caller-crossing AsyncLocal leak or OOM guarantees are claimed. The current
correction does not silently claim these paths closed.

Actual delayed/RabbitMq provider publish/topology cancellation remains open. Local
scheduling/ack test stubs do not prove broker publishing acceptance. Final119
all-host, current API/package/journey and whole-product coverage gates also remain open.
The earlier six-kernel coverage report is bound to de97, not these changed sources.

## Restored-source gates

Strict focused build completes with zero warnings/errors in13.24 seconds. Product
and Unit solution format verifications both complete exit0, no source edits, only the
known workspace-load warning. Git diff whitespace verification passes. The live frozen
internal review accepts the bounded correction without additional findings and releases
its source freeze; its full scope and exact hashes are recorded separately.

Full Core passes3,805/3,805, zero skip,37.887 seconds. Raw CTRF:
`/private/tmp/vsb-iteration119-getter-payload-factory-complete-profile/getter-payload-factory-complete-profile.ctrf.json`,
SHA-256 `efe7ef7c929350d4ccb719a7ee7ec72408af5ce900e9a2691bcb5c34f8736612`.
Raw Cobertura sibling `getter-payload-factory-complete-profile.cobertura.xml`,
SHA-256 `d40e429708a70e460a64d84c7c487d34c9bf69abe1ab684ff928f91d023fa3fa`.
The explicit profile SHA-256 remains
`3838fc1b6b73f21d8fb447beecad11a5c91cca053bd6c31f906a6036f248464d`:
all src code, debugger-attributed operations and auto-properties, excluding test source.
Raw graph49,332/60,934 lines and16,858/23,090 branches; this is the loaded Core graph,
not a whole-product or real-provider union. The owned current coverage summary records
the risk analysis separately. All proof processes and reviewer freezes are terminal/
released before further executable edits; no mutation is left in accepted source.

## Source-layout decision

`src/ViciOne.ServiceBus` owns the Core project, not an umbrella container. All other
direct siblings are independently compiled capability/contract projects; there are no
loose C# files directly below src. Persistence/Scheduling/Transports retain provider
family grouping. Human file-by-file review aligns internal functionality, type and
namespace folders without nesting another SDK project underneath Core. This preserves
optional dependencies and distinct package ownership; no wholesale path rewrite here.
