# Message initializer lifetime contract remediation — iteration 137

## Outcome and scope

The message-initializer coordinator now observes every started callback to its original completion, captures synchronous invocation failures without losing siblings, and preserves ordinary failure over cancellation. Required arguments are validated synchronously before factory effects. Additional inputs are shallow-snapshotted, applied in order and completed before the primary input. The initialized general send adapter rejects a null provider task without wrapping valid provider tasks.

The final fresh Release compiler completed with zero warnings and errors. Native Core execution completed with 4,545 passed, zero failed, pending, skipped or other cases; duration 26.872 seconds. All 4,457 parent case names and multiplicities are retained exactly; the additional multiset is exactly the 88 focused cases from 14 handwritten methods. Ten independently compiled causal mutations were killed, with exact restoration of all five candidate source/interface/test/ledger hashes after each attempt. The final clean compiler and native coverage run occurred after all restorations.

This is a bounded coordinator remediation, not a claim that the entire fork, every public API, all producer implementations or external release gates have reached A+. The original whole-repository goal remains active. The internal Sol counterreview is an authorized static Lead review, not independent external product-team acceptance.

## Authority and personally understood code

Parent commit: `6627e9f4e753bdc7db4a027f7bc9773e12d2f633`; branch: `feature/servicebus-a-plus-api`. The parent was already normally committed, annotated-tagged, atomically pushed and independently checked against the exact branch, tag object and peeled commit before this source work. The unchanged development-slice SHA256 is `5a9cd605540cd826a756e24ca3f813cb3ce9a1eaf1fc4f881b6cecfbee9c7199`.

The Lead personally read and understood the complete owning source, public interface and both fixtures, including every manually amended callback, assertion and functional comment. The continuing connected personally read source set is 53 files and 5,905 current lines, adding 15 files to the prior connected set; it is not a fresh-reading claim for the unchanged prior files. Its sorted `path TAB current_line_count TAB content_sha256 LF` manifest SHA256 is `933a4512da216829e8a3021659de081ca161aedcfbc4a048e2dd1e2c7a37abea`. The complete owning-test/support admission set now contains 627 personally read inputs, including the new fixture; sorted `path TAB content_sha256 LF` manifest SHA256 is `76ee07e8e85992fbda8b36b2d119bc7f9eebacecf9774cd3e00fcd6b0be862c1`. Rehashing is admission verification, not new reading credit.

No source, test, comment or disposition authoring generator was used. Productive changes and English comments were handwritten after understanding the actual implementation. Read-only inventory, JSON and XML tools only verified paths, hashes, bindings, cases and measured counters. No review, TestResults or legacy tree was accessed or modified. There was no dependency, TFM, SDK pin, build-policy, public signature, compatibility alias, suppression, directive or product-folder migration in this package.

## Findings, causes and disposition

| Boundary | Source-backed cause and correction |
| --- | --- |
| Started property and header batches | Caller-cancelable outer waits could complete the coordinator while returned callback tasks still modified the message or later failed. Coordinator-owned tasks and `Task.WhenAll` batches are now directly awaited; caller tokens are still forwarded for cooperative provider cancellation. |
| Synchronous callback failures | Lazy callback enumeration previously escaped at a synchronous throw, losing observation of already-started siblings. Private asynchronous callback helpers capture invocation and returned-task outcomes, allowing every snapshot callback to be invoked and observed. Null callback tasks retain exact property/header diagnostics. |
| Cancellation masking ordinary failure | An intermediate try/catch implementation converted synchronously thrown `OperationCanceledException` into a faulted callback task. When it preceded an ordinary sibling fault, the outer asynchronous operation became canceled and concealed the ordinary fault. Genuine asynchronous helpers classify callback cancellation consistently, so `Task.WhenAll` ordinary-fault precedence is retained. Original exception objects and provider tokens remain observable. |
| Required inputs and factory effects | Two preparation forms created a context before validating input or valid pre-cancellation. The multi-input form deferred its argument guards inside an async body. Non-async entry wrappers now validate context, compatible input and required array/pipe synchronously, then check cancellation before creating contexts. |
| Additional inputs | Existing shallow array snapshots, null-slot handling and ordered application are preserved and newly exercised against a pending real input. The correction directly awaits accepted applications and retains cancellation checks between separate inputs. |
| General initialized configuration | A provider violating its non-null task contract could return null without a stage-specific diagnostic. The general adapter now throws `The initialized general send pipe returned no configuration task.` and preserves exact non-null provider-task identity. |
| Existing cancellation assertions | Two existing tests asserted immediate coordinator cancellation while its own callback remained unsettled. These obsolete abandonment assertions were replaced, not deleted: the token is forwarded, the coordinator stays pending, the provider cooperatively settles its original task, then the coordinator reports cancellation. Caller-owned input-task cancellation remains prompt. |
| Test oracle independence | Caller, inherited context, send-context and provider tokens are independently constructed where distinction matters. General original tasks are independently observed even after a failed identity assertion. Exact returned initialization-context identity is checked. Void guard-only tests have no misleading Async suffix or dummy completed-task await. |

