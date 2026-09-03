# V5 payload, diagnostics, and analyzers review disposition

Date: 2026-09-03

| Review obligation | Disposition |
|---|---|
| Immutable payload-admission SPI and positive hard limits | Integrated in Abstractions with startup validation |
| Exact application-body decision | Integrated against the configured serializer's emitted body bytes |
| Independent final transport-envelope decision | Integrated in JSON, raw JSON, and MessagePack owners |
| Hard bounded `IBufferWriter` | Integrated with fixed owner capacity and pre-overrun failure |
| Single-pass serialization and shared admitted bytes | Integrated; counting converters/resolvers prove one application write |
| Rejection before observer/provider I/O | Integrated at the common send boundary and Event Hub single/batch special paths |
| Existing V4 MessageData ownership | Reused; only a real stored reference emits payload-free evidence |
| Missing owner and MultiBus isolation | Integrated with loud threshold rejection and bus-keyed runtime selection |
| Empty/missing MessageData behavior | Preserved; `HasValue` precedes `Address`, retaining exact fault semantics |
| Payload/member sensitivity metadata | Integrated with inherited conservative classification |
| Bounded safe diagnostics | Integrated for controls, Unicode, URI, primitive, arbitrary-object, and collectible-type cases |
| Payload-free low-cardinality rejection metrics | Integrated once per buffer with hostile exporter isolation |
| Logger/Activity/health/observer ownership | No duplicate admission owner added; established hostile pipeline owners remain green |
| VOSB5001-VOSB5005 | Integrated with canonical semantic symbols, generated-code exclusion, and deterministic deduplication |
| V5.1 RT-001 | Integrated as exact canonical `ConsumerDefinition` property-symbol identity |
| V5.1 RT-005 | Integrated as bounded single-pass body/envelope serialization without unbounded staging |
| Donor regression projects | Not copied; replaced by native source-owner xUnit 4/MTP v2 tests and passive requirement mappings |
| Exact final bytes versus serializer reservation | Explicitly distinguished; hard memory limit wins when a legal size hint exceeds remaining capacity |
| Provider/cloud execution | No new cloud dependency exists in this package; real provider release gates from other packages remain unchanged |

No second serializer, MessageData repository/policy, telemetry stack, analyzer verdict path, application `ToString`
fallback, unbounded diagnostic field, or payload-bearing metric dimension was introduced.
