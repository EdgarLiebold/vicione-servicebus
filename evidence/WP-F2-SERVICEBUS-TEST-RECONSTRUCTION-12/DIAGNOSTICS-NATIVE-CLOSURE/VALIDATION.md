# Diagnostics native closure validation

Technical subject: `ae2ae679e3b1d157d10e0b40cdc733eca9bd53f0`, tree
`233eb7777ea001738bb921c2d4e8935abb8b5cfd`. The technical range begins at the previously accepted
remote head `8764bcd81b11a6b25061a69cfdd23d5babd08d0f`.

The inherited NUnit/VSTest Diagnostics project is completely retired. Its 44 executable identities,
`OBL-R0-SML-0222..0265`, have 44 unique native xUnit 4/MTP v2 owners. A 45th Fact verifies that the
passive JSON requirement projection exactly matches compiled `RequirementCoverage` metadata. The old
three C# files, project file, lock file and now-empty `tests/Tools` directory are absent.

Positive verification:

- focused Diagnostics: 45 passed, 0 failed, 0 skipped;
- complete UnitArchitecture: 2,335 passed, 0 failed, 0 skipped across 20 CTRF files, above the active
  floor of 2,333;
- complete Engineering Release build: exit 0, 0 warnings, 0 errors;
- verification model: PASS;
- CI tool self-tests: 257 passed;
- assertion and anti-pattern review: every Fact has a behavioral assertion; no Skip, wall-clock sleep,
  shared fixture, static mutable test state or real-time absence oracle is present. Fake-time expiry
  carriers assert completion immediately after the clock advance, so an ignored injected clock fails
  rather than waiting for real time.

No LocalIntegration runtime was started: the slice changes no provider, broker, fixture or
LocalIntegration test path. The Engineering build nevertheless compiles the full LocalIntegration
project graph. The two real broker measurement scenarios remain deliberately manual diagnostics and
are not reported as acceptance gates.

M01-M05 independently weaken clock ownership, snapshot ordering, exactness, fallback delivery and
quiescence interpretation. Every mutant builds successfully and makes only its named focused carrier
red; all five finish in less than one second. Every target file is restored to its exact baseline
SHA-256 before the final positive run.