An accepted successful header batch still forwards its downstream configuration even if caller cancellation arrived during that batch; no new cancellation gate was inserted between those accepted stages. Header failure or provider cancellation prevents downstream execution. The prior typed-inner original-task evidence is retained by the complete parent case multiset, not replaced by this fixture's immediate downstream double.

## Structured diagnostic chronology

Raw artifact root: `/private/tmp/vsb-iteration137-initializer-lifetime.RnaYlb`.

1. The first test compiler found five CS0619 and five xUnit2014 errors: lambda inference selected task-returning synchronous exception assertions. Five lambdas were made explicit void Actions; two guard-only methods became void/non-Async instead of adding meaningless awaits. Compiler duration 55.86 seconds, ten errors, zero warnings; native tests were not run after that failed build. Binlog: `baseline-build-20260916-014520--82167--TpDfbV.binlog`.
2. A fresh corrected baseline compiler completed with zero warnings/errors in 26.96 seconds against unchanged parent coordinator source `a120e845df0d1e3f18fe18e4e4d99396e40b318f4cd26a1468ef1d9d9452e821`. Native execution returned exit 2: 68 cases, 31 passed and 37 failed, zero skips, duration 1.648 seconds. The failures identify owned property/header cancellation, synchronous invocation, input/factory ordering, multi-input synchronous guards and the missing general null-task diagnostic. Binlog: `baseline-v2-build-20260916-014730--82288--XFqruv.binlog`; CTRF SHA256 `9256e1fb1db4b31844e9723750a40b91d2cb980b3ecd6d376a539a44c727eb53`.
3. The first candidate freshly compiled with zero warnings/errors and passed all 68 cases, exit 0; its native CTRF SHA256 is `c19551c18460d5984d50b0c2ed35dbe31e5916e6ff7a0bc2081692abecf0d5a1`. That narrow success was not treated as closure.
4. The Lead added 16 mixed cancellation/fault/held-sibling combinations before further productive changes. The intermediate source SHA256 was `1bb6768caec4cf335cee896ec1d7eb31bcbad19b7b41708dcd2c394d0f322bc7`; expanded fixture SHA256 was `28a1dc895af93bf62333d8d13249053e28bb75733465b6e903f88bceb62283aa`. Fresh compiler: zero warnings/errors, 30.50 seconds. Native exit 2: 84 cases, 80 passed and four failed, zero skips, 1.610 seconds. All four failures require synchronous cancellation first, an ordinary sibling fault and either property or header stage; both ordinary failure origins fail. Binlog: `mixed-reproduction-build-20260916-015707--83033--oJw9Cn.binlog`; CTRF SHA256 `fd55056fce0087f43dd954d7e81d67f1e418700feb3feed827edce283e130db9`.
5. Genuine asynchronous callback helpers fixed that concrete defect. Four real input-provider/owned-sibling interaction cases were also added. Fresh candidate compiler: zero warnings/errors, 74.26 seconds. Native exit 0: 88 passed, zero skips, 1.811 seconds; CTRF SHA256 `3a017e436940b2a212584369628865ddc7fdf549ac83067fd63fab6d6e5c9427`. Binlog: `candidate-v2-build-20260916-015907--83225--qOddaB.binlog`.
6. Candidate unfiltered native coverage passed all 4,545 cases, exit 0, 22.104 seconds. After all ten compiled causal mutations and exact restorations, the final fresh compiler completed in 14.11 seconds with zero warnings/errors. Final native coverage passed all 4,545 cases, exit 0, 26.872 seconds. Final binlog: `final-build-20260916-021342--84722--Fk2W72.binlog`.

