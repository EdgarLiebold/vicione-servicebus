# Independent required-workflow oracle evidence

## Frozen subject

- Parent evidence commit: `6cf46de99bdc08359b5c2cf2220ea339d99cf26d`
- Technical correction commit: `c287ada951b87f57a2fafeb242934bc94a0820e6`
- Technical correction tree: `afe1b6496366645d2fe25ab8c65d7a67fd2a35df`
- Product and native-test C# delta: none

## Red-team counterexamples closed

Each support job is now independently pinned in the test source by the SHA-256 of its complete
normalized executable contract. Coordinated workflow/validator weakening therefore fails without
changing the independent oracle. Every pack dependency is additionally derived from the two support
gates and the complete model-owned job map, then removed one at a time in tests. All four build
commands and every adjacent ordering, plus every restore/build/clean/pack/pipefail/presence/hash/upload
obligation, are independently mutated.

## Stationary validation

| Check | Exact command | Verdict |
|---|---|---:|
| Focused workflow boundary | `python3 -m unittest tools.ci.tests.test_verification_model -v` | PASS, 47/47, exit 0 |
| Verification model | `python3 tools/ci/verification/model.py` | PASS, exit 0 |
| CI transition-tool tests | `python3 -m unittest discover -s tools/ci -p 'test_*.py'` | PASS, 253/253, exit 0 |
| Identity-tool tests | `python3 -m unittest discover -s tools/identity -p 'test_*.py'` | PASS, 103/103, exit 0 |
| Change-list check | `python3 tools/identity/change_list.py` | PASS, exit 0 |
| Patch whitespace | `git diff --check 6cf46de99bdc08359b5c2cf2220ea339d99cf26d..c287ada951b87f57a2fafeb242934bc94a0820e6` | PASS, exit 0 |

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
