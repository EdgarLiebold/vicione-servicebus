# System.Text.Json serialization mutation validation

The checks used the Release output of `tests2/ViciOne.ServiceBus.Tests` and ran the complete project
through the native xUnit 4 / Microsoft Testing Platform 2 entry point. Each mutation was applied
alone and reverted before the next check.

| Mutation | Expected causal failure | Observed result |
| --- | --- | --- |
| Stop copying interface-property attributes in `DynamicImplementationBuilder` | `InterfacePropertyConverter_IsHonoredByTheGeneratedImplementation` | 330 total; exactly this test failed because the value was `10` instead of `-1` |
| Let `SystemTextJsonConverterFactory` classify `IReadOnlyList<KeyValuePair<string, object>>` implementations as dictionaries | `DuplicateKeyPairs_RemainAnOrderedListWithoutKeyCollapse` | 330 total; exactly this test failed when the duplicate `Frank` key reached the dictionary converter |
| Remove the hostile `$type` property from the type-safety fixture | `PayloadTypeMarker_CannotChooseTheInstantiatedType` | 330 total; exactly this test failed because the required marker was absent |

After the mutations, both product files and the test fixture were restored exactly. The final
unmutated Release build and unfiltered profile are the acceptance verdict, not these deliberate
failures.