The known SDK-IPC sandbox boundary was handled through the explicitly approved elevated execution path, with task-specific TMPDIR, disabled build servers, one MSBuild worker, disabled shared compilation, no restore and unique binlogs for every compiler. No stale DLL was executed after a failed compiler. Two read-only analysis errors were diagnosed separately and corrected after terminal confirmation: system Ruby 2.6 lacks Array#tally, so multiset counting uses group_by; REXML::Attributes#fetch yields Attribute objects, so XML counters use string accessors. Neither required a source/test/environment/package change or a native rerun. A staged/working comparison initially treated UTF-8 and binary Ruby strings as different; exact binary String#b comparisons and SHA256 then proved all six staged/working byte sequences identical. No Git data repair was needed.

## Handwritten behavioral tests and exact requirements

Fixture: `tests/ViciOne.ServiceBus.Tests/Initializers/MessageInitializerLifetimeTests.cs`; fully qualified type: `ViciOne.ServiceBus.Tests.Initializers.MessageInitializerLifetimeTests`.

| Exact method | Cases | Behavioral distinction |
| --- | ---: | --- |
| `PropertyBatch_OwnsEveryStartedCallbackAndOriginalOutcomeAsync` | 15 | Five entry forms, caller cancellation while held, original success/fault/provider cancellation, exact callback token/input/message and no dispatch. |
| `HeaderBatch_OwnsStartedCallbacksAndOriginalOutcomeAsync` | 3 | Held header original outcomes, independent send token, exact context and accepted downstream success-only chain. |
| `ParallelCallbacks_ObserveHeldSiblingsAcrossEveryFailureFormAsync` | 16 | Property/header stages, both failure positions, synchronous exception, faulted task, null task and synchronous cancellation; every callback and held sibling is observed. |
| `ParallelCallbacks_OrdinaryFailureDominatesSynchronousOrTaskCancellationAsync` | 16 | Property/header stages, synchronous/task cancellation, synchronous/task ordinary fault, both invocation orders and a third held callback; original ordinary exception must win and downstream must not run. |
| `Initialization_PreCancellationHasNoFactoryOrCallbackEffectsAsync` | 5 | Valid precancellation in every entry form, zero factory/callback/downstream effects and exact caller token. |
| `Initialization_InvalidInputsAreSynchronousBeforeFactoryAndCancellation` | 10 | Every form, canceled/uncanceled caller, null and incompatible input, exact parameter/type diagnostic and zero factory effects. |
| `MultiInput_ValidatesArrayAndContextSynchronouslyWithoutFactory` | 2 | Exact synchronous required context/array guards before factory and callback effects, canceled/uncanceled caller. |
| `AdditionalInputs_AreSnapshottedOrderedAndPrecedePrimaryAsync` | 1 | Real cache/task-input initialization, caller array mutation, ignored null slots, ordered additional values, primary precedence and original message identity. |
| `AdditionalInputFailure_StopsBeforePrimaryWithOriginalOutcomeAsync` | 2 | Real additional input fault/provider cancellation, original exception/token, no later input or primary application. |
| `CallerOwnedInput_CancellationRemainsPromptAndLeavesValueUnsettledAsync` | 3 | Real task input through cache, initialized-pipe and multi-input paths; local wait cancellation leaves caller input unsettled. |
| `CallerOwnedInput_CancelsLocallyWhileBatchOwnsHeldSiblingAndOriginalFailureAsync` | 4 | Actual InputPropertyProvider → AsyncPropertyProvider → ProviderPropertyInitializer chain with a held owned sibling, both orders, owned success/fault; local input cancellation is prompt but batch termination waits for the owned sibling and preserves its ordinary fault. |
| `GenericInitializedPipe_RejectsNullTaskAndPreservesOriginalTaskAsync` | 3 | Exact null diagnostic versus original fault/canceled task identity, independent explicit/send/provider tokens, original-task cleanup. |
| `GenericInitializedPipe_OwnsHeldConfigurationAcrossOriginalOutcomesAsync` | 3 | Exact original held general provider task/context/token, caller cancellation cannot abandon it, original outcomes retained. |
| `Initialization_ReturnsCorrectMessageAndContextAcrossFormsAsync` | 5 | Five forms, actual populated message, exact returned context where exposed, input/token/factory effects and no implicit dispatch. |

Total: 14 methods, 88 cases. Controlled gates are released in finally blocks and gate/provider/callback/root tasks are independently observed. Bounded cleanup does not substitute for behavioral assertions. No case was skipped or weakened to obtain a green result. The 21 existing contract methods remain; the two changed cancellation tests now distinguish accepted owned work from caller-owned input values.

