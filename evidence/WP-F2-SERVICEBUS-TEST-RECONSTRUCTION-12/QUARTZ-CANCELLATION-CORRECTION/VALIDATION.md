# Quartz scheduled-job cancellation correction

## Frozen technical subject

- Technical commit: `64e88ba5b79a67234bc61f4d65468864f3c3fd46`
- Technical tree: `b4e4d9e33aa2631b59f1cf58a380881b25160b12`
- Product correction commit: `d6e7b6bf2414999803653bcd591e8ae318945a6c`
- Original accepted Quartz evidence: `233c5a54f099c7920afa362479a3e577a2ae6ba3`
- Runtime: .NET SDK `10.0.302`
- Test stack: xUnit 4 on Microsoft Testing Platform 2
- Configuration: `Release`

## Independent review finding

The product review found that `ScheduledMessageJob` caught every exception from endpoint resolution
and send. A causally requested Quartz execution cancellation was therefore logged as a send failure
and converted into a retrying `JobExecutionException`, allowing up to five immediate refires during
job interruption or host shutdown.

The product now has an explicit cancellation boundary before the general failure classifier:

- an `OperationCanceledException` propagates unchanged when the Quartz context token is requested;
- an unrequested dependency cancellation remains a send failure and preserves the existing bounded
  immediate-refire behavior.

This is a product correction, not a test accommodation. The direct send-boundary tests verify the
exact exception identity, the exact Quartz token passed to the endpoint, and the opposite dependency
cancellation outcome.

## Positive verdicts

| Gate | Verdict |
|---|---|
| Engineering Release build | exit 0, 0 warnings, 0 errors |
| UnitArchitecture | 1824/1824 passed, 0 failed, 0 skipped |
| Quartz focused | 87/87 passed, 0 failed, 0 skipped |
| LocalIntegration with run-scoped PostgreSQL and Azurite | 17/17 passed, 0 failed, 0 skipped |

No dependency input changed after the accepted Quartz locked restore. The final engineering build
uses that unchanged graph with `--no-restore`. `POSITIVE_EXECUTION.json` binds the commands, floors,
results, environment decision, and raw-artifact hashes.

## Causal mutation

M13 removes only the new requested-cancellation catch. The mutation builds with zero warning and
zero error. The requested-cancellation test then fails because the product returns an exact
`JobExecutionException` instead of the expected `OperationCanceledException`; the unrequested
dependency-cancellation control remains green. The mutation run therefore distinguishes the two
contracts instead of merely proving that some exception occurred.

`MUTATION_EVIDENCE.json` binds the exact source bytes, baseline and mutant hashes, complete build and
test argument vectors, exit codes, causal result, control result, raw CTRF hash, and restored source
hash. The restored source is byte-identical to the frozen technical commit.

## Verdict

**READY FOR INDEPENDENT RE-REVIEW.** The reported product defect is corrected, the complete local
qualification is green, and the correction is protected by positive, negative, and one-cause
evidence. Final acceptance depends only on the two independent static reviews of this frozen
technical and evidence subject.
