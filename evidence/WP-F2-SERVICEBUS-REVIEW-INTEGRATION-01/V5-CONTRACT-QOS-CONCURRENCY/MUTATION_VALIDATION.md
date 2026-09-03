# V5 contract, QoS, and concurrency mutation validation

Date: 2026-09-03

Each accepted mutation changed one production mechanism, compiled the complete owning test project, and
made the named native owner fail for the intended reason. Compiler errors and stale copied assemblies were
not counted. Every mutation was removed before the next mutation and before final positive validation.

| ID | One-cause production mutation | Causal killing observation |
|---|---|---|
| M01 | accept a leading-zero major version in canonical identity text | strict canonical parse case failed |
| M02 | raise the stable contract-name limit from 256 to 257 | exact maximum-plus-one boundary failed |
| M03 | accept major version zero | persisted version lower-bound case failed |
| M04 | invert exact catalog-registration idempotence | duplicate registration case failed |
| M05 | return a default identity for an unknown catalog type | fail-closed lookup case failed |
| M06 | reject consumer-owned QoS even on a genuinely dedicated endpoint | frozen V5 dedicated compatibility case failed |
| M07 | ignore conflicting endpoint QoS declarations | deterministic aggregate-conflict case failed |
| M08 | project consumer-definition concurrency back onto endpoint transport QoS | endpoint prefetch/concurrency ownership matrix failed |
| M09 | bypass bounded semaphore admission | seven exclusion, bound, cancellation, failure, and disposal cases failed |
| M10 | choose a partition from the message object instead of its selected key | real consume-pipeline same-key exclusion failed |
| M11 | omit the global concurrency pipe specification | exact built-pipeline probe owner failed |
| M12 | invert exact consumer-policy idempotence | configuration conflict/idempotence owner failed |
| M13 | let legacy `UseConcurrentMessageLimit<TConsumer>` bypass the new owner | exact built-pipeline probe owner failed |
| M14 | validate only the first QoS declaration in runtime endpoint discovery | shared endpoint started instead of failing before materialization |
| M15 | classify consumer-definition endpoint settings as endpoint-owned | two consumers with identical QoS incorrectly started their shared endpoint |
| M16 | classify one shared endpoint-definition instance as consumer-owned | both Quartz raw/envelope scheduler owners failed eager topology validation |

M10 deliberately produced one nullable compiler warning while remaining buildable and reaching the causal
runtime owner; that mutant warning is not baseline evidence. The restored analyzer-active candidate builds
with zero warnings.

An initial M04 attempt built only the production project and then invoked a test application's stale copied
product DLL, so the intended case stayed green. That run was rejected. Rebuilding the complete test project
made the same mutation causally red and is the only result counted above. This preserves the operational
rule that direct MTP application execution must use freshly copied dependency assemblies.

The restored implementation has these SHA-256 values:

```text
db2e8a66a46b48f5b89665b61115b18104b0d767e2b1d2de697a7d6a68ac40da  MessageContractIdentity.cs
dff2ecff1fdba87e3a0131b2d7dac9a71eda60f989c81f91b14e53c3384daf1d  MessageContractCatalogBuilder.cs
f06ff1f7bdb6c77cef7e2ea77e9aa4a466d6c3c9daa9808ec48e0336f83c7fb4  EndpointQosTopologyValidator.cs
6f886099c7e3c71d79038d9fdf41bce6a874620caab7044aaf81704cd121eac7  ConsumerConcurrencyGate.cs
8e1def2841bb324420c2c505152bd606d54853f9ef45129ddacdde8b0ea5d10f  PartitionedConsumerConcurrencyGate.cs
9c587d4e681a88bbdb10c62cc10aba0f7a49003683c6064ae2d4023c24b15752  ConsumerSpecification.cs
8e408aa25825bd691cddcebb5bd8acc72e0531226885800f417bfb2c1fe9079e  CombinedEndpointDefinition.cs
dc5b3398f2c0e398236e446f70d3cda0dd61226ce711352a176c66584f446e4d  DelegateEndpointDefinition.cs
```

The final positive Core owner passes 1,635/1,635 and the complete Unit/Architecture aggregate passes
3,345/3,345 with zero skips after every mutant was removed.
