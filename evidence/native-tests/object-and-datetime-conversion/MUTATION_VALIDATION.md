# Object and date-time conversion mutation validation

Each mutation was applied alone to the Release product, followed by the complete native core test
project, and reverted before the next mutation.

| Mutation | Observed causal result |
| --- | --- |
| Change `DateTimeOffsetTypeConverter` output from round-trip (`O`) to general (`G`) format | 335 total; only `DateTimeOffsetMinimum_RoundTripsThroughTheInvariantTextForm` failed |
| Remove the concrete fallback from `JsonElement.GetObject<T>` | 335 total; the direct `GetObject` and dependent `Transform` facts failed |
| Make `Transform<T>` serialize an empty object instead of its source | 335 total; only `Dictionary_TransformCreatesTheExactRequestedShape` failed |

The unmutated project also passed 335/335 under both the normal process culture and
`ar_SA.UTF-8`, confirming the tested invariant round-trip strings do not depend on the device
culture. Product files were restored exactly before the final acceptance run.
