# AWS native closure correction — validation

## Bound subject

This correction package validates technical commit
`d0043bae8dccfee74d8ae223b5e8031409a2784d`, tree
`3380c74e8594c5cca510df15e1c89cea40f068af`, whose direct parent is the remotely
bound AWS evidence state `0ff3e84e966d469a1b77048da6f84b52040ed23a`.
`TECHNICAL_DIFF.txt` is the exact 18-path `git diff-tree --name-status` projection
of the technical commit. This correction evidence does not itself change product,
test, build, fixture or workflow behavior.

The technical correction closes four independently reproduced product boundaries:

- DynamoDB saga registration captures its configured context factory at registration
  time; mutating a retained configurator cannot rewrite the live runtime dependency.
- DynamoDB saga load treats JSON null, a foreign persisted row key, a foreign payload
  correlation identity and a payload/persisted-version mismatch as serialization
  corruption rather than returning or executing the wrong saga.
- Amazon SQS marks an expired maximum visibility-renewal window as a lost receive lock,
  and an owned connection attempts both SNS and SQS disposal while preserving the exact
  sole failure or deterministic SNS-then-SQS aggregate order.
- SQS `SentTimestamp` is now protected by an exact hermetic Unix-millisecond/UTC oracle;
  the LocalStack test no longer uses a wall-clock interval as its oracle.

## Positive execution

- UnitArchitecture: the unfiltered Release solution command passed 2,043/2,043,
  with zero failure, skip, pending or other result. Seventeen separately named CTRFs
  sum independently to the same count. The raw solution transcript is
  `positive/unit-solution.log.gz`.
- LocalIntegration: one fresh canonical fixture owned PostgreSQL, Azurite and LocalStack
  on Docker-selected loopback ports. The unfiltered Release solution passed 149/149,
  and six separately named CTRFs sum independently to 149/149 with no failure or skip.
  The run identity is `vicione-b0b9bcb4e77a`; `fixture-findings.json` is empty and
  binds all three captured broker logs.
- Unit, LocalIntegration and complete Engineering locked Release builds each finished
  with zero warnings and zero errors. Their MSBuild binary logs are bound under
  `positive/`.
- The focused AWS totals are SQS 51 Unit + 48 LocalStack, DynamoDB 12 Unit + 9
  LocalStack and S3 6 Unit + 5 LocalStack.

## Exact mutation closure

`MUTATION_MANIFEST.json` binds M15–M23 by target path, baseline SHA-256, literal
one-occurrence replacement, occurrence index, mutant SHA-256, expanded build/test
command, owning xUnit method, causal result and post-restore SHA-256. All nine
mutants build in Release with zero warnings and zero errors. Their owning xUnit 4 /
Microsoft Testing Platform runs execute 26 cases: 11 causal failures and 15 explicit
control passes, with no skip. Every run exits 2. The mutation worktree finishes clean
at the exact technical commit and every restored source hash equals its baseline hash.

## Obligation, cloud and review truth

The accepted 111-row AWS disposition remains unchanged: 100 native executing
replacements, nine real-AWS `EXTERNAL_PENDING` rows, one invalid duplicate retired
row and one PO-superseded raw-secret API row. LocalStack is real local provider-path
evidence, not real AWS evidence. No cloud credential-chain, quota/throttling,
service-controlled lifecycle or long-running real-AWS result is counted green.

This package is a stationary review candidate, not final acceptance. Two independent
static read-only PASS reviews and the separate real-AWS External release gate remain
open. Neither is represented as locally completed.

## Reproducible local execution boundary

The successful runs use .NET SDK 10.0.302 with `rollForward=disable`, a task-local
`DOTNET_CLI_HOME`, the existing package cache, disabled MSBuild node reuse and disabled
shared compilation. Direct MTP project execution explicitly selects
`VICIONE_TESTS__Profile=UnitArchitecture`. Earlier silent local stalls were isolated
to restricted-sandbox process/NamedPipe behavior plus orphaned MSBuild nodes; the
identical product commands pass outside that boundary after targeted build-server
shutdown. No test, runner, package or product assertion was weakened for the tooling
environment.

## Deliberate exclusions

- The run-scoped fixture ownership token is never evidence and is not committed.
- Real AWS was not executed and remains an explicit External release gate.
- Independent reviewer conclusions are not authored by the implementation context and
  therefore are not claimed here.
- The generated 8,309-entry CHANGELIST and all 103 identity-tool self-tests pass. The
  separate whole-fork `identity_gate.py scan` is not claimed green: its persisted
  baseline/API mapping predates the deliberately retired and moved legacy paths and
  currently reports 59,494 stale bindings. Repairing and independently reviewing that
  repository-wide provenance projection is a separately scoped governance task, not an
  AWS product/test result.
