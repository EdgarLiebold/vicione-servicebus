# R0-CORE-C — findings

Read-only census of 115 test files and the owning product code, at baseline commit
`ae73c6da748e3bc3257dffa4971ee8680e086207`. Every finding names its file and, where the exact text
matters, its line. Nothing below was changed.

---

## 1. Serialization (highest-risk area of this cohort)

### 1.1 Formats and content types the product actually carries

Measured from `src/ViciOne.ServiceBus/Serialization/` and `src/ViciOne.ServiceBus.MessagePack/`:

| Serializer | Media type | Deserializer | Covered by this cohort |
|---|---|---|---|
| `SystemTextJsonMessageSerializer` (envelope) | `application/vnd.vicione.servicebus+json` | yes | yes, extensively |
| `SystemTextJsonRawMessageSerializer` (raw) | `application/json` | yes | yes, 4 fixtures |
| `MessagePackMessageSerializer` | `application/vnd.vicione.servicebus+msgpack` | yes | yes, 84 identities |
| `CopyBodySerializer` | caller-supplied | n/a (send only) | once, in `DeserializerFault_Specs` |
| `SystemTextJsonBodyMessageSerializer` | inherits | n/a | indirectly, via forward/schedule |

There is **no XML and no Protobuf product serializer**. `XmlPayload_Specs.cs` states that
deliberately and asserts the negative: `Serializer.ContentType.MediaType` must not contain `xml`.

### 1.2 Section 11 requirement versus what is covered today

Lead plan section 11: *"Serializer-Sicherheits- und Kompatibilitaetstests enthalten die vereinbarten
typischen Formate, darunter generiertes Protobuf als JSON sowie XML/Raw XML, ohne einen entfernten
Produktserializer wieder einzufuehren."*

**Covered.**

* *Generated Protobuf as JSON* — `Serialization/ProtoBufAsJson_Specs.cs`, one identity
  (`Serializing_a_protocol_buffer_message(SystemTextJsonMessageSerializer).Should_return_the_array_values`).
  It round-trips `TradesBookedViciOneServiceBus` through the envelope serializer and asserts the
  repeated field has two elements. `TradesBookedViciOneServiceBus.Populate.cs` supplies the
  `[JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]` that makes this possible at all.
* *XML* — `Serialization/XmlPayload_Specs.cs`, six identities (3 cases x 2 serializers). An XML
  document with a non-ASCII entity, an attribute and a CDATA section survives as a string character
  for character and as bytes byte for byte, under both retained serializers, and the message never
  advertises an XML media type.

**Gaps.**

1. **Protobuf-as-JSON is one assertion deep.** Only a repeated-field *count* is asserted. Nothing
   covers a scalar field, the `map<string, string>` (`additional_data`), the well-known
   `google.protobuf.Timestamp`, an unknown-field body, or Protobuf-as-JSON under the **raw** JSON
   serializer. The generated type declares 24 fields; one of them is exercised.
2. **"Raw XML" has no case in the section-11 sense.** If the requirement means *an XML document as
   the message body under a raw serializer*, nothing covers it — and `XmlPayload_Specs` proves the
   opposite by design. If it means *XML as a payload value*, `XmlPayload_Specs` already satisfies it.
   The reconstruction cannot choose interpretation (a) without touching the removed-serializer
   boundary that section 2 Nr. 3 protects. **This needs a Lead answer before the serializer cohort is
   rebuilt** (`OBL-R0-CORE-C-0473`).
3. **Descriptor drift in the generated Protobuf sources.** Both `.proto.cs` files are labelled
   `DO NOT EDIT` but were renamed by hand. Decoding the embedded `FileDescriptor` base64 gives:
   * `TradeBookedViciOneServiceBus.proto.cs` -> file `Events/TradeBookedMT.proto`, package
     `Messages.Events`, message `TradeBookedMT`, nested
     `Messages.Events.TradeBookedMT.AdditionalDataEntry`;
   * `TradesBookedViciOneServiceBus.proto.cs` -> file `Events/TradesBookedMT.proto`, package
     `Messages.Events`, message `TradesBookedMT`.

   The checked-in `.proto` files declare `package ViciOne.ServiceBus.Tests.Serialization` and the
   ViciOne message names, and `TradesBookedViciOneServiceBus.proto` imports
   `Events/TradeBookedViciOneServiceBus.proto`, a path that does not exist in the repository. **The
   generated C# cannot be reproduced from the checked-in schema.** Anything the rebuild asserts about
   the descriptor, the JSON field names or reflection over these types is asserting the *old* names.