`CoreRequirements.json` preserves all 2,976 parent rows as an exact semantic prefix and has exactly 14 unique additive requirement/variant/assembly/type/method bindings, matching every new attribute: 2,990 rows. Ledger SHA256: `b4593227840c6f77e5ffa7093a14f5c10f66587795564ca5f85793f99ef59938`.

## Compiled single-cause mutation witnesses

Each mutation uses a complete handwritten literal patch against the same owning source, a fresh successful zero-warning/error compiler, the unchanged 88-case focused fixture and a distinct native CTRF. Native exit 2 is required; compile failures do not count as killed mutants. Before the next mutation all five candidate hashes are restored and compared exactly. Across ten attempts, 880 cases executed: 828 passed, 52 intentionally failed, none skipped or pending. This is ten selected causal witnesses, not a whole-fork mutation score or proof of every possible defect.

| ID | Single cause | Passed / failed | Exact failing method witnesses |
| --- | --- | ---: | --- |
| M01 | Restore cancelable outer property-batch waits | 69 / 19 | PropertyBatch (15), CallerOwnedInput mixed held sibling (4). |
| M02 | Restore cancelable outer header-batch wait | 85 / 3 | HeaderBatch (3). |
| M03 | Represent synchronous property cancellation as faulted Task | 86 / 2 | ParallelCallbacks ordinary-failure precedence (2). |
| M04 | Represent synchronous header cancellation as faulted Task | 86 / 2 | ParallelCallbacks ordinary-failure precedence (2). |
| M05 | Forward inherited context token instead of explicit property caller token | 75 / 13 | PropertyBatch (9), Initialization return/context (3), CallerOwnedInput prompt cancellation (1). |
| M06 | Forward initializer-context token instead of send-context header token | 85 / 3 | HeaderBatch (3). |
| M07 | Forward send-context token instead of explicit general caller token | 82 / 6 | Both GenericInitializedPipe methods (3 each). |
| M08 | Remove general null-task diagnostic | 87 / 1 | GenericInitializedPipe null-task/identity (1). |
| M09 | Keep the caller array instead of a snapshot | 87 / 1 | AdditionalInputs snapshot/order/precedence (1). |
| M10 | Allow factory creation before valid precancellation | 86 / 2 | Initialization precancellation/factory effects (2). |

Exact witness method names appear in the preceding table. Raw directories have descriptive IDs `M01-property-batch-abandonment` through `M10-pre-canceled-factory-effects`. Each ID has `<ID>-build.log`, a uniquely expanded `<ID>-build-{}.binlog`, `<ID>-native.log` and `<ID>/<ID>.ctrf.json` under the raw root. The mutations never alter the fixture or requirement bindings.

## Final measured coverage — nine modules only

Collector settings SHA256: `c049632b61e7fcf5be95962df1629c620a9f66e77366dae0e7ddf7c91f0d4a13`, byte-identical to the parent settings. Test assemblies are excluded; auto-properties and attributed/generated code are not excluded by convenience. Dynamic managed instrumentation is enabled. No threshold was weakened.

Final Cobertura: 50,660 / 64,605 lines = 78.41498336042102%; 17,523 / 24,348 branches = 71.96895022178413%. Its package counters must sum exactly to these root counters. This excludes unselected transport, persistence and scheduling assemblies. CRAP risks, complete product-tree coverage and all-API completeness are not calculated or certified by this scoped run.

| Module | Lines covered / valid | Branches covered / valid |
| --- | ---: | ---: |
| ViciOne.ServiceBus.Abstractions | 4,939 / 8,310 | 1,611 / 2,996 |
| ViciOne.ServiceBus.Sagas | 4,828 / 7,512 | 1,488 / 2,571 |
| ViciOne.ServiceBus.JobService | 4,353 / 4,552 | 2,067 / 2,303 |
| ViciOne.ServiceBus.Courier | 2,526 / 2,835 | 682 / 904 |
| ViciOne.ServiceBus | 28,546 / 35,518 | 10,118 / 13,629 |
| ViciOne.ServiceBus.Futures | 1,356 / 1,495 | 424 / 496 |
| ViciOne.ServiceBus.Testing | 3,176 / 3,365 | 882 / 1,131 |
| ViciOne.ServiceBus.Initializers | 100 / 100 | 4 / 4 |
| ViciOne.ServiceBus.Mediator | 836 / 918 | 247 / 314 |

