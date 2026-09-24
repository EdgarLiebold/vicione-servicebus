# Public ref-return property access

The exact product-wide profile at `151bf0dc1` measured
`PropertyAccessorFactory.IsCompilationFailure` at CRAP 72: complexity 8,
0/1 covered lines. A public class property returning `ref int` is a real
reflection boundary: compiled object-valued expression access cannot box
its by-reference result, while reflection can return its current value.

`ObjectValuedAccessors_ReadTheCurrentValueOfARefReturnProperty` exercises
both public object-valued accessors, `ReadOnlyProperty` and
`ReadOnlyProperty<RefReturnTarget>`. Each returns 42 initially and 73 after
the referenced field is changed through the property. These four exact
assertions protect both independent compilation-failure fallbacks and prove
that neither accessor caches the first value. No production code changed.

The focused Microsoft CodeCoverage report is
`artifacts/coverage-property-accessor-ref-return-20260924/raw/final/coverage.cobertura.xml`,
SHA-256 `81e8e9821edd613618c7d05aa5adf2e7a0e3a18d7d59c5b8e08e82d78ca5c070`.
It measured `IsCompilationFailure` at 1/1 lines and CRAP 8. The catch paths
in `CreateUntypedGetter` and `CreateGetter<T>` were both executed. This is a
focused Abstractions result, not a new product-wide profile.

The final Abstractions suite passed 786/786, and the Release Unit/Architecture
gate passed 10,257/10,257 with no failures or skips. The Release solution
build had zero warnings and errors. The test has one `RequirementCoverage`
attribute and a matching `AbstractionsRequirements.json` row. A read-only
adversarial review first found that only the untyped accessor was asserted;
after the typed-target accessor was added, the review returned PASS with no
remaining concrete finding.

The last complete product-wide profile remains the one at `151bf0dc1`:
89.6626% line, 81.8534% conservative branch observation, and 52 methods
with CRAP > 30. Global A+ remains open until a fresh all-assembly profile
of the current test bytes is collected and the remaining gaps are resolved.