### 1.3 Envelope, versioning and round-tripping

* `JsonMessageEnvelope` (JSON) and `MessagePackEnvelope` (MessagePack) both carry `MessageId`,
  `RequestId`, `CorrelationId`, `ConversationId`, `InitiatorId`, the four addresses, `MessageType[]`,
  `ExpirationTime`, `SentTime`, `Headers` and `Host`. **Neither carries a version field**, and no
  case in this cohort asserts forward or backward compatibility of the envelope shape. There is no
  envelope-version obligation to reconstruct because there is none today — recorded as a product gap
  rather than a lost test purpose.
* The single most load-bearing round-trip statement in the cohort is **not** in a test method: it is
  in `Serialization/SerializationTest.cs`, `Return<T>`, which asserts on **every** round trip that
  `SourceAddress`, `DestinationAddress`, `FaultAddress`, `ResponseAddress` and `RequestId` survived.
  All 236 `Serialization` identities inherit it silently. In the rebuild this must be an explicit,
  named obligation, or 236 cases quietly stop proving envelope addressing.
* `MessagePackEnvelopeClone_Specs.cs` holds the clone act: a MessagePack payload is carried over
  rather than re-encoded, copied rather than aliased, the overlay flag is preserved, a foreign
  envelope is still encoded, and headers are copied. `Redelivery_Specs.cs` holds the end-to-end
  consequence. Both are needed — the end-to-end case alone would not localise the defect, and the
  unit case alone would not prove redelivery works.

### 1.4 Header and property preservation

* `Forward_Specs.cs` (two fixtures) — forwarding preserves headers, the full concrete body and the
  envelope content type.
* `ExtensionData_Specs.cs` — `[JsonExtensionData]` keys survive publish/consume under envelope and
  raw JSON.
* `MisnamedProperty_Specs.cs` — constructor-parameter/property name mismatch.
* `Redelivery_Specs.cs` — `SupportedMessageTypes` present on both the faulted and the consumed
  context.
* Not covered: header **case-insensitivity** on the wire. `DictionarySendHeaders`,
  `JsonMessageEnvelope.Headers` and `MessagePackEnvelope.Headers` are all built with
  `StringComparer.OrdinalIgnoreCase`; nothing asserts it.

### 1.5 Polymorphic and interface message types

* `PolymorphicProperty_Specs.cs` — three fixtures covering a property, an array and an `IList` of an
  abstract base, using declared `[JsonDerivedType]` polymorphism.
* `Interface_Specs.cs` — proxy generation including `init`-only and settable properties, plus
  dispatch through the consume pipe.
* `Attribute_Specs.cs` — a `JsonConverter` declared on an interface property reaches the proxy; a
  nullable reference collection deserializes non-null.
* `InterfaceFormatterCaching_Specs.cs` — the MessagePack interface formatter writes through the
  formatter of the **runtime** type, compiles once per concrete type, and stays correct under 64-way
  parallel round trips.
* Note the interaction: `SystemTextJsonConverterFactory.IsConvertibleInterfaceType` explicitly
  **refuses** to build an interface proxy converter for a type carrying `JsonDerivedTypeAttribute` or
  `JsonPolymorphicAttribute`. No case asserts that exclusion. A rebuild that adds a
  `[JsonDerivedType]` to an interface message would silently change which converter is chosen.

### 1.6 Unknown and missing member handling

* `Array_Specs.Should_come_from_json_as_null` — a body member that is literally `null` must stay a
  null reference, not become an empty collection.
* `PropertyType_Specs.Should_handle_a_missing_nullable_value` — a missing `char?`.
* `IEnumerable_Specs.Should_not_convert_to_a_dictionary` — a list of key/value pairs with duplicate
  keys must not be collapsed.
