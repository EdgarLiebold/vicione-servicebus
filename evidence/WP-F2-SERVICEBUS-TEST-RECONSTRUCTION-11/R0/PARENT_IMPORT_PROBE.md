# R0 probe — the unconditional parent import, and what the forbidden `Exists` guard actually does

Lead plan section 6 item 6: the nested `Directory.Build.props` and `Directory.Build.targets` under
`tests2/` (later `tests/`) resolve the next parent **at the beginning of the file** with
`MSBuild::GetPathOfFileAbove` and import it **without an `Exists` guard**. "Der Parent ist
verpflichtend; fehlt er, muss bereits die MSBuild-Evaluation scheitern. Ein `Exists`-Guard oder ein
später Ersatzfehler in einem Target wäre fail-open beziehungsweise zu spät und ist verboten."

`REQ-TEST-103` restates it: "no Exists guard makes it fail-open", proven by two import-removal
mutants.

Measured in a disposable two-level project under the session scratchpad. The bound candidate was not
touched.

## Measurements

A repository-level `Directory.Build.props` sets `RepositoryWideRuleApplied=true`; a nested
`tests2/Directory.Build.props` imports it and sets `TestsWideRuleApplied=true`; the project prints
both.

| # | Import shape | Parent file | Exit | Observed |
|---|---|---|---:|---|
| 1 | Unconditional (plan form) | present | 0 | `repo=true tests=true` — inheritance real |
| 2 | Unconditional (plan form) | **removed** | **1** | `MSB4020: Der Wert "" des Project-Attributs im <Import>-Element ist ungültig` — fails during **evaluation**, before any target runs |
| 3 | `Exists` guard (forbidden form) | present | 0 | `repo=true tests=true` — indistinguishable from the plan form while nothing is broken |
| 4 | `Exists` guard (forbidden form) | **removed** | **0** | `repo= tests=true` — **the build succeeds and the repository-wide rule is silently empty** |
| 5 | Unconditional (plan form) restored | present | 0 | `repo=true tests=true` — the probe is not permanently in any state |

## Conclusion

Row 3 versus row 4 is the whole argument for the ban. With the guard in place the two shapes are
indistinguishable as long as nothing is wrong; the difference appears only in the situation the
control exists for. At that moment the guarded form loses every repository-wide rule — `LangVersion`
handling, package boundaries, analyzer settings, the completeness flag itself — and still reports
success. Nothing in the build output says a rule went missing.

The unconditional form fails at evaluation with `MSB4020` before a single target executes, which is
what "fail-closed" has to mean here.

## Method note

The first attempt at row 3 was red with the parent present, which would have made row 4 worthless as
a comparison — a negative probe that is red for an unrelated reason proves nothing. The cause was my
own nested quoting inside the `Condition` attribute (`MSB4092: unerwartetes Token`), not a property
of the guard. Capturing the resolved path into a property first and guarding on that property makes
the forbidden shape actually work, which is the only way to measure what it does wrong. The rows
above are from the corrected probe, with row 5 confirming the canonical shape still passes
afterwards.
