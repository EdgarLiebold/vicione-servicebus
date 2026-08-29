# AWS native closure — review correction 02

This evidence binds the independent-review correction at Technical Commit
`1d6016f78b2f903c691d12f6e8d4efa6929ba227`, Tree
`62a2bbb154b0c3316fcfc848baf9d53f144b7844`, Parent
`a6c97049ca4d91bba834170fc202c4d437a187a6`. Architecture commits `c7a8b28`
and `ad85105` authorize the five product boundaries and the benchmark snapshot consumer.

## Closed boundaries

- DynamoDB registrations own an immutable context factory per saga type. Both registration orders
  with two distinct saga/context identities execute in the focused carrier.
- The public low-level SQS host boundary accepts only the sealed snapshot produced by the typed
  builder. URI user info, query and fragment controls are rejected centrally, and retained builder
  mutation fails after the first snapshot.
- SQS/SNS client-pair construction cleans a partial SQS client, preserves a sole primary failure and
  its factory stack, and aggregates dual failures in primary-then-cleanup order. Fully built
  connection disposal retains the original sole client stack as well.
- Queue and topic caches receive one immutable per-host cache option snapshot. Capacity and maximum
  age are validated, the old process-global mutable defaults and ineffective `MinAge` surface are
  gone, and deterministic expiry uses the injected `TimeProvider`.
- A failed or caller-cancelled `Complete` preserves the exact provider exception and marks the
  receive lock lost after settlement has begun.

## Positive execution

Locked restores preceded the Release builds. Unit, LocalIntegration and Engineering builds all
completed with zero warnings and zero errors; their restore and build binlogs are bound here. The
UnitArchitecture profile produced 17 CTRFs and 2,055/2,055 passing tests. The LocalIntegration
profile produced six CTRFs and 149/149 passing tests. Its runner identity is
`vicione-0de4f67f232f`; PostgreSQL, Azurite and LocalStack were exposed only through dynamically
allocated loopback ports, `fixture-findings.json` is empty, and broker logs plus endpoint projection
are bound under `positive/local-fixture/`. No real AWS resource, credential or GitHub Actions run was
used.

The first fresh-worktree Unit attempt before this freeze exposed missing generated MTP props for six
LocalIntegration projects that had not yet been restored in that worktree. Restoring both locked
solution graphs generated the required SDK files; the unchanged architecture carrier and full
profile then passed. This is retained as build-state diagnosis, not represented as a product or test
fix.

A later diagnostic repeat of the full LocalIntegration profile observed the inherited EF-only
`InboxOutboxConcurrencyTests.ConcurrentRedeliveries_EnterTheConsumerOnceAndCommitOneEffectSet` once
at counter 2 rather than 3; all AWS modules remained green. The same unchanged EF carrier passed
immediately in isolation against a fresh PostgreSQL fixture. That non-AWS pre-existing timing
candidate is recorded in `.testagent/status.md` for a separately authorized slice and is not counted
as an AWS acceptance result. The bound acceptance run above is the complete 149/149 run.

## Mutation closure

`MUTATION_MANIFEST.json` binds M24-M34 by exact target path, baseline SHA-256, complete literal
replacement, occurrence count/index, mutant SHA-256, build project and binlog, owning MTP method,
minimum count, CTRF result and post-restore SHA-256. All eleven mutants build; M28 intentionally
emits CA2200 because its sole purpose is the direct-rethrow counterexample, while the other ten
mutant builds are warning-free. All eleven owning MTP invocations exit 2 for their stated cause:
19 causal cases fail, seven control axes stay green, and no case is skipped. The disposable mutation
worktree ends byte-clean at the Technical Commit.

## Tooling and evidence integrity

The CI-tool selftests pass 257/257 outside the macOS sandbox because their process-tree contract must
execute `ps`; the identical sandbox attempt failed only with `EPERM` at that boundary. Identity
selftests pass 103/103, the verification model exits zero, and the generated CHANGELIST check passes.
`SHA256SUMS` binds every other file in this directory and no file outside Evidence, append-only
status and generated CHANGELIST belongs to the Evidence Child.
