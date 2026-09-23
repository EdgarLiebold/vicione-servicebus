# Delegate-based consume transform contract

## Product contract and test change

The public `TransformSpecification<TMessage>.Set(propertyExpression, valueProvider)`
overload supplies the delegate with the original message, the evaluated source
property value, and `HasValue`. The existing class-based pipeline test only
used constant `Set` values, leaving this overload unverified through an actual
consume pipeline.

`ClassBasedTransform_ChangesEveryConfiguredPropertyAsync` now sends two
messages through the in-memory receive endpoint. One has `Second = "Previous"`;
the other has `Second = null`. The transform sets `First` to `"First"` and
computes `Second` from the delegate's original `Input.First`, `Value`, and
`HasValue`. It asserts respectively `"Hello:Previous"` and `"Hello:<null>"`
beside the `"First"` result. The null case distinguishes `HasValue == true`
from `Value != null`, as the public context contract requires. The first
result also distinguishes the original `Input.First` from the already changed
target property. No product code was altered.

The adversarial read-only review first found that the original single-row
version did not check `HasValue == true` when `Value` is null. The null row was
added; the final review returned PASS with no concrete blocker. Neither
review edited files or ran tests. The existing requirement-projection method
name was retained; a temporary rename caused the projection gate to fail and
was corrected before the final clean run.

The Microsoft `code-testing-agent` skill was applied before changing the
test. `test-gap-analysis` and `coverage-analysis` guided the risk selection,
and `run-tests` supplied the xUnit-v3/MTP filter syntax. Assertions were
reviewed against the source and the read-only Red Team's concrete mutant.

## Final validation

The following counts and build result were observed in interactive command
output; this phase did not archive those console logs. The coverage profile
records its separately saved reports and provenance limits.

- Focused transform class: 11/11 passed, zero failures or skips.
- Core Unit project: 6,373/6,373 passed, zero failures or skips.
- Exact-commit Unit/Architecture solution gate: 10,219/10,219 passed, zero
  failures or skips. The complete Release solution build succeeded with zero
  warnings and zero errors.
- Source/test commit: `5da15ce2e65690e81533f12ec64833b5d9534560`.

## Measured method effect

| `DelegatePropertyProvider.GetPropertyAsync` | Previous profile `175017360` | Current profile `5da15ce2e` |
| --- | ---: | ---: |
| Measured lines | 0/8 | 5/8 |
| Complexity | 6 | 6 |
| CRAP | 42.00 | 7.90 |

The full 36-report profile is recorded in
`product-wide-profile-5da15ce2e.md`. This phase tests an existing public
product contract rather than adding a coverage-only call.
