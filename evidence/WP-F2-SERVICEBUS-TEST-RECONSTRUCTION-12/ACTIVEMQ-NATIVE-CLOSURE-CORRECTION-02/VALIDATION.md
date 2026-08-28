# ActiveMQ native closure — final protocol and evidence correction

## Frozen subject

- Technical commit: `3d191610111d30df4bf3e724a614306e16eb3049`
- Tree: `e80eefbed5698f29d9c652fff931549ffdc26634`
- Parent: `c9b0cd2cd0cdec1018ec3985f2a6b2d423e575a9`
- Authorized correction baseline: `7c140863d042c0b7c8ce04057c6e98c0022222fe`
- Technical delta: 21 paths, 1,282 insertions, 70 deletions; the exact binary-capable patch is `TECHNICAL_DIFF.patch.gz`.

This evidence supersedes the positive acceptance claims in `ACTIVEMQ-NATIVE-CLOSURE-CORRECTION` for the final protocol/API, benchmark, URI-hostile-data, Classic timing, and wall-clock guard boundaries. It does not rewrite the earlier bytes.

## Product and API result

The public host API now requires both a typed `ActiveMqTransportProtocol` and an explicit port. No public protocol-less host constructor or host-and-port extension remains. Both typed entry points project `OpenWire` to `activemq` and `Amqp` to `amqp`; the tests inspect the actual configured host address, not merely successful configuration.

The benchmark no longer invents localhost, port 61616, or OpenWire. Host, named protocol, and port are mandatory; protocol names select the exact provider; numeric enum text is rejected; credentials and TLS reach the chosen provider; a repeated parse cannot retain prior coordinates; and the reported logical address is canonical.

The hostile URI cases carry valid explicit ports, so their only rejection cause is now credentials, query, or fragment. Non-positive/absent scheduling delay is proved separately for Classic and Artemis against pre-populated provider-property sentinels. The ActiveMQ no-wall-clock gate rejects direct, aliased, fully qualified, and using-static `Delay`/`Sleep` terminal invocations.

## Positive execution

All positive commands ran from the frozen technical worktree. The complete commands are recorded in `FINAL_RESULTS.json`; their combined stdout/stderr and MSBuild binlogs are hash-bound here.

| Gate | Result |
|---|---:|
| Engineering locked restore | Exit 0 |
| Engineering Release build | Exit 0, 0 warnings, 0 errors |
| UnitArchitecture | 2,215/2,215 passed, 0 failed, 0 skipped, 19 CTRFs |
| ActiveMQ unit module within UnitArchitecture | 130/130 passed |
| Benchmark module within UnitArchitecture | 16/16 passed |
| Architecture module within UnitArchitecture | 124/124 passed |
| LocalIntegration | 244/244 passed, 0 failed, 0 skipped, 7 CTRFs |
| ActiveMQ LocalIntegration module | 95/95 passed |
| CI tool self-tests | 257/257 passed |
| Verification model | Exit 0 |

The LocalIntegration run used one run-scoped fixture, `vicione-62668da77f43`, with PostgreSQL, Azurite, LocalStack, ActiveMQ Classic and Artemis on loopback. Its broker logs, endpoint projection and empty findings document are included. The `--evidence-dir` parser argument is deliberately unused by `--command` mode; the runner's actual run root was copied after guarded teardown. No run-root token or credential material is included.

The first attempt to run the CI tool self-tests inside the filesystem sandbox was not an accepted test verdict: eight tests could not perform their required read-only `ps` process enumeration. The byte-identical command was immediately rerun outside the sandbox and passed 257/257; only that passing rerun is present as `positive/ci-tooltests.log`. This is the established structured diagnosis for the local sandbox failure mode.

## Mutation closure

M30–M40 are eleven independent, single-cause mutations: ten product/API mutations and one repository test-gate sabotage. Every mutant built successfully with 0 warnings and 0 errors, then its designated filtered test process exited 2 with the exact expected failing cases and zero skips. Unrelated data rows in the same Theory remained green where applicable.

For each mutation the evidence contains:

1. the gzip-bound exact unified patch plus compressed and expanded SHA-256;
2. the frozen baseline file SHA-256;
3. the reconstructed mutant file SHA-256;
4. the exact build and test argv, cwd and exit codes;
5. build log and binlog;
6. test log and machine-readable CTRF;
7. the post-restore SHA-256, equal to the frozen baseline.

The patches reconstruct byte-exactly against the technical tree. The detached mutation worktree was clean again before positive execution. `MUTATION_MANIFEST.json` binds semantics and hashes; `MUTATION_EXECUTION.tsv` binds commands and raw artifacts.

## Closure and limits

The inherited ActiveMQ closure remains 113 unique R0 obligations representing 179 historical execution identities, with 40 UnitArchitecture and 73 LocalIntegration owners. All remain `REPLACED_EXECUTING`; no old test project was reintroduced. Workflows remain globally manual/disabled as authorized by the PO, but their checked-in commands and floors are still fail-closed and validated locally.

This correction does not claim that the optional real-cloud External cohort ran. It does claim the complete local provider estate listed above, including both ActiveMQ brokers and the Azurite-backed Azure Table cohort.
