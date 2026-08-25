# Message identity, conversation, time, and header mutation validation

Each accepted probe changed one externally observable product rule, rebuilt the affected Release
project without restore, ran only the named native xUnit/MTP test, and was required to fail. Each
probe was reverted with an explicit inverse patch. Product-source hashes were checked after the last
reversion. Two non-killing exploratory changes exposed intentional overlapping implementation paths;
they are described in the product-path analysis and are not counted below.

| Probe | Single mutation | Detecting test | Result |
|---|---|---|---|
| M1 | put `CommandId` ahead of `EventId` | `NamedGuidProperties_FollowCorrelationThenEventThenCommandPrecedence` | FAIL: CommandId replaced EventId |
| M2 | execute the caller pipe before topology | `ExplicitSendContextValue_IsAppliedAfterTheMessageConvention` | FAIL: convention replaced caller value |
| M3 | remove root conversation generation | `RootSendAndPublish_CreateIndependentNonEmptyConversationIds` | FAIL: conversation was null |
| M4 | remove conversation inheritance | `MessageProducedInsideAConsumer_InheritsConversationAndEndpointSource` | FAIL: a new id replaced the inherited id |
| M5 | omit the initiating-conversation header | `StartNewConversationInsideAConsumer_RecordsThePreviousConversation` | FAIL: previous id was null |
| M6 | clear final outgoing source metadata | `MessageProducedInsideAConsumer_InheritsConversationAndEndpointSource` | FAIL: source address was null |
| M7 | add one tick to derived `SentTime` | `ConsumedSentTime_IsTheExactUtcTimestampOfItsMessageId` | FAIL: exact timestamp differed |
| M8 | alias instead of copying independent headers | `CopyingConstructor_CreatesAnIndependentCaseInsensitiveSnapshot` | FAIL: lookup/copy isolation broke |
| M9 | bypass reference-header deserialization | `ObjectHeader_RoundTripsEveryInterfaceMemberWithoutAliasingTheSenderObject` | FAIL: header object was null |
| M10 | append instead of prepend an explicit topology selector | `ExplicitMessageTopologySelector_PrecedesEveryBuiltInConvention` | FAIL: built-in selector won |
| M11 | disable the caller-options interface fallback | `JsonElement_GetObjectMaterializesAPublicInterfaceWithCallerOwnedOptions` | FAIL: interface deserialization was unsupported |

Final product-source restoration hashes:

```text
20260a3aa50c38c5580b8adfddfa32b6a0272fa067e4af7bd938c047b4de89bd  src/ViciOne.ServiceBus/Topology/Configuration/CorrelationIdMessageSendTopologyConvention.cs
a9b13262f83b8e743de42919bb021ad339541edb084b1b30c12f8d56a92590e1  src/ViciOne.ServiceBus/Middleware/SetCorrelationIdFilter.cs
864ae56501931fa7a516a0e1f23d42ae5b7239d09d4eeaa000e9e77302e43288  src/ViciOne.ServiceBus/Transports/SendEndpoint.cs
bc59cdf6eccd739c27d9a09fa811f252c6796e0b62722fca34d26be130c3dc2f  src/ViciOne.ServiceBus.Abstractions/Contexts/SendContextExtensions.cs
27ebcf694260c872896b9dd6609518125d4c5be051d2fb09177bdf4886ab6f21  src/ViciOne.ServiceBus/Contexts/Context/MessageSendContext.cs
7699404df81587ec4772171790419ad128361bdde848f3fdd51c40cc4aa7e3a0  src/ViciOne.ServiceBus/Serialization/DictionarySendHeaders.cs
096fbd3cf824765c5d8bc43249f4c290181ce028fb3db8b5471bd74bd424c469  src/ViciOne.ServiceBus/Serialization/ReadOnlyDictionaryHeaders.cs
b4db5553ce4d8d00c4ea69253111703052798728acd7ae324e916ba17d1d2ef8  src/ViciOne.ServiceBus/Serialization/SystemTextJsonExtensions.cs
```
