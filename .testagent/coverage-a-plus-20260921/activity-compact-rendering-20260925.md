# Test-harness compact activity rendering

Code and test commit: `a82d69c66`.

`ActivityListener_CompactOutputOmitsDetailColumnButRetainsTheTraceAsync`
starts a real child activity, closes the listener, and checks the rendered
header, exactly one root and child row, the child tree marker, and the
absence of the detail column and hidden processor value. The test uses
`await using` so a failed assertion cannot leave the global activity
listener registered. The requirement registry includes its diagnostics
variant.

`TestActivityListener.GenerateOutput` now delegates chart construction to
`BuildChart` and only selects and writes the output table. This preserves
the rendered output while reducing the measured method complexity. The
read-only adversarial review initially found that the test could accept a
flattened tree. After requiring the child marker and unique rows, the final
review found no P1/P2 issue. A deliberate product mutation that placed the
child at root depth failed the new test on the missing `└` marker. The
source was restored to SHA-256
`b1779ad24d73644e05c8dbefb0f3dbd86be26d9b0f36a408a8fdac714e0b9273`,
rebuilt, and the targeted test passed.

The full Core Microsoft Testing Platform coverage run at `a82d69c66`
passed 6,439/6,439 with no skips. Report:
`artifacts/coverage-activity-compact-a82d69c66/coverage.cobertura.xml`,
SHA-256 `74377c5cf816f8f186b5991c92d97ec7e3cb55c79512c3f1186adefe1ac66451`.
The Testing assembly has 94.6837% line and 79.3103% branch coverage in this
report. `GenerateOutput` has 10/10 lines, 83.33% branches, and CRAP 6;
`BuildChart` has 17/20 lines, 88.46% branches, and CRAP 28.28. The prior
combined `GenerateOutput` measured CRAP 33.26.

The global A+ goal remains open. This Core report uses cached dependencies;
it is not a fresh isolated or whole-product provider coverage profile.
