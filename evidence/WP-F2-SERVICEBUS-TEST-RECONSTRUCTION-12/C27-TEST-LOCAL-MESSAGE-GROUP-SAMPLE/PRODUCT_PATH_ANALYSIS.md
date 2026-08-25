# Test-local message-group sample — disposition analysis

## Finding

`tests/ViciOne.ServiceBus.Tests/Groups/Group_Specs.cs` contains its entire subject inside the test
file: `OIUWERO.CombineWith`, `CorrelatedMessageGroup<TKey>`, `IMessageGroup`, `AddOrderItem`, and
`CreateOrder`. None of the three tests invokes a type or operation from a retained
ViciOne.ServiceBus product project.

The similarly named product `GroupKeyProvider` types configure batch grouping and are unrelated to
this test-local collection example. Recreating the old cases under `tests2` would test newly copied
test code, not preserve a product capability.

## Disposition

All three R0 obligations are `NON_PRODUCT_TEST_LOCAL_SAMPLE`. No replacement test and no product
change are required. The UnitArchitecture floor remains 1350.
