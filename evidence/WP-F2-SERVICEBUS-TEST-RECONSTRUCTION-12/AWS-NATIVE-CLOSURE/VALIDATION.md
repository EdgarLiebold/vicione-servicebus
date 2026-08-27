# AWS native closure — validation

## Bound subject

This package validates technical commit
`e18fcc071ba5bd42d51237a8be8cf5785f00e5c9`, tree
`52702c9b1012aa1d98d403ec5702a54c96086f35`, descended from the accepted product
baseline `427894348e992551c8d2ae15095c416d2cc1b329`. Architecture authority is
`d284f7afeb6902296ac322a7c9c58a480bfb7279` under assignment `PO-2026-08-27-02`.
The technical subject was pushed to `origin/feature/aws-native-closure` before the evidence freeze
and remains an ancestor of that remote ref. The ref may advance only through additive evidence or
correction descendants; its tip is therefore not used as an immutable technical identifier.

`TECHNICAL_DIFF.txt` is the exact 195-path `git diff --name-status` projection from the accepted
baseline through the technical subject. The evidence child does not alter product, test, build,
fixture or workflow behavior.

## Positive execution

- UnitArchitecture: the unfiltered Release solution command passed 2,032/2,032, with zero failure
  and zero skip. The raw solution output is `positive/unit-solution.log.gz`. Seventeen separately named
  CTRF files sum independently to the same 2,032/2,032 with zero pending or other result.
- LocalIntegration: one canonical fresh fixture owned PostgreSQL, Azurite and LocalStack on
  Docker-selected loopback ports. The unfiltered Release solution passed 149/149, and six separately
  named CTRF files sum independently to 149/149. `fixture-findings.json` is empty and binds each of
  the three captured broker-log digests. The run-scoped secret ownership token is deliberately not
  evidence and is not committed.
- Unit, LocalIntegration and complete Engineering Release builds each finished with zero warnings
  and zero errors. Their MSBuild binary logs are bound under `positive/`.
- The focused AWS contribution is SQS 46 Unit + 48 LocalStack, DynamoDB 6 Unit + 9 LocalStack and
  S3 6 Unit + 5 LocalStack.

## Obligation and cloud truth

The frozen 111-row set remains exact: 100 native executing replacements, nine real-AWS
`EXTERNAL_PENDING` rows, one invalid duplicate retired row and one PO-superseded raw-secret API row.
LocalStack is provider-crossing local evidence, not cloud evidence. No credential-chain refresh,
real service quota/throttling, service-controlled TTL/lifecycle deletion or long-running real-AWS
claim is counted green.

## Mutation closure

`MUTATION_MANIFEST.json` binds M01–M14 by exact target path, baseline SHA-256, literal one-occurrence
replacement, mutant SHA-256, expanded build and test commands, owning xUnit method, exit code,
causal failure, CTRF and post-restore SHA-256. Every mutant built with zero errors and warnings; each
owning test then exited 2 with exactly one failed, zero skipped test for the intended reason.

M11 and M12 cross the real local DynamoDB provider boundary. Each has its own fresh LocalStack run,
full wrapper stdout/stderr, endpoint projection, broker log and empty teardown findings in addition
to the causal CTRF. The mutation worktree finishes clean at the technical commit, and every restored
source hash equals its baseline hash.

## Deliberate exclusions

- `positive/unit.json` is not included because a solution-level shared CTRF filename was overwritten
  by successive modules. It is replaced by the 17 uniquely named module CTRFs and the raw unfiltered
  solution log; no result is inferred from the overwritten file.
- `run-root.token` is never included because it is an ephemeral ownership secret, not evidence.
- Real AWS was not executed and remains an explicit external release gate.
