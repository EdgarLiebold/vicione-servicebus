# ActiveMQ native closure — validation

## Frozen subject

- Technical commit: `fa938ae508bbc422bd281d82ae5d14ba995517d8`
- Technical tree: `4f0e47743584c33e62035fe3bdc6f801aac28867`
- Technical parent: `fb52f80077aa4591279bbc954c6a95db2ae2b7e8`
- Branch and remote: `feature/activemq-native-closure` / `origin/feature/activemq-native-closure`
- The technical commit was already pushed before this evidence was assembled. This directory is a
  direct evidence child and does not alter product, test, build, package, solution or workflow bytes.

## Terminal R0 closure and retirement

The committed `.testagent/activemq-native-obligation-map.tsv` is the authoritative 113-row mapping.
Its SHA-256 is `05e3a90bd09a8303eb5ca0ec68c06b1251655a348bc230af1bcbe5317dddc66a`.
It contains `OBL-R0-BLD-0095` exactly once and the contiguous range
`OBL-R0-BRK-0355..OBL-R0-BRK-0466` exactly once, with 40 UnitArchitecture and 73 LocalIntegration
owners. Those 113 obligations account for all 179 historical execution identities; no pending or
non-executing disposition is represented as green.

The inherited project retirement is atomic. All 34 C# files and its four tracked build/configuration
files are absent from the technical tree, as is its old verification expected-list. `RETIRED_PATHS.tsv`
binds every parent blob and `TECHNICAL_DIFF.txt` binds the complete 56-path technical delta. Git is the
archive; no inert renamed copy remains in the working tree.

## Positive execution

- Locked Engineering restore: exit 0; raw log and binlog bound.
- Engineering Release build: exit 0, 0 warnings, 0 errors; raw log and binlog bound.
- Unfiltered serial UnitArchitecture: 2158 total, 2158 passed, 0 failed, 0 skipped, 0 pending and
  0 other across 18 CTRF files.
- Unfiltered LocalIntegration: 244 total, 244 passed, 0 failed, 0 skipped, 0 pending and 0 other across
  seven CTRF files. The wrapper binds run identity `vicione-b4f830b1f675`, dynamic loopback endpoints,
  PostgreSQL, Azurite, LocalStack, ActiveMQ Classic and Artemis readiness, the ActiveMQ outage-control
  channel, broker logs and guarded teardown.

The profile commands in `FINAL_RESULTS.json` are the commands that produced these raw results. MTP is
the process verdict, xUnit is discovery/result ownership, and the repository test configuration makes
skips and warnings fail. No VSTest separator, NUnit runner, private receipt system or Python policy
validator participates.

## One-cause attacks

`MUTATION_MANIFEST.json` binds baseline bytes, exactly one replacement, mutant bytes and post-restore
bytes for M01–M17. `MUTATION_EXECUTION.tsv` binds the fully expanded build and test commands and their
exit codes. M01–M15 and M17 are product mutations; M16 is a workflow-gate sabotage. Every compiled
mutant built with 0 warnings and 0 errors. Every focused MTP run exited 2 and failed at the expected
behavioral boundary, with 0 skips. M15 additionally uses a fresh run-scoped ActiveMQ/Artemis fixture
and binds both broker logs. M14 intentionally demonstrates the missing recovery transition through
the test's bounded five-second causal wait; it is not a successful absence-until-timeout oracle.

After every mutation the target SHA-256 equals its technical baseline. The final Git status has no
technical modification; only this evidence child and the accompanying append-only status/CHANGELIST
records are added.

## Scope and claims

This evidence claims local ActiveMQ capability only: Classic OpenWire, Classic AMQP and Artemis under
fresh local digest/build-context fixtures. ActiveMQ has no deferred cloud/account boundary. It does
not claim GitHub Actions execution; the four repository workflows remain manually disabled by PO
decision while their future command shape stays statically fail-closed.

Final acceptance additionally requires two independent static read-only reviews of the exact frozen
technical commit and its direct evidence child. Those reviews may not run .NET, MSBuild, Docker or
network processes and may not change repository bytes.
