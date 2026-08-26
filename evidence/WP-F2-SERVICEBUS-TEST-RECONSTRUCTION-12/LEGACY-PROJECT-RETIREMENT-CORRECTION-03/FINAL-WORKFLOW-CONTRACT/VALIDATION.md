# Required workflow execution contract evidence

## Frozen subject

- Parent evidence commit: `06be85ff2e8de5e0a92fd9c894d57ba48f9a1df7`
- Technical correction commit: `478b18bc4027786f75df300c5b0bca7364339b97`
- Technical correction tree: `83a1e509668668e7be2a7b1f0905401f22e0e9b6`
- Product and native-test C# delta: none

## Red-team counterexample closed

The complete executable workflow header is canonical and fail-closed. It binds the required name,
all-branch push, pull-request and manual triggers, read-only repository permission and exactly three
global environment values. The SDK value is derived from `global.json`. PATH/PYTHONPATH injection,
trigger removal or filtering, elevated permission, workflow concurrency and divergent SDK selection
are rejected before jobs run. Canonical top-level syntax, canonical Verify-job structure and exact
four-step execution together form one closed workflow contract.

## Stationary validation

| Check | Exact command | Verdict |
|---|---|---:|
| Focused workflow boundary | `python3 -m unittest tools.ci.tests.test_verification_model -v` | PASS, 39/39, exit 0 |
| Verification model | `python3 tools/ci/verification/model.py` | PASS, exit 0 |
| CI transition-tool tests | `python3 -m unittest discover -s tools/ci -p 'test_*.py'` | PASS, 245/245, exit 0 |
| Identity-tool tests | `python3 -m unittest discover -s tools/identity -p 'test_*.py'` | PASS, 103/103, exit 0 |
| Change-list check | `python3 tools/identity/change_list.py` | PASS, exit 0 |
| Patch whitespace | `git diff --check 06be85ff2e8de5e0a92fd9c894d57ba48f9a1df7..478b18bc4027786f75df300c5b0bca7364339b97` | PASS, exit 0 |

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
