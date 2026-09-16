# Iteration 148 — Event Hubs Testing read admission

## Scope and result

This bounded packet personally rereads the sole current C# file and every comment in
`ViciOne.ServiceBus.EventHubs.Testing`, its project metadata, the directly owning test and the bound
local-integration requirement projection. The owner has 1 C# file / 22 lines; its test owner has
1 C# file / 123 lines.

No current correctness, lifetime, cancellation, naming, namespace, placement, dependency,
comment or public-API defect was reproduced. The extension validates required inputs before scope
access, resolves the producer provider from the running harness scope, and forwards the exact Event
Hub name and cancellation token. The admission changes no product or test code.

| Admission | Files / lines | Manifest SHA-256 | Chain SHA-256 |
| --- | ---: | --- | --- |
| Source | 1 / 22 | `ab51f3510b1340fea4b0d7e9f8673c933d2881a7b900599bed89f9f65ff56f4c` | `860263f5d456c2d4ae9f60522175735a19d8ea23563ba8b2452ee962dbb863d9` |
| Owning test | 1 / 123 | `a476a507bb6015b258d2b8c3766e51b796ebabfcd8a9dbbb8d21b168c2b8296c` | `9476b121136659b1a331b7dc42367f0cded753be3dc66e6cc8a0f231d05cc26a` |

The chains extend Iteration-147 chain hashes
`75af15709721752e8ffa493cb96dbf80cee0ff59b4f767b83a41b905e133b8a6` and
`ccd9e9ccce6e37fdfe7b5c3cda5d6a71921138f2efc58e77445d7b61842b292e`.
Both manifests validate against the final tree. Cumulative current personal source admission is
180 of 4,116 current C# files.

## Assertion, pairing and coverage review

The three owner tests assert null harness rejection, null/empty/whitespace entity-name rejection,
absence of unintended proxy calls, exact producer identity, exact `topic:orders` address
projection, harness-scope provider resolution and cancellation-token identity. Personal review
found no assertion-free, tautological, unawaited, skipped, wall-clock-dependent or swallowed-
exception case.

The mandatory Roslyn pairing analyzer ran once against isolated directory
`/private/tmp/vsb-iteration148-pairing.F9xIUt`; protected and unrelated trees were not input. It
classified exactly one source and one test file and paired the source to that test.

Fresh instrumentation covers 3/3 executable lines. The package contains no instrumented branch,
one method, complexity 1 and CRAP 1; no coverage or risk gap exists.

## Terminal validation

| Gate | Result | Duration |
| --- | --- | ---: |
| Unchanged focused baseline | 3 passed; 0 failed/skipped/other | 0.426 s |
| Focused final coverage | 3 passed; 0 failed/skipped/other | 0.704 s |
| Strict product Release build | 0 warnings / 0 errors | 4.29 s |
| Strict owner-test Release build | 0 warnings / 0 errors | 5.07 s |
| Product format verification (`warn`) | Exit 0, no differences | — |
| Owner-test format verification (`warn`) | Exit 0, no differences | — |
| Unchanged-product native Core gate | 4,799 passed; 0 failed/skipped/other | 29.289 s |

The parallel exploratory build passed but produced shared-artifact copy-retry warnings and was
rejected as strict evidence. Both builds were rerun serially and passed with zero warnings and zero
errors. The sandboxed coverage attempt was rejected because Microsoft Testing Platform could not
bind its local named pipe; the identical command passed outside that IPC restriction.

Final owner sorted-name SHA-256 is
`8fd5088676cad8753ebe317a1f518cd470e520738a574a95a8c4ddae3ce5ddc6`; coverage CTRF and Cobertura
SHA-256 values are `6e93430d53d5ff339b3180109431a51869e5eebb8a0e1f841e9ef21a517eb151`
and `c6c0f32a65ab9a202157d03c5592c8c90d993ff37d5d7a4e5ccb5a4f1b302898`.
The immediately preceding Core artifact remains
`ae3ddaba5eb0dc76b335d5d36140342b7bf15d3c47deeead12ccdfab0e86e10c`, with identical product and
test sources throughout this read-only admission.

## Checkpoint and continuing goal

Raw artifacts remain under `/private/tmp/vsb-iteration148-*`. Protected review, TestResults and
legacy trees were not modified or used as evidence.

The intended annotated tag is
`servicebus-a-plus-iteration-148-eventhubs-testing-read-admission-2026-09-16`. External publication
is not pre-claimed and remains subject to destination- and payload-specific authorization.

Whole-fork personal source/comment completion, global API/new-parameter behavior coverage, global
bidirectional naming/type/file/directory consistency, legacy/dummy/directive absence, whole-fork
coverage/CRAP and real external-provider acceptance remain open. This packet proves only the
current Event Hubs Testing owner admission and its fresh local evidence.
