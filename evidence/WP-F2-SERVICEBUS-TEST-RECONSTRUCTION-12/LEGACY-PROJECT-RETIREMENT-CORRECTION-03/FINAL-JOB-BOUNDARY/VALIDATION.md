# Canonical verification-job correction evidence

## Frozen subject

- Parent evidence commit: `90fd51dc04eec1ba04c0d440f7cc445089525085`
- Technical correction commit: `8c9182a53a4b772eb8c65973db30530b81fd49fc`
- Technical correction tree: `a84588c8c730d716ca3ac51d01a480ca8fc7ed7f`
- Product and native-test C# delta: none

## Red-team counterexample closed

Every model-owned verification job now uses one exact canonical plain-key structure. Its direct keys
have a fixed order, `runs-on` is exactly `ubuntu-24.04`, the timeout is a positive integer, and the
only optional job environment is the globalization setting required by the database fixtures. Quoted,
escaped, complex, duplicate, reordered, hidden or unsupported job controls fail closed. The already
exact four-step parser continues to bind checkout, SDK setup, the selection command and result upload.

## Stationary validation

| Check | Exact command | Verdict |
|---|---|---:|
| Focused workflow boundary | `python3 -m unittest tools.ci.tests.test_verification_model -v` | PASS, 31/31, exit 0 |
| Verification model | `python3 tools/ci/verification/model.py` | PASS, exit 0 |
| CI transition-tool tests | `python3 -m unittest discover -s tools/ci -p 'test_*.py'` | PASS, 237/237, exit 0 |
| Identity-tool tests | `python3 -m unittest discover -s tools/identity -p 'test_*.py'` | PASS, 103/103, exit 0 |
| Change-list check | `python3 tools/identity/change_list.py` | PASS, exit 0 |
| Patch whitespace | `git diff --check 90fd51dc04eec1ba04c0d440f7cc445089525085..8c9182a53a4b772eb8c65973db30530b81fd49fc` | PASS, exit 0 |

The CI process-tree tests ran outside the workspace sandbox because they intentionally execute `ps`
and own child process groups. No .NET or MSBuild process was needed or started for this correction.

## Bound raw results

- `CORRECTION_DIFF.txt`
- `final-focused-workflow-tests.txt`
- `final-verification-model.txt`
- `final-ci-tool-tests.txt`
- `final-identity-tool-tests.txt`
- `final-change-list.txt`
- `SHA256SUMS`
