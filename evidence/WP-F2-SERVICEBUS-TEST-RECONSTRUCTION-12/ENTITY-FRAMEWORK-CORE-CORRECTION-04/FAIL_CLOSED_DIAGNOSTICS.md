# Fail-closed diagnostics

These files are diagnostic evidence, not passing acceptance evidence.

## `positive/fail-closed-stale-workflow-floor.log`

The first unfiltered run after adding the consumed-sentinel owner executed 1887 cases. The product
tests were green, but the architecture test rejected `.github/workflows/native-tests.yml` because
its executable minimum still declared 1886. The workflow floor was corrected before the final
technical commit. The final architecture test and the final unfiltered 1888-case run are green.

## `positive/fail-closed-lost-cleanup-signal.log`

The next unfiltered run executed 1887 cases and found the existing `GreenCache` capacity case
`SimpleValuesAboveCapacity_AreReducedToANonEmptyBoundedSet(60, false)`: 200 values were added, none
were removed, and the cache remained at 200. Static and causal analysis identified a discarded
cleanup request while the single queued cleanup waited for the tracker lock. The final repair is
protected by the deterministic queued-cleanup owner and M15. The pre-existing capacity theory was
also stressed repeatedly before the final complete run.

Neither log is used to calculate an acceptance count. They demonstrate that the gates failed closed
and that the defects were fixed rather than hidden by changing an assertion, filter or minimum.
