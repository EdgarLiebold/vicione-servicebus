# Canonical workflow syntax correction evidence

## Frozen subject

- Parent evidence commit: `78819f6f2a2cf613fd018688be6cf96e03e924ff`
- Technical correction commit: `6aecdf4dc55dd631f0605dd45e66ed0af4e232d7`
- Technical correction tree: `480305e077ba4ca30ef88f32ea961623586b642f`
- Product and native-test C# delta: none

## Red-team counterexample closed

The dependency-free workflow reader now accepts only the repository's explicit canonical set of
unquoted plain top-level GitHub Actions keys. Workflow defaults are not in that set. Quoted, escaped,
tagged, complex, anchored, merged, duplicate and BOM-prefixed top-level forms fail closed before the
jobs are interpreted. Hostile tests cover defaults before and after `jobs`, direct and escaped quoted
keys, tags, complex keys, anchors and merges, a UTF-8 BOM and duplicate top-level keys. A positive test
covers every supported canonical key.

## Stationary validation

| Check | Exact command | Verdict |
|---|---|---:|
| Focused workflow boundary | `python3 -m unittest tools.ci.tests.test_verification_model -v` | PASS, 22/22, exit 0 |
| Verification model | `python3 tools/ci/verification/model.py` | PASS, exit 0 |
| CI transition-tool tests | `python3 -m unittest discover -s tools/ci -p 'test_*.py'` | PASS, 228/228, exit 0 |
| Identity-tool tests | `python3 -m unittest discover -s tools/identity -p 'test_*.py'` | PASS, 103/103, exit 0 |
| Change-list check | `python3 tools/identity/change_list.py` | PASS, exit 0 |
| Patch whitespace | `git diff --check 78819f6f2a2cf613fd018688be6cf96e03e924ff..6aecdf4dc55dd631f0605dd45e66ed0af4e232d7` | PASS, exit 0 |

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
