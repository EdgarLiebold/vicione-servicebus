# Iteration 99 — Initializers Owner Validation

Date: 2026-09-13

Baseline: `4d6c26ba3666972b7eeda9fc45122433e5d47bac`

Scope: complete `ViciOne.ServiceBus.Initializers` production owner, its direct tests, architecture
rules, package contract, and consumer closure

## Manual owner review

All eight C# production files and their comments were read against their implementations before any
source or documentation change. No source/comment generator was used. The six advanced facade files
already express and test their forwarding, pipe, timeout, cancellation, required-input, and
unsupported-capability contracts accurately.

The owner remains an independent capability project beside Core. `src/ViciOne.ServiceBus` is the
Core project directory, not an umbrella for sibling assemblies. Within Initializers, an explicit
`ViciOne.ServiceBus` root namespace makes the `Advanced` and `Initializers` folders mirror the two
public namespace branches. This preserves assembly ownership while improving navigation.

## Remediation

- Replaced the private, one-implementation `IdContext` and `TimestampContext` interfaces with
  sealed nested context types while preserving distinct `GetOrAddPayload` cache identities.
- Corrected the copied `timestampContext` local name in `IdVariable`.
- Added `TimestampVariable(TimeProvider)` with required-input validation; the parameterless
  constructor delegates to `TimeProvider.System`.
- Moved both variable files beneath `Initializers/Variables`, matching
  `ViciOne.ServiceBus.Initializers.Variables` relative to the project root namespace.
- Added three exact requirement-mapped tests for system UTC capture, deterministic supplied-time
  behavior and missing-clock rejection, and first-value sharing for both explicit variable kinds.
- Added red-first architecture requirements for .NET interface prefixes and Initializers
  namespace/folder alignment.

## Test quality and mutation evidence

The static source/test pairing heuristic reports all eight files as paired, but was used only as a
discovery aid. Twenty direct tests were manually checked for assertion quality and async execution;
none is assertion-free, trivial-only, self-referential, blocking, skipped, fixed-delay based, or
dependent on mutable shared fixture state. The one intentional system-clock assertion uses a
bounded interval to verify the convenience constructor; exact clock semantics use a deterministic
`TimeProvider`.

One default-timestamp counterchange survived the pre-existing suite and was treated as a real gap,
not hidden. After the clock tests were added, four of four isolated substantive counterchanges were
killed and restored:

1. bypass ID payload-cache reuse;
2. bypass timestamp payload-cache reuse;
3. return the default timestamp from the system-time constructor;
4. ignore the supplied `TimeProvider`.

The naming rule failed red with exactly `IdContext` and `TimestampContext`. The navigation rule
failed red with the two old variable paths and evaluated root namespace. Both pass after remediation.

## Final validation

- Engineering Release build: 77 projects, 0 warnings, 0 errors.
- Complete serial Unit/Architecture solution: 6,239 passed, 0 failed, 0 skipped.
- Architecture host: 295 passed, 0 failed, 0 skipped.
- Core host: 3,278 passed, 0 failed, 0 skipped.
- Fresh full-host coverage: 75.3322% line, 68.0491% branch.
- Initializers package coverage: 100% line, 100% branch, complexity 13.
- Coverage artifact: `/private/tmp/vsb-iteration99-initializers-final.cobertura.xml`, SHA-256
  `039bbdeeb0bed44ea4b49d9ea5310edb136b0e0352b57552b26b310aeed2dd8f`.
- Complete Engineering and Unit/Architecture format gates: passed with exit code 0.
- Initializers info-level style gate: passed with no information, warning, or error diagnostic.
- Developer journeys: 18 scenarios, 31 fresh packages, 3 isolated provider-testing consumers,
  30 runtime package APIs.
- Packed public API: 19,030 lines, SHA-256
  `a31b98d00ab15941a47bef08aa05447a24db4e445aba00d0838a0686ca85aa0a`.
- Requirements JSON, Git whitespace, preprocessor, dummy-marker, TODO/HACK, temporary-marker,
  `NotImplementedException`, interface-name, and empty-directory checks: passed.

The intentional packed API delta is the new `TimestampVariable(TimeProvider)` constructor. The first
strict journey run rejected that uncommitted contract difference; the explicit update run rebuilt
and re-executed the entire gate successfully.

The protected `review/` and `TestResults/` trees were not modified or staged. This is an author
validation, not an independent Red Team acceptance. The complete repository A+ goal remains active
for the remaining source owners and final audit.
