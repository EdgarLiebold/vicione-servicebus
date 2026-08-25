# Message contexts and dynamic contracts mutation validation

Each probe changed one observable product rule, rebuilt the affected Release project without restore,
ran only the owning native xUnit/MTP class, and was required to fail. Every probe was reverted by an
explicit inverse patch. Final product hashes prove full restoration.

| Probe | Single mutation | Detecting test | Result |
|---|---|---|---|
| M1 | permit an open generic proxy contract | `UnsupportedContractShape_IsRejectedBeforeTypeEmission` | FAIL: the required `interfaceType` rejection disappeared |
| M2 | bypass property-only method/event validation | `UnsupportedContractShape_IsRejectedBeforeTypeEmission` | FAIL: invalid types leaked as `InvalidOperationException`/`TypeLoadException` |
| M3 | group property names case-sensitively | `UnsupportedContractShape_IsRejectedBeforeTypeEmission` | FAIL: the ambiguous case pair was accepted |
| M4 | pass named attribute arrays without typed conversion | `InheritedProperties_PreserveValuesInitMetadataAndCompleteAttributes` | FAIL: proxy emission threw `InvalidCastException` |
| M5 | restore the CLI serializable bit | `ValidInterface_ProducesOnePublicSealedCollectibleImplementation` | FAIL: emitted metadata carried the forbidden bit |
| M6 | replace caller cancellation with an anonymous canceled task | `CanceledRequest_ReportsCancellationInsteadOfTimeout` | FAIL: cancellation token identity differed |
| M7 | emit an empty request accept-type header | `AddressedRequestAndResponse_PreserveCausationAddressesAndAcceptedTypes` | FAIL: accepted response URN was absent |
| M8 | omit inherited interface contracts from emission | `DynamicImplementationBuilderTests` | FAIL: inherited accessors and conflict validation were lost |

The inverse patches first restored `DynamicImplementationBuilder.cs` to
`898ff2efa5fb6b7a0d5d1bee466108979a7db63e66d3de60da069c6d399a5dca` and
`ClientRequestHandle.cs` to `a65062fb19d5e818b13d0b7fbda39590ec7689854d6b7b7f6873e43866480067`.
The subsequent bounded cleanup normalized comments/indentation and replaced the remaining real-time
request timer with the shared `ClientFactoryContext.TimeProvider` boundary. Final product hashes are:

```text
06327cc9e0641de52fcc84b059a6f8a45dba712aef357eb44f98c9b0aafcfd2e  src/ViciOne.ServiceBus/Internals/Reflection/DynamicImplementationBuilder.cs
ba71a6ab8319463403254d8049cce49c313d5b041ad094a4e11314fe809e85a0  src/ViciOne.ServiceBus/Clients/ClientRequestHandle.cs
5b37569bcca1ba068c37822819f6dc1c5498efebd53a443884c7a4fac2849d41  src/ViciOne.ServiceBus.Abstractions/Clients/ClientFactoryContext.cs
f698c97368780131c9e10fe7a900f7388c828bb5ecaa09e4e244ce5c6055ca05  src/ViciOne.ServiceBus/Clients/BusClientFactoryContext.cs
12b98abd00126c84e816707089a61d7afea8fe35182ed119169723524fa80831  src/ViciOne.ServiceBus/Clients/ReceiveEndpointClientFactoryContext.cs
3b003bc229917cfe0a07fe12b6ae6a1ecd01a86a6880ffb6c2ab08a33802ed56  src/ViciOne.ServiceBus/Clients/HostReceiveEndpointClientFactoryContext.cs
d70af8d0eaa989420470fe1be334a07ad29dc05ce5d677af429f8140e3c3f2de  src/ViciOne.ServiceBus/Mediator/Contexts/MediatorClientFactoryContext.cs
cd882deacd6572477d802857973f6d25b76832ada58108eb2b41f3918b1ab1f6  src/ViciOne.ServiceBus/DependencyInjection/DependencyInjection/ScopedClientFactoryContext.cs
```
