# C36 product-path analysis — structured bus probe

## Preserved capability

`OBL-R0-CORE-D-0199` requires bus introspection to report the configured input endpoint address.
The useful product capability is the structured bus probe, not the inherited TestFramework helper
that serializes that probe to JSON and reparses fixed property names.

## Complete relevant path

1. `IntrospectionExtensions.GetProbeResult` creates one `ProbeResultBuilder` and invokes the bus as
   an `IProbeSite`.
2. `ViciOneServiceBusBus.Probe` creates the `bus` scope and delegates to its host.
3. `BaseHost.Probe` creates the `host` scope, records transport metadata and asks
   `ReceiveEndpointCollection` to enumerate its current endpoints.
4. Each endpoint creates one `receiveEndpoint` scope, then `ReceiveEndpoint.Probe` delegates to the
   receive transport.
5. `InMemoryReceiveTransport.Probe` creates `receiveTransport` and stores its real input `Uri` under
   `address`, together with prefetch and concurrency metadata.
6. Stopping a dynamic endpoint handle removes it from the collection before transport shutdown, so
   the next probe cannot report a stale endpoint.

The harness has two persistent endpoints: its configured input endpoint and its internal bus
endpoint. Connecting a dynamic endpoint creates a third. The native tests assert all three unique
addresses, then assert the exact two-address state after the dynamic handle stops.

The old rate-limit, concurrency-limit, handler and MultiTestConsumer setup was unrelated to the sole
assertion and is not reproduced. No product defect was found and no product source was permanently
changed.