The capability assembly's 100 instrumented lines do not represent all initializer implementation: the owning coordinator and the adjacent producer implementations reside in Core and contribute to its coverage counters. Do not infer whole-initializer or whole-fork completeness from that small assembly's percentage.

## Internal counterreview and multidimensional admission

The authorized internal Sol reviewer personally read all 330 owning-source lines, 61 interface lines, 807 lifetime-fixture lines and 722 existing contract-fixture lines, covering 35 test methods. All four content hashes were checked before and after those stable reads; ledger prefix and 14 bindings were independently reconciled. The Lead did not start mutations until the reviewer acknowledged completed stable reads. Remaining concrete bounded findings: Critical 0, High 0, Medium 0, Low 0. The reviewer executed no runtime tests or mutations and made no candidate or Git changes. Its handwritten 60-line report is `internal-counterreview-candidate.md` under the raw root; SHA256 `0a7a9a683c4a10ec03303feed9f02422b8cad23bffedc52ba6b5b0eda9c478ea`. The Lead personally read the complete report.

A separate internal artifact fact audit fully read the original 158-line report and reconciled the historical/final native results, compiler summaries, mutation witnesses, coverage counters, hashes and publication boundaries. It found one Low documentary chronology error: array snapshotting was already present in the parent. The Additional inputs row above now distinguishes that preserved behavior from the directly owned application-await correction. The Lead separately refined the external-acceptance scope to the bound RabbitMQ/InMemory durable-provider circle. The handwritten 21-line audit is `internal-evidence-fact-audit.md` under the raw root; SHA256 `0877eeb17400435c03d7f39137840bcc695c0198da33716741e1891ba8df4942`. The Lead personally read the complete audit; it is not runtime or external acceptance.

| Review axis | Bounded evidence and remaining scope |
| --- | --- |
| Correctness, outcome ownership, cancellation | Exact callback/task/context/token and held original-outcome assertions, mixed ordinary-fault precedence and compiled M01–M07 witnesses. Adjacent producers remain open. |
| API guards and startup effects | Five forms, synchronous typed argument guards and no valid precancellation factory effects; causal baseline and M10. No broad startup-options closure claimed. |
| Inputs and losslessness | Real cache/application ordering, shallow snapshots, null slots, primary precedence, original failure and caller-owned input cancellation; M09 and all parent cases retained. Other conversion/input shapes are not certified. |
| Async naming and comments | Every method in the two changed source files and two fixtures was read bidirectionally; Task-returning contracts use Async and synchronous Create/guard methods do not. Functional English comments were manually understood and amended. This is not the whole-fork async/comment audit. |
| Types, files and architecture | Related static convention facade/internal generic coordinator retain their shared MessageInitializer stem; interface, capability test ownership and Initializers test folder are coherent. No optical folder migration or compatibility bridge was introduced. Whole-tree type/file/API-layer closure remains open. |
| Format, directives and dummy code | Diff whitespace checks and warning-as-error compiler are clean. No productive dummy, convenience suppression or directive was introduced; no all-fork absence claim follows from this scope. |
| Test quality and mutation efficacy | Independent tokens, exact identity and status, real input-provider interaction, finally cleanup, stable 88-case multisets, ten compiled killed witnesses and clean post-restoration full Core execution. |
| Dependencies and external acceptance | No dependency/TFM/pinning change. RabbitMQ/InMemory durable-provider acceptance and broader external gates are not executed by this scoped run. Complete product build/pack/public-API journeys and full-fork coverage/CRAP are not certified by the coordinator evidence. |

## Frozen candidate and final artifact binding

