# Workflow-default correction evidence

## Frozen subject

- Parent evidence commit: `1c5c90cb9f73a3f964ff29ecb2f25ff3acccdb49`
- Technical correction commit: `4851582c155d4b30900185c997a7dd6a58192a8e`
- Technical correction tree: `b32ef206e279f40e3e1403ce95cbe660400cee4a`
- Product and native-test C# delta: none

## Red-team counterexample closed

The workflow parser now rejects a top-level `defaults` section before interpreting verification
jobs. A workflow-wide shell template such as `bash {0} || true` can therefore no longer turn every
otherwise valid verification step falsely green. The focused hostile test supplies that complete
workflow shape and requires a fail-closed `WorkflowShapeError`.

## Stationary validation

| Check | Exact command | Verdict |
|---|---|---:|
| Verification model | `python3 tools/ci/verification/model.py` | PASS, exit 0 |
| CI transition-tool tests | `python3 -m unittest discover -s tools/ci -p 'test_*.py'` | PASS, 218/218, exit 0 |
| Identity-tool tests | `python3 -m unittest discover -s tools/identity -p 'test_*.py'` | PASS, 103/103, exit 0 |
| Change-list check | `python3 tools/identity/change_list.py` | PASS, exit 0 |
| Patch whitespace | `git diff --check 1c5c90cb9f73a3f964ff29ecb2f25ff3acccdb49..4851582c155d4b30900185c997a7dd6a58192a8e` | PASS, exit 0 |

The CI process-tree tests ran outside the workspace sandbox because they intentionally execute `ps`
and own child process groups. No .NET or MSBuild process was needed or started for this correction.

## Bound raw results

- `CORRECTION_DIFF.txt`
- `final-verification-model.txt`
- `final-ci-tool-tests.txt`
- `final-identity-tool-tests.txt`
- `final-change-list.txt`
- `SHA256SUMS`