* `DeserializerFault_Specs` — a body whose member *types* do not match (a `bool` where an `int` is
  declared) faults the endpoint.
* Not covered: an **unknown** member (a property in the body the contract does not declare) and
  `AllowTrailingCommas` / `ReadCommentHandling.Skip`, all three of which the shared options enable.

### 1.7 Culture and time-zone sensitivity

* `DateTimeConverter_Specs` — `DateTime.MinValue`, `DateTimeOffset.MinValue` and
  `MinValue.ToUniversalTime()` round trip through the string form.
* `DateTimeFormat_Specs` — an ISO-8601 `Z` timestamp read into `DateTime` yields `DateTimeKind.Utc`.
* `GivenAComplexMessage.Crazy_date_time` — `DateTimeKind.Local` and a `Local`-derived UTC value.
* **Gap:** the product pins culture in exactly two places —
  `JsonConverters/StringDecimalJsonConverter` (read *and* write with `CultureInfo.InvariantCulture`)
  and `CaseInsensitiveDictionaryStringObjectJsonConverter.WriteValue` (decimal, invariant).
  **No case runs under a non-invariant `CurrentCulture`.** A decimal written under `de-DE` without
  the invariant pin would produce a comma separator and the round trip would silently change value.
  `When_serializing_decimals` proves the string form and the exact digits, but only under whatever
  culture the runner happens to have. This is the single largest *unproven* serializer invariant in
  the cohort.
* No case sets a non-UTC `TimeZoneInfo.Local` either.

### 1.8 Large payloads

* `MessageData_Specs.Should_work_with_System_Text_Json` publishes nine payloads from 1 000 to
  1 000 000 items (about 1.9 million objects in one case) with `MessageDataDefaults.Threshold = 1`.
* `MessageBodyLength_Specs` covers non-ASCII byte-vs-character counting and Base64 decoded length,
  but no case covers a body larger than a few hundred kilobytes at the **serializer** level.

### 1.9 Security-relevant deserialization guards

Four guards exist and all four are covered:

1. **No body-controlled type resolution.** `TypeHandling_Specs.Should_ignore_the_type_attribute`
   feeds `{"$type":"Command.TestCommand, TestDeserializationWithDummyClasses", ...}` and asserts both
   halves: the payload values survive **and** the created type's full name does not contain the
   assembly named in the body. The product has no `TypeNameHandling` equivalent; the requested
   contract decides.
2. **Hardened MessagePack reader.** `InternalMessagePackResolver.Options` uses
   `MessagePackSecurity.UntrustedData`. `MessagePackHardening_Specs.Hardening_the_message_pack_reader`
   asserts the mode and `HashCollisionResistant`, and that a string-keyed dictionary — the shape the
   overlay path reads — stays readable under it.
3. **Ownership of the hardened option set.** `Owning_the_message_pack_option_set` decodes the
   compiled IL of `ViciOne.ServiceBus.MessagePack` with the runtime opcode table and asserts that no
   method outside `InternalMessagePackResolver` carries a metadata token naming
   `MessagePack.MessagePackSerializer` — as a call, a method group (`ldftn`), a type token
   (`ldtoken`) or a field access. It has a control case that lifts the exclusion, so an empty result
   cannot mean the rule saw nothing.
4. **The rule that guards the rule.** `The_rule_that_guards_the_option_set` emits each bypass shape
   into a throwaway assembly with `PersistedAssemblyBuilder` and runs the rule against it — including
   a type with the owner's *simple* name in another namespace, which is exactly the shape a
   name-comparison rule would miss.

These four are the strongest constructs in the cohort and should be rebuilt as they are. Two carry a
migration risk: (3) and (4) are IL/metadata tests bound to `System.Reflection.Metadata` and
`PersistedAssemblyBuilder`; they are `UnitArchitecture`-profile and must not be weakened into a
string scan.