| Path | SHA256 |
| --- | --- |
| src/ViciOne.ServiceBus/Initializers/MessageInitializer.cs | `ee198c5967d635908143d66ebc0e79230d8f672007b2ef40a2eb63b5fde622e3` |
| src/ViciOne.ServiceBus.Abstractions/Initializers/IMessageInitializer.cs | `acbb8ac86924f3db7732dd621a35cb005c0eeeb1b5ae8296fe19d059b916a315` |
| tests/ViciOne.ServiceBus.Tests/Initializers/MessageInitializerLifetimeTests.cs | `3e0b78d7b9db228e45d3b78d2fa8ba89b19abc6dfd6eeb479c47b8d23646409b` |
| tests/ViciOne.ServiceBus.Tests/Initializers/MessageInitializerContractTests.cs | `c7dafd416b00cdf348284891832c24c0036e60913d52645ffbf812040062025f` |
| tests/ViciOne.ServiceBus.Tests/Requirements/CoreRequirements.json | `b4593227840c6f77e5ffa7093a14f5c10f66587795564ca5f85793f99ef59938` |
| Final native CTRF | `931ebb6c71eb90b42857de6020236a806bc77b093647998331758c4179b6946b` |
| Final Cobertura | `a0004d4b425cb270c2242986fef5619096050844ab404555aeee944a4765383e` |
| Final 4,545-case name/count multiset | `f1752bedd349636075db99fd6ba6b42f7b18b7ad0d3f8e4f7ad91521eb0db2ea` |

The complete final artifact paths are `final-core/final-core.ctrf.json` and `final-core/final-core.cobertura.xml` under the raw root. Raw artifacts are local diagnostic evidence, not asserted to have been uploaded. The source/interface/tests/ledger and this report are the exact six-file normal Git checkpoint scope.

## Reproduction and publication boundary

Every compiler uses the same native owning-test project; no VSTest filter or separator is used. Actual SDK: stable .NET 10.0.302. Compiler and final native commands:

```sh
TMPDIR='/private/tmp/vsb-iteration137-initializer-lifetime.RnaYlb' dotnet build tests/ViciOne.ServiceBus.Tests/ViciOne.ServiceBus.Tests.csproj -c Release --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false -warnaserror '/bl:/private/tmp/vsb-iteration137-initializer-lifetime.RnaYlb/final-build-{}.binlog'
TMPDIR='/private/tmp/vsb-iteration137-initializer-lifetime.RnaYlb' dotnet exec artifacts/sdk/bin/ViciOne.ServiceBus.Tests.Unit/release/ViciOne.ServiceBus.Tests.dll --results-directory '/private/tmp/vsb-iteration137-initializer-lifetime.RnaYlb/final-core' --minimum-expected-tests 4545 --zero-tests-policy strict --fail-skips on --fail-warns on --progress off --timeout 5m --report-xunit-ctrf --report-xunit-ctrf-filename final-core.ctrf.json --coverage --coverage-output-format cobertura --coverage-output '/private/tmp/vsb-iteration137-initializer-lifetime.RnaYlb/final-core/final-core.cobertura.xml' --coverage-settings '/private/tmp/vsb-iteration137-initializer-lifetime.RnaYlb/core-productive-coverage.config'
```

Focused native execution uses `--filter-class ViciOne.ServiceBus.Tests.Initializers.MessageInitializerLifetimeTests`, minimum 88 and the same strict skip/warn/zero-test policies after a fresh successful compiler. Baseline/minimum 68 and mixed-reproduction/minimum 84 are distinct historical source/test states, not substitutes for the final 88-case fixture.

Publication is a normal scoped commit and a new annotated tag, followed by the approved normal atomic branch-plus-tag push and an independent exact three-ref check: branch commit, annotated tag object and peeled tag commit. This report is written before creating its own checkpoint; the actual new commit, trees and report hash are bound by the tag and verified separately, never guessed inside a self-referential report. No force push or existing-tag overwrite is authorized or used.

## Next connected work and whole-goal audit

Next, repair the already personally read provider-owned outer waits in ProviderPropertyInitializer, ProviderHeaderInitializer, SetHeaderInitializer and both AsyncPropertyProvider variants, including converter-owned tasks. Preserve prompt waits on caller-owned task values and exact TaskPropertyProvider exports; Task nesting is not by itself an ownership rule. Add causal held-provider/converter outcomes and mixed owned/input cases before producer changes, then repeat full reads, comments, type/file/API review, compiled mutations, native validation and a normally secured iteration.

The current cache/additional-input tests do not exhaust every owned producer behind additional-input conventions, null inner inputs, all conversions or multiple-ordinary-failure selection policies. Those gaps stay explicitly open. The whole requested A+ source/API/architecture goal, every API/new-parameter behavioral proof, complete manual productive-tree disposition, full-fork bidirectional naming/comment/type/file checks, total line/branch coverage and CRAP risk closure, complete build/pack/journey gates and real external-provider release gates remain subject to evidence-backed iterative completion. This package is progress toward that goal, not a narrower replacement goal or final A+ certificate.
