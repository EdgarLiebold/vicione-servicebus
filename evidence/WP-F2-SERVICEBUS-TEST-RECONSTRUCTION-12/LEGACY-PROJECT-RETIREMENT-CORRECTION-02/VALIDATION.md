# Final legacy project retirement correction evidence

## Frozen subject

- Parent evidence commit: `a4a546b7215ae649d15fbd179657ea22ab9cd34f`
- Technical correction commit: `d8f67790bf77ddcaa43c82c410931ab7c8f708d0`
- Technical correction tree: `b365c083f29a3aefc5e8b7f82ff565fb94bcd963`
- Product and native-test C# delta: none

## Red-team counterexamples closed

1. Every modeled verification job now has exactly four steps in fixed order: commit-pinned checkout,
   commit-pinned SDK setup, the canonical model-owned selection call and commit-pinned result upload.
   Job-level conditions, error continuation, defaults, containers and reusable jobs are rejected; an
   additional shell step or any other fifth step is rejected independently of its spelling.
2. The four commentless/binary exceptions are checked against a literal independent test contract.
   Baseline sources are unique, the validator binds exact baseline-source/current-target pairs, and
   `NOTICE` plus the dedicated `MODIFICATIONS.md` section must equal the complete ordered contract.
   Wrong sources, missing map entries and additional stale document entries are fail-closed.

## Stationary validation

| Check | Exact command | Verdict |
|---|---|---:|
| Verification model | `python3 tools/ci/verification/model.py` | PASS, exit 0 |
| CI transition-tool tests | `python3 -m unittest discover -s tools/ci -p 'test_*.py'` | PASS, 217/217, exit 0 |
| Identity-tool tests | `python3 -m unittest discover -s tools/identity -p 'test_*.py'` | PASS, 103/103, exit 0 |
| Change-list check | `python3 tools/identity/change_list.py` | PASS, exit 0 |
| Patch whitespace | `git diff --check a4a546b7215ae649d15fbd179657ea22ab9cd34f..d8f67790bf77ddcaa43c82c410931ab7c8f708d0` | PASS, exit 0 |

The CI process-tree tests ran outside the workspace sandbox because they intentionally execute `ps`
and own child process groups. No .NET or MSBuild process was needed or started for this correction.

## Bound raw results

- `CORRECTION_DIFF.txt`
- `final-verification-model.txt`
- `final-ci-tool-tests.txt`
- `final-identity-tool-tests.txt`
- `final-change-list.txt`
- `SHA256SUMS`
