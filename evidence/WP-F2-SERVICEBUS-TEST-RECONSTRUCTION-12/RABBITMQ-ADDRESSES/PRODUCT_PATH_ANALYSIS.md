# RabbitMQ address product-path analysis

## Frozen inherited input

- Source fixture: `tests/Transports/ViciOne.ServiceBus.RabbitMqTransport.Tests/RabbitMqAddress_Specs.cs`
- Source fixture SHA-256 before retirement: `9264bfe2e2e763ba683b7bd1afc6473a4d3d18a9090105454c7f21751843ba48`
- Frozen semantic ledger: `evidence/WP-F2-SERVICEBUS-TEST-RECONSTRUCTION-11/R0-CONVERGENCE-06/COMBINED_SEMANTIC_LEDGER.jsonl`
- Frozen ledger SHA-256: `5b7134255db42f7fdb4276598b4790660059d006e13bd184db8fe5cf6bf51a66`
- Closed inherited range: `OBL-R0-BRK-0215` through `OBL-R0-BRK-0260`, exactly 46 unique obligations.

The source fixture was read in full. `INHERITED_BEHAVIOR_DISPOSITION.json` maps every frozen
obligation exactly once to an executing native xUnit/MTP replacement before the old fixture is
removed. The old Git history remains the byte-preserving archive.

## Product owners read in full

- `RabbitMqHostAddress`
- `RabbitMqEndpointAddress`
- `RabbitMqAddressExtensions`
- `ConfigurationHostSettings`
- `RabbitMqHostConfigurator`
- `RabbitMqEntityNameValidator`
- `RabbitMqSendSettings`
- `RabbitMqAddressException`
- the shared query/path parser and every RabbitMQ construction call site

## Product corrections

- Host and endpoint addresses are immutable values with concrete normalized ports.
- `rabbitmq`, `rabbitmqs`, `amqp`, and `amqps` have explicit scheme semantics; TLS is never inferred
  from a port number.
- All recognized query values are parsed strictly; duplicates, unknown options, invalid values and
  conflicting lifetime flags fail closed.
- Host options are accepted on a full RabbitMQ URI, where they take effect, and rejected on opaque
  `queue:`/`exchange:` short addresses, where they cannot take effect.
- All endpoint option values are percent-decoded on input and encoded on output; binding collections
  are validated, deduplicated in ordinal order, defensively copied and exposed read-only.
- Delayed exchanges normalize independently of option order and generated auxiliary names pass the
  same entity-name validation as public names.
- Entity-name length is enforced as the RabbitMQ limit of 255 UTF-8 bytes, not 255 UTF-16 code units.
- Credential parsing splits only the first authority colon, preserving colons in passwords.
- TLS uses operating-system protocol selection, validates certificate chains and names by default,
  and uses the configured host as the default certificate server name. Policy exceptions remain an
  explicit opt-in through the existing SSL configurator.
- The obsolete HTTP help link and formatter-based exception serialization surface are absent.

## Test ownership

The replacement tree mirrors the source ownership under
`tests2/Transports/ViciOne.ServiceBus.RabbitMqTransport.Tests`. It uses one native MTP executable,
the shared repository test configuration, the central requirement attribute, and an embedded
requirement projection checked against compiled test metadata. No broker, container, network,
clock, random source, skip, sleep, shared mutable fixture or alternate test runner is involved.
