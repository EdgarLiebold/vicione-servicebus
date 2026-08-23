# System.Text.Json Collection Compatibility Mutation Validation

Date: 2026-08-23

Each mutation changed exactly one deserialized behavior or requirement-projection fact in the
canonical candidate, rebuilt the native Core project in Release with zero warnings and zero errors,
ran only the owning test, and was then reverted with an empty `src/**` diff. The complete unfiltered
profiles ran only after every mutant had been removed.

| Mutation | Owning verdict | Result |
|---|---|---|
| Remove the second nested-list element at the real serializer-context boundary | `NestedObjectList_RoundTripsEveryValueInOrder` | Exit 2; exact count failure, expected 2 and observed 1. |
| Replace the nested value of dictionary key `key-1` at the real serializer-context boundary | `Dictionary_RoundTripsEmptySingleAndMultipleEntries(2)` | Exit 2; exact value failure, `value-1` versus `__mutated__`. |
| Remove `body` before deserializing the constructor-bound private-setter contract | `ControlCharacterAndPrivateSetter_RoundTripExactly` | Exit 2; exact value failure, control string versus `null`. |
| Reverse the generic object array at the real serializer-context boundary | `GenericObjectArray_RoundTripsEveryValueInOrder` | Exit 2; exact ordered-collection failure at position 0. |
| Remove the `nested-object` row from `CoreRequirements.json` | `CoreRequirements_MatchCompiledRequirementMetadata` | Exit 2; the compiled fact is rejected as unprojected. |

Every behavior mutant compiled before its deliberate test failure. No mutant survived, no skip or
minimum-count proxy replaced the named assertion, and the final working tree contains none of the
mutation code.

Verdict: **PASS**.
