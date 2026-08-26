# Core pipeline mutation manifest

## Frozen subject

- Technical commit: `ea262b869a67b3b095ef064653eeb3b1e0a5a900`
- Technical tree: `b4e6129795f800d7e5b1667e84d9245e7631b2ad`
- Disposable worktree: `/private/tmp/vsb-core-pipeline-a-plus-20260826`
- Build: Release, locked graph restored before the matrix, no incremental mutation build reuse, one MSBuild node, build servers disabled
- Test platform: native xUnit 4 on Microsoft Testing Platform 2

`MUTATION_RECIPES.json` is the byte-level authority for every edit. It binds the target,
baseline hash, exact old and new bytes, occurrence, and resulting mutant hash.
`MUTATION_EXECUTION.json` binds the exact common build argument vector and each native test
selection, expected floor, exit code, raw CTRF hash, and result count.

## Results

| ID | Single injected defect | Causal native verdict |
|---|---|---|
| M01 | Remove the handler constructor null guard | 1/1 failed: no `ArgumentNullException` |
| M02 | Permit a null context-filter decision task | 1/1 failed: wrong exception contract |
| M03 | Invert the synchronous context-filter decision | 1/2 failed: rejected context forwarded |
| M04 | Invert the asynchronous context-filter decision | 2/2 failed: both decisions reversed |
| M05 | Treat an unrequested initial OCE token as caller cancellation | 1/2 failed: dependency failure was not retried |
| M06 | Treat an unrequested active-retry OCE token as caller cancellation | 1/1 failed: active dependency failure was not retried |
| M07 | Replace the configured transaction timeout with 30 seconds | 1/1 failed: exact options mismatch |
| M08 | Replace inactive-context renewal with active-context replacement | 2/954 failed: nested active ownership and fresh retry ownership |
| M09 | Replace an externally owned context | 1/1 failed: exact context identity changed |
| M10 | Remove the owned transaction commit | 1/1 failed: commit count mismatch |
| M11 | Remove the owned transaction rollback | 1/1 failed: rollback count and exception mismatch |
| M12 | Permit a null transaction-factory result | 1/1 failed: fail-closed exception missing |
| M13 | Suppress async flow in the default transaction-scope overload | 1/1 failed: scope could not cross the continuation |
| M14 | Suppress async flow in the timeout transaction-scope overload | 1/1 failed: scope could not cross the continuation |
| M15 | Accept a negative transaction timeout | 1/1 failed: validation result missing |
| M16 | Remove the new retry-cancellation requirement projection | 1/1 failed: compiled metadata no longer matched the projection |

Every mutation built successfully and every test command exited `2` because the intended assertion
failed. No mutation survived and no test was skipped. After every run, the exact baseline bytes were
restored. The final disposable worktree was clean and all seven affected files matched their frozen
baseline SHA-256 values.
