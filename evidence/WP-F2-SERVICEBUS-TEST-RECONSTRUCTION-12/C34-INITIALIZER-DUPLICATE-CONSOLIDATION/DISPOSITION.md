# C34 disposition — duplicate initializer preservation path

## Result

`OBL-R0-CORE-D-0415` is terminally replaced by two already accepted native tests:

1. `MessageInitializerScalarConversionTests.MatchingScalarTypes_AreCopiedWithoutConversion`
   proves exact string and `DateTime` copying, plus every other supported scalar and object identity,
   through `MessageInitializerCache<T>.Initialize`.
2. `DynamicContractIntegrationTests.AnonymousValues_SendAsAnInterfaceWithEveryValueAndContextIntact`
   proves anonymous-object materialization of a dynamic interface contract including `Guid` and
   string values, nested values and the real in-memory send/receive boundary.

Together they strictly contain the inherited test's Guid/string/DateTime interface-initialization
contract. Both have exact passive requirement carriers, source-mirrored owners and successful
one-cause evidence from their accepted cohorts. They passed again in the final 1466/1466 unfiltered
UnitArchitecture run for C33.

No product or native-test code is changed, no additional test is justified, and the profile floor
remains 1466. `tests/ViciOne.ServiceBus.Tests/SendProxy_Specs.cs` is removed only after this exact
mapping is established; its original bytes remain recoverable through Git.
