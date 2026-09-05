# Work package D plan

1. Establish the semantic inventory of concrete startup-configuration models and bind every model to
   an executing positive case and every declared invariant to a negative case.
2. Make one per-bus startup validator own transport, limits, feature ownership, reliable-messaging,
   serializer, contract-catalog, and endpoint-QoS composition; move feature selection into the bus
   block and add the fluent message-journal provider path.
3. Introduce mandatory `MessageLimits`, enforce the wire-envelope boundary in all nine receive paths,
   bind JSON depth and raw/TryGet materialization to the same limits, and preserve telemetry envelope
   ordering before observers and provider I/O.
4. Replace every CBC/V1/V2 message-data encryption path with one versioned AES-GCM envelope and a
   rotating key-provider contract; prove authentication, rotation, key-material ownership, bounds,
   and nonce uniqueness.
5. Normalize all `ConfigurationException` messages through one public provider SPI, bind the rule with
   Roslyn, execute focused counterexamples, then run strict builds and the complete UnitArchitecture
   profile three times without filters or skips.
