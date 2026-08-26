# Legacy project retirement correction evidence

## Frozen subject

- Parent evidence commit: `081c396faf3f1465741e282985c297ecdd011e7c`
- Technical correction commit: `0d9d73f7855c5b65fd62aada4c7c03bc57373e9d`
- Technical correction tree: `3092b8faba2007835de0f43c5e2bdafc5c0a0393`
- Product and native-test C# delta: none

## Corrections

1. Every job in `build/verification/VERIFICATION_MODEL.json` is now coupled to exactly one
   unconditional canonical `python3 tools/ci/verify.py --selection <model-selection>` workflow step.
   Missing, wrong, duplicate, conditional, `continue-on-error` and shell-composed invocations are
   rejected. Required CI executes the model validator before the transition-tool self-tests.
2. The deleted Abstractions text fixture is no longer a legal-format exception. The four retained
   commentless or binary files have explicit baseline-source-to-current-target bindings; moved
   solution and transport-test files no longer depend on a derived path that does not exist.
3. `NOTICE`, `MODIFICATIONS.md` and the generated `CHANGELIST.md` agree with the retained file set.

## Stationary validation

| Check | Exact command | Verdict |
|---|---|---:|
| Verification model | `python3 tools/ci/verification/model.py` | PASS, exit 0 |
| CI transition-tool tests | `python3 -m unittest discover -s tools/ci -p 'test_*.py'` | PASS, 215/215, exit 0 |
| Identity-tool tests | `python3 -m unittest discover -s tools/identity -p 'test_*.py'` | PASS, 100/100, exit 0 |
| Change-list check | `python3 tools/identity/change_list.py` | PASS, exit 0 |
| Patch whitespace | `git diff --check 081c396faf3f1465741e282985c297ecdd011e7c..0d9d73f7855c5b65fd62aada4c7c03bc57373e9d` | PASS, exit 0 |

The CI process-tree tests were run outside the workspace sandbox because they intentionally execute
`ps` and own child process groups; the identical sandbox run failed only with `Operation not
permitted: 'ps'`. No .NET or MSBuild process was started for this Python/workflow/legal correction.

The identity takeover scanner's full `scan` mode is a historical snapshot-evidence generator, not a
live acceptance gate after authorized product removals and public-API changes. Running it against the
current tree therefore reports the accumulated post-takeover delta and stale persisted takeover
records. The current legal-format contract is exercised directly by
`test_active_legal_documents_and_format_exceptions_are_consistent` and is green in the bound 100-test
identity-tool run.

## Bound raw results

- `CORRECTION_DIFF.txt`
- `final-verification-model.txt`
- `final-ci-tool-tests.txt`
- `final-identity-tool-tests.txt`
- `final-change-list.txt`
- `SHA256SUMS`
