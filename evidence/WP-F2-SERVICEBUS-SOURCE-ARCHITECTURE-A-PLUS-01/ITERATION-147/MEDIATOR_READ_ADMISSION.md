# Iteration 147 — Mediator read admission

## Scope and result

This bounded packet personally rereads every current C# file and comment in
`ViciOne.ServiceBus.Mediator`, its project metadata, all directly owning tests, the adjacent public
test-harness contract and the bound requirement projection. The owner has 28 C# files / 2,755
lines; the owning test/support packet has 15 C# files / 3,588 lines.

No current correctness, architecture, lifetime, concurrency, cancellation, naming, namespace,
placement, dependency, comment or public-API defect was reproduced. Direct/container construction,
send/publish dispatch, request-response routing, observer isolation, body ownership, message
limits, dependency-injection scope preservation, virtual deadlines and asynchronous cleanup remain
coherent. The admission intentionally changes no product or test code.

| Admission | Files / lines | Manifest SHA-256 | Chain SHA-256 |
| --- | ---: | --- | --- |
| Source | 28 / 2,755 | `c3fcfc6c81711720d77607fbb0dfc8cfe608aaec3fd4205a826ddaef60101e16` | `75af15709721752e8ffa493cb96dbf80cee0ff59b4f767b83a41b905e133b8a6` |
| Owning tests/support | 15 / 3,588 | `7b9ce83e78991f41fa572116a275e26238a06f085316fee0909ce2b7bfd5b7d9` | `ccd9e9ccce6e37fdfe7b5c3cda5d6a71921138f2efc58e77445d7b61842b292e` |

The chains extend Iteration-146 chain hashes
`aebc26e7a6438835af4ec5a8d1c4e282911525be42e89c4aea1951adc3539e73` and
`8a7fdef6790051a5ec4a289a8baad4ab416b71358ff303cac9dd89776e261de4`.
Both manifests validate against the final tree with zero mismatches. Cumulative current personal
source admission is 179 of 4,116 current C# files.

## Assertion and gap review

The 14 direct test files contain positive, negative and boundary assertions for construction,
configuration, every advanced dispatch shape, addressed requests, message admission, exact body
ownership, observer ordering/isolation/fault containment, caller cancellation, virtual deadlines,
context projections, scoped resolution, handler adapters and container integration. The adjacent
harness contract proves observation, exact exception identity and idempotent asynchronous cleanup.
Personal review found no assertion-free, tautological, unawaited, skipped or swallowed-exception
owner test and no uncovered behavioral partition that justified a speculative product change.

The mandatory Roslyn pairing analyzer ran once against isolated directory
`/private/tmp/vsb-iteration147-pairing.FlAhUC`; protected and unrelated trees were not input. It
classified exactly 28 source and 14 direct test files. Twelve product files are name-paired and 16
are statically unpaired. Those 16 are internal implementations reached through public-boundary
tests, DI/reflection paths, instance-invoked extension methods, probe members, or the zero-
declaration global-using file. This result is preserved as emitted rather than relabeled; static
pairing is a parse-only heuristic and runtime coverage is reported separately.

## Terminal validation

| Gate | Result | Duration |
| --- | --- | ---: |
| Unchanged root owner baseline | 74 passed; 0 failed/skipped/other | 2.184 s |
| Complete owner final coverage run | 92 passed; 0 failed/skipped/other | 3.857 s |
| Strict Mediator product Release build | 0 warnings / 0 errors | 30.01 s |
| Strict Unit-owner Release build | 0 warnings / 0 errors | 76.61 s |
| Product format verification (`warn`) | Exit 0, no differences | — |
| Test-owner format verification (`warn`) | Exit 0, no differences | — |
| Native Core final | 4,799 passed; 0 failed/skipped/other | 29.289 s |

The initial sandboxed coverage and both format attempts were rejected as environmental evidence:
Microsoft Testing Platform and Roslyn build hosts could not bind their local named pipes. The
identical coverage and format commands passed outside that IPC restriction. The PowerShell CRAP
helper could not run because `pwsh` is not installed; the documented formula was applied directly
to the same Cobertura method data instead.

Final Mediator sorted-name SHA-256 is
`b3fbcb167132175ae405322a21cf7ec529f58fc33afdfab5fd988841d3eb9ca9`; coverage CTRF and Cobertura
SHA-256 values are `038fbdc8dfdc299acbe6d002f77e4dc8c6544f14b1710baa79bf24caa8913cec`
and `cf6b1e5e60c55c21edb00b0a98014762afc84eec5898a08fbb0e87de387672e5`.
Core retains the exact Iteration-146 4,799-name multiset at
`6f8c418b9a75c807825e4758f8cc5dd92e80b16cb657b27cfe32365c249b09f5`; final Core CTRF SHA-256 is
`ae3ddaba5eb0dc76b335d5d36140342b7bf15d3c47deeead12ccdfab0e86e10c`.

## Coverage and CRAP

Coverage uses repository configuration SHA-256
`3838fc1b6b73f21d8fb447beecad11a5c91cca053bd6c31f906a6036f248464d`.

| Assembly | Lines | Branches | Complexity | Methods | CRAP > 30 | Below 80% |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Mediator | 832/918 (90.6318%) | 242/314 (77.0701%) | 327 | 283 | 0 | 39 |

The highest CRAP score is 24 at 100% line coverage for
`MediatorSendEndpoint.SendMessageAsync<T>`. The highest incompletely covered scores are 8.8338 for
the broadest `ScopedMediator.PublishAsync` delegation and 8.3040 for `InProcessMediator.DisposeAsync`.
The 39 below-threshold members are predominantly zero-complexity or low-complexity accessors,
delegations and defensive arms. No score approaches the risk threshold, so coverage-only product
rewrites or artificial tests are not warranted by this admission.

## Checkpoint and continuing goal

Raw artifacts remain under `/private/tmp/vsb-iteration147-*`. Protected review, TestResults and
legacy trees were not modified or used as evidence.

The intended annotated tag is
`servicebus-a-plus-iteration-147-mediator-read-admission-2026-09-16`. Local commit and tag occur
after exact-path verification. External publication is not pre-claimed and remains subject to
destination- and payload-specific authorization.

Whole-fork personal source/comment completion, global API/new-parameter behavior coverage, global
bidirectional naming/type/file/directory consistency, legacy/dummy/directive absence, whole-fork
coverage/CRAP and real durable-provider/external acceptance remain open. This packet proves only
the current Mediator owner admission and its fresh evidence.