**Not guarded, not covered:** neither serializer bounds object-graph depth beyond the MessagePack
default of 500 (the product's own XML doc says so), and nothing bounds what a hostile payload can
allocate. That is stated honestly in the product comments and no test claims otherwise — recorded so
the rebuild does not invent an assurance that was never there.

---

## 2. `AsyncTestHarness` — the documentation that goes stale

`src/ViciOne.ServiceBus/Testing/AsyncTestHarness.cs` is the one file under `src/**` that Lead plan
section 2 Nr. 3 allows to be edited editorially. The reference in question is in the XML
documentation of `BeginTestScope()`, **lines 54-56**, and reads verbatim:

```
/// This type carries no test framework dependency. The framework lifecycle calls this once, in
/// ViciOne.ServiceBus.TestFramework.
```

Findings:

* This is the **only** occurrence of the string `TestFramework` anywhere in
  `src/ViciOne.ServiceBus/Testing/` (85 files). Verified with
  `grep -rn "TestFramework" src/ViciOne.ServiceBus/Testing/`.
* The sentence has two halves and only one of them goes stale. *"This type carries no test framework
  dependency"* stays true and is a statement about the product API; it must survive the edit
  unchanged. *"The framework lifecycle calls this once, in ViciOne.ServiceBus.TestFramework"* names a
  project that section 10 removes, so after the removal it points at nothing.
* The correction is editorial only. Signatures, behaviour and the XML-documentation *semantics* of
  the retained API stay unchanged, which is what section 2 Nr. 3 requires.
* **The functional consequence is larger than the sentence.** `BeginTestScope()` is what gives each
  test method its own `TestTimeout` budget; the doc explains why (one fixture instance is reused for
  every test method, so a shared budget lets the first tests spend the last ones' time). Today the
  only caller is the removed `ViciOne.ServiceBus.TestFramework` lifecycle. **If the rebuilt xUnit
  lifecycle does not call `BeginTestScope()` at the start of every test, every fixture in this cohort
  that relies on `TestTimeout` silently shares one budget**, and the failure mode is a late test
  failing for a reason it did not cause. This is a rebuild obligation, not just a doc edit.
* Second-order: `AsyncTestHarness` also documents `TestTimeout`/`TestInactivityTimeout` defaults
  chosen from `Debugger.IsAttached` (50/30 minutes attached, 30/6 seconds otherwise). Fixtures in
  this cohort override `TestTimeout` to 5 or 8 seconds and `TestInactivityTimeout` to 5 or 30
  seconds. Those overrides must be carried over per fixture; they are not decoration.

---

## 3. Courier — routing slip semantics as inherited

Coverage measured against `src/ViciOne.ServiceBus/Courier/` (23 files) and the `Courier/Contracts` /
`Courier/Messages` families in Abstractions.

| Semantic | Covered by |
|---|---|
| Itinerary, one activity | `SingleActivityEvent_Specs` (two fixtures, 12 identities) |
| Itinerary, two activities, per-activity events | `TwoActivityEvent_Specs` (7) |
| Empty itinerary completes immediately | `RanToCompletion_Specs`, `ReviseItinerary_Specs.Should_immediately_complete_an_empty_list` |
| Activity arguments override variables | `ArgumentOverload_Specs` (4 fixtures, 8) |
| Argument fallback: missing / explicit null / type default | `ArgumentOverload_Specs` |
| Activity log data on completed events | `SingleActivityEvent_Specs`, `TwoActivityEvent_Specs`, `FaultActivityEvent_Specs` |
| Compensation, reverse order, same tracking number | `FaultActivityEvent_Specs` (9) |
| Fault mid-slip, thrown vs returned | `Fault_Specs` (6) |
| Compensation that itself fails | `Fault_Specs`, `RetryActivity_Specs` |
| Retry then succeed / retry then compensate | `RetryActivity_Specs` (4) |
| Delayed redelivery of a faulting activity with variables | `ActivityRedelivery_Specs` (3), `FaultyRedelivered...`, `FaultyRetried...` |
| Variables: set, removed, null, faulted-with | `TwoActivityEvent_Specs`, `Fault_Specs`, `UriArgument_Specs` |
| Subscriptions: address, event flags, contents `None`/`All` | `Subscription_Specs` (5), `ItinerarySubscription_Specs` |
| Custom subscription message | `SendEvent_Specs` (2), `Subscription_Specs` (2 generic closings) |
| Revise itinerary: append, revise to empty, `RoutingSlipRevised` | `ReviseItinerary_Specs` (3), `ItinerarySubscription_Specs` |
| Completion events plus timestamp arithmetic | `SingleActivityEvent_Specs.Should_receive_the_routing_slip_timestamps` |
| Request/response over a slip, faulted, fault-response | `RequestRoutingSlip_Specs` (3) |
| Message data as an activity argument | `MessageDataArguments_Specs` (2) |
| Object graph / non-JSONPath keys / nullable enums | `ObjectGraph_Specs` (2) |
| Cyclic argument graph | `Builder_Specs.Should_not_crater_the_planet` |
| Partitioned activity host | `PartitionActivity_Specs` |
| `Uri` argument and variable | `UriArgument_Specs` (2) |
| Slip as an ordinary message | `RoutingSlip_Specs` |

**Not covered.** `ExecutionResult.Terminate()` and `TerminateExecutionResult` exist in the product
(`Courier/Results/TerminateExecutionResult.cs`, publishing `RoutingSlipTerminated`), and
`RoutingSlipTerminated` / `RoutingSlipTerminatedMessage` are registered in both the JSON converter
factory and the MessagePack resolver — **but no case in this cohort executes a terminate.** This is a
product-derived gap under section 3.1 ("Eine im alten Bestand fehlende Pruefung wird ergaenzt"), not
a lost inherited purpose. Likewise `CompletedActivityOptions.Delay` (the `ScheduleSend` branch of
`CompletedExecutionResult.Evaluate`) has no case.

---

## 4. MessageData — repository semantics

| Semantic | Covered by |
|---|---|
| In-memory repository put/get | `InMemory_Specs`, `DataBus_Specs` |
| File-system repository, expiration-derived folder | `FileSystem_Specs` (2) |
| Encrypted repository over the file system | `DataBus_Specs.Sending_a_large_message_through_the_file_system_encrypted` (3) |
| Threshold: inline below, stored above | `DataBus_Specs.Sending_inlined_message_data` (3) |
| Stream is never inlined | `DataBus_Specs.Should_not_inline_stream` |
| Address reuse on a response | `InitializerMessageData_Specs`, `InitializerClassWithMessageData_Specs` |
| Nested positions: property, array, list, dictionary | `DataBus_Specs.Receiving_nested_message_with_data` |
| `string`, `byte[]`, `Stream`, application object | all `MessageData/` fixtures plus `MessageDataOfT_Specs` |
| Missing required body faults the request | `A_missing_body_should_fault` (both initializer fixtures) |
| No filter added when no message data is present | `A_message_with_no_message_data` (empty body) |
| Serialization of a `MessageData<string>` property | `Serialization/MessageDataSerialization_Specs` |
| Courier argument as message data | `Courier/MessageDataArguments_Specs` |

**Not covered.**

* **Expiration is written but never read back.** `FileSystem_Specs.Should_generate_time_based_folder`
  passes `TimeSpan.FromDays(30)` and asserts only that the value loads. Nothing asserts the folder
  layout `yyyy/MM/dd/HH` that `FileSystemMessageDataRepository.GenerateFilePath` produces, and
  nothing asserts that `MessageDataDefaults.TimeToLive` or `ExtraTimeToLive` reach the repository —
  both are read in `PutMessageDataPropertyProvider.Put`.
* **`MessageDataNotFoundException`** (thrown by `InMemoryMessageDataRepository.Get`) and
  **`EmptyMessageData<T>`** (which throws `MessageDataException("The message data is empty")` from
  both `Address` and `Value`) have no case.
* **`DeserializedMessageData<T>.Value`** always throws `"The message data was not loaded"`. That is
  the placeholder state before the pipeline transform runs, and nothing asserts the transform
  actually replaces it — a pipeline regression would surface as this exception at an unrelated
  moment.
* **`EncryptedMessageDataRepository`** writes `parameters["keyId"] = ""` unconditionally in `Put`, so
  the address always carries an empty `keyId` and `Get` always falls back to the default key. The
  covered case passes because the test key provider has one key. A multi-key deployment is untested
  and, reading the code, would not work.

---

## 5. TLP-011 quality defects

Every item names the file. All are recorded on the affected ledger rows in `notes`.

### 5.1 Shared mutable static state

| File | State written | Restored |
|---|---|---|
| `MessageData/DataBus_Specs.cs` (`Sending_inlined_message_data`) | `MessageDataDefaults.AlwaysWriteToRepository = false`, `MessageDataDefaults.Threshold = 4096` | **no** |
| `MessageData/MessageData_Specs.cs` | `MessageDataDefaults.AlwaysWriteToRepository`, `.TimeToLive`, `.Threshold = 1` | **no** |
| `Topology/CreateTopology_Specs.cs` | `MessageCorrelation.UseCorrelationId<T>` -> `GlobalTopology.Send` | **no** |
| `Serialization/SerializationConfigurationValidation_Specs.cs` (`Configuring_the_shared_json_options`) | `SystemTextJsonMessageSerializer.Options` | yes, `[SetUp]`/`[TearDown]` |
| `Courier/DoubleActivity_Specs.cs` | `SystemTextJsonMessageSerializer.Options` (adds a converter) | yes, conditionally |

The first three are the dangerous ones: they change process-wide defaults for **every later test in
the run**, and they are the reason ordering matters today. `SystemTextJsonMessageSerializer.Options`
is a public **static mutable field**
(`src/ViciOne.ServiceBus/Serialization/SystemTextJsonMessageSerializer.cs`, line 2858) — every
fixture that touches it must own a non-parallel collection in the rebuild.

### 5.2 Fixed sleeps used as synchronization

* `ReliableMessaging/InboxLock_Specs.cs` — `Task.Delay(5)`, `Task.Delay(50)`, `Task.Delay(5)` between
  three publishes; the whole deduplication statement rests on those spacings.
* `Testing/ConsumerTest_Specs.cs` — `await Task.Delay(2000)` inside the "slow consumer".
* `Caching/Cache_Specs.cs` — `Task.Delay(1000)` in a factory plus a `Stopwatch` comparison with a
  0.1 second fudge factor.
* `Testing/AsyncMessageList_Specs.Should_complete_later` — `Task.Delay(1000)` in a detached task.
* `Courier/FaultyRedeliveredActivityWithVariable_Specs.cs` and
  `Courier/FaultyRetriedActivityWithVariable_Specs.cs` — `Intervals(1000)`.
* `Courier/ActivityRedelivery_Specs.cs` — `r.Interval(redeliveryCount, 100)` and an
  `await Task.Delay(1)` inside the activity, apparently to force an async boundary.

`InactivityObserver_Specs.cs` is the counter-example and the model to copy: it uses an interval that
*provably cannot elapse* while the case runs, so a completion can only come from the statement under
test, and a second interval short enough to elapse twice — but it waits on **events**, never on a
clock.

### 5.3 Unbounded and blocking waits

* `Courier/ReviseItinerary_Specs.Should_immediately_complete_an_empty_list` — the proof that the
  discarded activity did not run is `testActivityCompleted.Wait(TimeSpan.FromSeconds(3))` inside
  `Assert.Multiple`: a **blocking** wait, on the test thread, used as proof of absence.
* `Audit/Audit_Specs.cs` and `Audit/AuditFilter_Specs.cs` — the store is enumerated as
  `_store.Count(x => x.Result.Metadata...)`, blocking on `Task.Result` and rethrowing wrapped.
* `Transactions/*` — the "not yet delivered" halves use `_received.OrTimeout(s: 3)` and assert a
  `TimeoutException`. Bounded and diagnostic, but the bound is a fixed three seconds.
  `Using_transaction_scope_with_publish_and_complete` does it correctly instead, with its own
  `CancellationTokenSource(TimeSpan.FromSeconds(2))` passed into the harness call.

### 5.4 `async` without `await`, unawaited tasks, swallowed exceptions

* `Serialization/MisnamedProperty_Specs.cs` — `CommonRequestConsumer.Consume` calls
  `context.Respond(response)` and **discards the returned Task**, then returns `Task.CompletedTask`.
* `Testing/AsyncMessageList_Specs.Should_complete_later` — `Task.Run(async () => ...)` with
  `#pragma warning disable 4014`; the task is never observed.
* `Audit/Audit_Specs.cs`, `Audit/AuditFilter_Specs.cs`,
  `MessageData/*_Specs.A_missing_body_should_fault`,
  `MessageData/A_message_with_no_message_data.Should_not_add_a_filter`,
  `Transports/MessageFabric_Specs` (both) — `async Task` methods with no `await`.
* `Courier/RequestRoutingSlip_Specs.cs` — three `protected override async Task BuildRoutingSlip`
  overrides with no `await`.
* `Courier/FaultyRedelivered...`/`FaultyRetried...` — `configurator.Handler<T>(async context => { ... })`
  where the body only calls `TrySetResult`.
* `Testing/SagaTest_Specs.cs` — `async Task Consume(ConsumeContext<C>)` and
  `async Task Consume(ConsumeContext<B>)` with empty bodies.
* `AsyncInactivityObserver.TimeoutTask` (product) ends in `catch (Exception) { }` — a fully swallowed
  exception in the observer the harness depends on. Product code, not editable here; recorded because
  `InactivityObserver_Specs` is the fixture that would otherwise notice.

### 5.5 Tautological or assertion-free cases

100 of the 473 ledger rows are marked `QUESTION`; the majority are assertion-free success paths.
The clearest ones:

* `Courier/Configuration_Specs.Should_have_a_clean_interface` — **completely empty method body.**
* `MessageData/DataBus_Specs.A_message_with_no_message_data.Should_not_add_a_filter` — **empty
  `async Task` body.**
* `Courier/Builder_Specs.Should_properly_map_the_types` — calls `AddActivity`, asserts nothing.
* `Caching/Cache_Specs.Should_get_and_add`, `.Should_get_and_add_and_get`,
  `ValueTracker_Specs.Should_not_die` — the statement is "it does not throw".
* `Audit/Audit_Specs` and `AuditFilter_Specs` — `var expected = _harness.Consumed.Select<A>().Any();`
  and `var expectedB = ...` are computed and **never asserted**; only a count is checked. The filter
  fixture's real statement (that `B` was excluded from Send and `A` from Consume) is encoded only as
  `1` versus `2`, which would also pass if the wrong type were excluded.
* `Testing/ConsumerTest_Specs.When_a_slow_consumer_is_being_tested` — all four cases are
  `Assert.That(async () => await ..., Is.True)`, passing an async delegate where a value is expected.
* `Serialization/DeserializerFault_Specs.Should_fault`,
  `Courier/RequestRoutingSlip_Specs.Should_complete_the_request` and the fault-response variant,
  `Courier/RoutingSlip_Specs`, `MessageData/PublishMessageData_Specs` — the obligation is carried
  entirely by an awaited task completing (or by a handler that throws), with no `Assert`.

None of these is a deletion reason on its own (section 9). They are non-terminal until the Lead
disposes them, and the rebuild owes each of them a real assertion.

### 5.6 Assertions inside consumers

`MessageData/NestedInitializer_Specs.cs` puts eight `Assert.That` calls inside
`configurator.Handler<Documents>(async context => ...)`. A failed assertion there becomes a **consumer
fault**, not a test failure, and the test then reports the fault message instead of the assertion.
The success branch of its `if (_handled.Task.IsCompleted) { }` is an empty block.

### 5.7 Non-reproducible randomness

`Caching/Cache_Specs.cs` — `Should_handle_a_random_distribution` and
`Should_handle_a_random_distribution_with_time_to_live` draw 10 000 samples with
`MathNet.Numerics.Distributions.Zipf.Samples(_samples, 0.86, 500)` **with no seed**, then assert an
*exact* equality on a derived hit ratio. Section 11 requires reproducible or property-based
randomness with the seed printed on failure.

### 5.8 Order dependence

* Every fixture in 5.1 that does not restore its static state makes the run order matter.
* `Audit/*_Specs` observe an audit store written by an **asynchronous** observer while asserting an
  exact count synchronously after a single awaited response.
* `Serialization/MessageBodyLength_Specs` documents in its own source that comparing bodies built
  from two different `MessageSendContext` instances made a case fail at random, because each context
  stamps its own `MessageId` and `SentTime`. The current version builds one context per case. That
  reasoning must survive the rebuild.

### 5.9 Culture- or clock-dependent assertions

* No case sets a culture (see 1.7).
* `Courier/SendEvent_Specs` asserts `context.Message.Timestamp >= startTime` where both come from
  `DateTime.UtcNow` — clock-derived but monotonic within the process, acceptable.
* `Serialization/GivenAComplexMessage.Crazy_date_time` uses `DateTimeKind.Local` and
  `ToUniversalTime()`, so its result depends on the machine time zone. It passes everywhere today
  because the round trip is symmetric, but the case does not say which time zone it assumes.
* `Testing/StateMachineSagaTest_Specs` uses `DateTime.Now` (not `UtcNow`) for the fault timestamp it
  round-trips.
* `Caching/Cache_Specs.Should_get_and_add_and_get_same_time` is the one genuine wall-clock assertion:
  `timer.Elapsed + 0.1s >= 1s`.

---

## 6. Other constructs needing a Lead decision

1. **`Diagnostics/Trace_Specs.cs` is 100 % commented out.** It carries three named intents about a
   message-trace API that no longer exists, and the commented body still names the removed XML
   content type `application/vnd.vicione.servicebus+xml`. Zero identities. `OBL-R0-CORE-C-0455`.
2. **`MessageData/MessageDataOfT_Specs.cs`** — the source annotates its third dictionary assertion
   `// Will fail`, and the second `// Will pass`, yet all three are inside one `Assert.Multiple` and
   the identity is bound as **executing** in the anchor. One of the two is stale and the difference
   must be explained, not silently normalised.
3. **`MessageData/MessageData_Specs.cs`** publishes about 1.9 million objects in a single case and
   then inspects only the **first** consumed message. Eight of nine sizes are unasserted and the case
   is the most expensive in the cohort.
4. **`Diagnostics/StatsD_Specs.cs`** binds a real UDP socket on `127.0.0.1` with an ephemeral port.
   Machine-local and broker-free, but not resource-free — the profile matrix should say which bucket
   it belongs to.
5. **Cross-cohort dependencies.** `Caching/CacheRecovery_Specs.cs` uses
   `Middleware.Caching.TestValueObjects` (`SimpleValue`, `SimpleValueFactory`, `TestException`)
   declared in `tests/ViciOne.ServiceBus.Tests/Middleware/Caching/Tests.cs`, which is another agent's
   scope. `Messages/ClientMessage.cs`, `DeleteMessage.cs`, `UpdateMessage.cs` and
   `UpdateAcceptedMessage.cs` are declared in the **root** namespace `ViciOne.ServiceBus.Tests` (not
   `...Tests.Messages`) and are referenced from outside this cohort.
6. **`ReliableMessaging/ReliableStateMachine.cs` imports `ViciOne.ServiceBus.TestFramework`** for
   `IntentionalTestException`. It is the only file in this cohort that does. The new test
   infrastructure must supply that type before `src/ViciOne.ServiceBus.TestFramework/**` can be
   removed.
7. **MessagePack ownership** — 67 of the 454 identities are bound to `core` by the anchor but map to
   `ViciOne.ServiceBus.MessagePack.Tests` under section 5. See `RECONCILIATION.md` section 5.
8. **`NUnit1032` is suppressed 45 times in 17 of the 115 files** of this cohort with `#pragma warning disable NUnit1032`
   around `Task<ConsumeContext<T>>` fields. The analyzer is warning that a disposable field is not
   disposed; the suppression is inherited wholesale and no fixture explains it. The rebuild should
   not carry the suppression forward without a reason.
