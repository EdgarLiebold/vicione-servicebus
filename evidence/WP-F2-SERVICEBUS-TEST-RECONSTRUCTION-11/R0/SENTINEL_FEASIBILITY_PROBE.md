# R0 probe — the in-process completeness sentinel is buildable as specified

Lead plan section 12.1 and section 6 item 2 specify a completeness sentinel that must satisfy four
constraints simultaneously. This probe establishes by measurement, before any F1 design work, that
all four hold at the same time. Built in a disposable project under the session scratchpad; the
bound candidate was not touched.

## The four constraints and their measured result

| Constraint (Lead plan) | Result |
|---|---|
| The extension project references **only** `xunit.v3.extensibility.core` 4.0.0 — no `xunit.v3.mtp-v2`, no `xunit.v3.core`, no runner | **Holds.** A project with that single package reference, `IsPackable=false`, `IsTestProject=false`, compiles the obligation attribute, the thread-safe receipt set, the invocation interceptor and the assembly fixture with 0 warnings and 0 errors |
| A stable metadata marker reports the obligation **at actual xUnit invocation**, not at discovery | **Holds.** `BeforeAfterTestAttribute.Before(MethodInfo, IXunitTest)` applied at assembly level fires per executed test and reads `TestObligationAttribute` off the method |
| A mismatch fails in the assembly fixture's **cleanup phase** and surfaces natively as `ITestAssemblyCleanupFailure` with a non-zero MTP exit | **Holds.** MTP reports `fehlerhaft [Test Assembly Cleanup Failure (…)]` and the process exits **2** |
| The sentinel never starts or discovers tests, never parses stored results, never emits its own PASS | **Holds by construction.** It only compares two in-memory sets inside `IAsyncLifetime.DisposeAsync` and throws; MTP remains the sole execution and result owner |

## Measurements

Identical code and identical preconditions in all three rows; only the named property changes.

| # | Changed property | Exit | Reported |
|---|---|---:|---|
| 1 | **Positive control** — expected set equals the carried obligations | **0** | `gesamt: 1 · fehlgeschlagen: 0 · erfolgreich: 1`, no cleanup node |
| 2 | **Negative** — expected set demands `OBL-2`, which no test carries | **2** | `Test Assembly Cleanup Failure` node, `System.InvalidOperationException : VOSB0001 obligation set mismatch. missing=[OBL-2] unknown=[]`, `gesamt: 2 · fehlgeschlagen: 1 · erfolgreich: 1` |
| 3 | **Mandatory mutant** — the `[TestObligation]` marker is removed while the test itself still exists and still passes | **2** | `missing=[OBL-1] unknown=[]`; the test itself is still counted as `erfolgreich: 1`, and the run fails **only** through the cleanup node |

Row 3 is the section 12.1 mutant "Entfernen einer `TestObligation`-Angabe". It is the sharpest of
the three because the ordinary test verdict stays green and the total test count is unchanged: a
gate that watched only pass/fail counts would report success. The failure message names exactly the
assurance under test, so the probe is red for its own reason.

Row 1 versus rows 2 and 3 proves the probe is not permanently red.

## Shape that the measurement validates

The extension project (target: `tests2/Testing/ViciOne.ServiceBus.Testing.Xunit`) carries:

- `TestObligationAttribute(string id) { string? VariantKey }` — method- and class-level, inheritable,
  `AllowMultiple`, so one test may discharge several obligations and a theory can key its variants;
- a static receipt set over `ConcurrentDictionary` with ordinal comparison, thread-safe under xUnit's
  default parallelization;
- `ObligationInterceptorAttribute : BeforeAfterTestAttribute`, applied once per assembly, recording
  `id` or `id#variantKey` on invocation;
- `abstract ObligationCompletenessFixture : IAsyncLifetime` whose `DisposeAsync` computes both set
  differences and throws a stable `VOSB`-coded error naming `missing` and `unknown`.

The consuming executable test assembly adds two assembly-level attributes and one derived fixture
that supplies the expected set. In the product implementation that expected set comes from the
Lead-hash-bound projection embedded as a verified resource, not from a literal — that binding is F1
work and is not part of this feasibility probe.

## What this probe does not yet establish

It does not establish the MSBuild pre-build hash comparison of the projection file, the
`RequireCompleteObligationSet` provenance property, the partition check across projections, or the
remaining section 12.1 mutants. Those are F1 obligations. This probe removes only the architectural
risk that the specified sentinel cannot be built under the specified package boundary at all.
