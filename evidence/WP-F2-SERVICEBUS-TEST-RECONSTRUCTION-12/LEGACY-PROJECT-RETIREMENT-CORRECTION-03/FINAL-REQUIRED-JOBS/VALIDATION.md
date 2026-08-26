# Complete required workflow evidence

## Frozen subject

- Parent evidence commit: `6ab5d862f803830fd1439e4ae8551aea3facddbe`
- Technical correction commit: `d71dc2cd11e2bf5399cd1088b6e728c93c6dc354`
- Technical correction tree: `bcf0e162c44fc853157c0e42d7bcac81d028abcc`
- Product and native-test C# delta: none

## Red-team counterexamples closed

The canonical workflow contract now covers every required job. The three support jobs bind all
non-comment executable lines: the complete model/CI/identity tooling gate, both root and engineering
restore/build chains, and the pack dependency list, clean output, locked restore, build, no-restore
pack, package-presence check, hashes and pinned upload. No-op, deleted, reordered or hidden controls
fail closed. SDK derivation is proved by both a global.json-only mismatch and a different matching
stable SDK version.

## Stationary validation

| Check | Exact command | Verdict |
|---|---|---:|
| Focused workflow boundary | `python3 -m unittest tools.ci.tests.test_verification_model -v` | PASS, 47/47, exit 0 |
| Verification model | `python3 tools/ci/verification/model.py` | PASS, exit 0 |
| CI transition-tool tests | `python3 -m unittest discover -s tools/ci -p 'test_*.py'` | PASS, 253/253, exit 0 |
| Identity-tool tests | `python3 -m unittest discover -s tools/identity -p 'test_*.py'` | PASS, 103/103, exit 0 |
| Change-list check | `python3 tools/identity/change_list.py` | PASS, exit 0 |
| Patch whitespace | `git diff --check 6ab5d862f803830fd1439e4ae8551aea3facddbe..d71dc2cd11e2bf5399cd1088b6e728c93c6dc354` | PASS, exit 0 |

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
