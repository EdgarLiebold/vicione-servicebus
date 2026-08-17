# ViciOne.ServiceBus modifications

ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.

The baseline is the complete MassTransit 8.5.10 source tree at upstream commit `62ab339afa3bac2e9b3fe1769d0d35d7e44778e9`, imported into the local fork baseline commit `1de4bf6eb45c406da3cd6f26bdab6ed6d5aeefbc` (tree `2b09d4e2b2e14289f06ba112ce1ae52e326a0307`).

ViciOne changed the technical identity to `ViciOne.ServiceBus` across paths, projects, assemblies, packages, source identifiers, configuration, wire formats, topology, diagnostics, tests, documentation and automation. The deterministic mapping and one-to-one baseline census are generated under `evidence/WP-F2-SERVICEBUS-IDENTITY/`.

## Exact changed-format exceptions

The following changed baseline files cannot carry a syntax-valid in-file comment. Each entry is exact; this is not an extension- or directory-wide waiver.

| Target path | Reason and modification |
|---|---|
| `ViciOne.ServiceBus.slnx` | Solution file; paths renamed, and the solution migrated to the SDK's XML solution format, which this repository does not annotate per file because CHANGELIST.md carries the section 4(b) record. |
| `ViciOne.ServiceBus.snk` | Binary strong-name key; path renamed, bytes retained. |
| `vicione-servicebus-logo.png` | Binary PNG; replaced with the ViciOne.ServiceBus product asset. |
| `src/vicione-servicebus-logo.png` | Binary PNG packaging copy; replaced with the same product asset. |
| `tests/ViciOne.ServiceBus.Abstractions.Tests/NewId/texts.txt` | Commentless golden-vector fixture; path renamed, vector bytes retained. |
| `tests/ViciOne.ServiceBus.EventHubIntegration.Tests/config.json` | Strict JSON fixture; identity-bearing keys/values renamed. |
| `tests/ViciOne.ServiceBus.KafkaIntegration.Tests/KafkaMessage.avsc` | Strict Avro JSON schema; identity-bearing names renamed. |
| `tests/ViciOne.ServiceBus.RabbitMqTransport.Tests/client.p12` | Binary certificate fixture; path renamed, bytes retained. |
