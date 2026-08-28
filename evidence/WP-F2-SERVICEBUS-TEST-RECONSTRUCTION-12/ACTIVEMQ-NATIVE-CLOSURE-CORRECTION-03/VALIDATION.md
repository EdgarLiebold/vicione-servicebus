# ActiveMQ native closure — final compiler-input and API-boundary evidence

## Frozen subject

- Technical commit: `d3afdfa386909317f20894e47a6137601b215f68`
- Tree: `4249d72bfb541c27d8d5b14dda55b0a898383ab3`
- Parent: `f5dd3c5e584c1a25a190f70c3b73605fd4aabc42`
- Technical comparison baseline: `3d191610111d30df4bf3e724a614306e16eb3049`
- Authorized correction baseline: `7c140863d042c0b7c8ce04057c6e98c0022222fe`
- Technical delta since the preceding Correction-02 technical subject: 19 paths, 576 insertions,
  99 deletions. `TECHNICAL_DIFF.patch.gz` is the exact binary-capable patch after excluding older
  evidence, generated status and generated CHANGELIST bytes.

This directory supersedes the positive acceptance claims of
`ACTIVEMQ-NATIVE-CLOSURE-CORRECTION-02`. Earlier evidence bytes remain immutable historical
checkpoints.

## Closed product, API and build boundaries

- Public ActiveMQ host construction is typed (`OpenWire` or `Amqp`), requires a valid explicit
  port and valid host, rejects unknown enums, hostile URI components, relative URIs and the
  unavoidable default-struct state before URI projection.
- Benchmark ActiveMQ options require host, named protocol and port on every parse, clear credentials,
  TLS and effective settings before reparsing, select the exact provider, and remain an internal
  sealed implementation detail exposed only to the narrowly named test friend.
- The Unit solution contains the complete Release-evaluated product ProjectReference closure,
  including Azure Service Bus Core and PostgreSQL transport dependencies reached by Benchmark.
- The ActiveMQ no-wall-clock gate evaluates the real Release `Compile` items in one Roslyn
  compilation with evaluated SDK `Using` items, aliases/static imports, target-framework defines
  and language version. It catches direct, qualified, implicit-using, alias, method-group/delegate,
  cross-tree and conditional-compilation references to `Task.Delay` and `Thread.Sleep`. The source
  set itself must be non-empty before the synthetic global-usings tree is added.

## Positive execution

Every command in `FINAL_RESULTS.json` ran from the exact frozen technical checkout. Raw combined
stdout/stderr, CTRFs and MSBuild binlogs are included.

| Gate | Result |
|---|---:|
| Engineering locked restore | Exit 0 |
| Engineering Release build | Exit 0, 0 warnings, 0 errors |
| UnitArchitecture | 2,228/2,228 passed, 0 failed, 0 skipped, 19 CTRFs |
| ActiveMQ unit module | 133/133 passed |
| Benchmark unit module | 21/21 passed |
| Architecture module | 129/129 passed |
| LocalIntegration | 244/244 passed, 0 failed, 0 skipped, 7 CTRFs |
| ActiveMQ LocalIntegration | 95/95 passed |
| CI tool self-tests | 257/257 passed |
| Identity tool self-tests | 103/103 passed |
| Verification model | Exit 0 |

The LocalIntegration command created run-scoped fixture `vicione-751339deadb8` with PostgreSQL,
Azurite, LocalStack, ActiveMQ Classic and Artemis on loopback. `positive/local-fixture` binds its
endpoint projection, five broker logs, empty findings and the request/result pairs for two controlled
ActiveMQ interruptions and successful recoveries. The runner's private run-root token is deliberately
not retained. In command mode, the runner owns the actual `artifacts/run-output/...` location; the
complete actual run directory was copied only after guarded teardown.

The process-enumeration checks were run outside the filesystem sandbox because the local sandbox
cannot expose the required process tree. This is the previously diagnosed environment restriction;
no assertion, product rule or test count was weakened.

## Mutation closure

M30-M62 are 33 independent, byte-exact attacks against the final technical tree. Each exact patch
changed one target, each mutant built in Release with Exit 0, and each designated filtered MTP run
exited 2 for its own causal assertion with zero skips. Every target was restored to its frozen
baseline SHA-256 and the detached mutation worktree ended clean.

For each mutation this evidence includes the compressed exact patch, execution metadata, build log,
build binlog, test log and CTRF. `MUTATION_MANIFEST.json` binds compressed and expanded patch hashes,
baseline/mutant/restore hashes and semantic intent. `MUTATION_EXECUTION.tsv` records the fully
expanded argv and cwd that actually ran. M61 specifically removes only the evaluated `Compile` item
projection: the new non-empty-input assertion fails at its own line while the target restores
byte-identically.

## Closure and limits

The inherited ActiveMQ closure remains exactly 113 unique R0 obligations representing 179 historical
execution identities, with 40 UnitArchitecture and 73 LocalIntegration owners, all
`REPLACED_EXECUTING`. The retired inherited project remains absent. GitHub workflows remain globally
manual/disabled as authorized by the PO; their checked-in commands and floors remain locally
fail-closed. No external-cloud execution is claimed by this local provider evidence.

This is an evidence candidate, not self-acceptance. Two independent read-only reviews must bind this
technical commit and its final evidence child before remote publication.
