# Work package D validation

## Reproducible execution environment

Final commands used SDK 10.0.400 and runtime 10.0.11 with a task-scoped CLI home,
`DOTNET_MULTILEVEL_LOOKUP=0`, the existing NuGet cache, disabled MSBuild node reuse, disabled build
servers, and serial MSBuild. Microsoft Testing Platform and Roslyn workspace commands ran outside the
filesystem sandbox because their local IPC is not reliable inside it.

No accepted test invocation used a filter. Focused filtered executions were diagnostic or stability
evidence only and are not counted as full-profile acceptance.

## Final acceptance results

| Gate | Result |
|---|---|
| Product Release build, warnings as errors | exit 0; 0 warnings; 0 errors |
| Engineering Release build, warnings as errors | exit 0; 0 warnings; 0 errors |
| Unit Release build, warnings as errors | exit 0; 0 warnings; 0 errors |
| Engineering `dotnet format --verify-no-changes` | exit 0; no changes |
| UnitArchitecture accepted run 1 | 3,676 total; 3,676 passed; 0 failed; 0 skipped; 2m 46.316s |
| UnitArchitecture accepted run 2 | 3,676 total; 3,676 passed; 0 failed; 0 skipped; 2m 46.937s |
| UnitArchitecture accepted run 3 | 3,676 total; 3,676 passed; 0 failed; 0 skipped; 2m 51.059s |
| Product package build | 19 packages; exit 0 |
| Developer Journeys | 14 scenarios; 8 freshly packed packages; 0 warnings; 0 errors; exit 0 |
| Packed public API baseline | 8 assemblies; 20,791 lines; SHA-256 `f51f7d1bf02f264fdd8e1f3d01ce6b71e109a05b0a20786b9500773fbb8ed91a` |
| NuGet vulnerability audit | 22 product projects; 0 vulnerable direct or transitive packages; exit 0 |
| `git diff --check` | exit 0 |

The complete-profile command was identical in all three accepted runs except for the results
directory:

```text
dotnet test --solution ViciOne.ServiceBus.Tests.Unit.slnx -c Release --no-build --no-restore \
  --results-directory artifacts/test-results/a-plus-api-d-accepted-run-<n> \
  --minimum-expected-tests 3676 --max-parallel-test-modules 1
```

## Startup-configuration inventory

The review's value 44 was a lexical pre-program count. The semantic source inventory contains 40
concrete options models. The lexical suffix set also contains two validator/configurer types that are
not options models and one abstract options base; immutable operation records are invocation values,
not startup configuration. `docs/static-configuration-validation.json` records this reconciliation.

All 40 concrete models have an executing positive test. Every model with a declared invariant has an
executing negative test. The existing architecture inventory test binds the source set, invariant
counts, test methods, and requirement coverage to that document.

## Bus composition and journal providers

Exactly one startup validator is registered per bus. It aggregates transport and `MessageLimits`
selection, orphan/duplicate feature ownership, reliable-messaging store and dispatcher ownership,
contract-catalog materialization, serializer uniqueness, and endpoint-QoS ownership before runtime
hosted services materialize the bus.

The bus block owns contracts, redaction, payload admission, reliable messaging, and
`UseMessageJournal`. The journal builder rejects incomplete or duplicate provider selection. The
Entity Framework Core and Azure Table provider selectors are tested through construction and service
resolution without database or network I/O. No real Azure cloud acceptance is claimed by this work
package.

## Mandatory limits

Nine architecture cases bind RabbitMQ, ActiveMQ, Amazon SQS, Azure Service Bus, Event Hubs,
PostgreSQL, SQL Server, In-Memory, and Mediator to the common guarded receive boundary. Runtime tests
prove inclusive maximum and maximum-plus-one behavior; deserialization can only read the guarded
body. Separate cases bind `MaxJsonDepth`, raw JSON, and `TryGetMessage` to the same rejection model.

The send boundary remains activity/header creation, payload admission, observers, then provider I/O.
This is documented in the repository-wide deviations record because serialization must include trace
headers while no observer or provider may see an unadmitted payload.

## Encryption and configuration messages

The sole message-data encryption implementation is AES-GCM with magic/version, key identifier,
96-bit random nonce, ciphertext, and 128-bit tag. Version and key identifier are authenticated data.
Ten requirement-bound cases cover exact and boundary round trips, every authenticated-region tamper,
wrong key, key rotation and removal, 10,000 unique nonces, object limits, AES key sizes, and defensive
key copies.

The banned CBC primitives `PaddingMode`, `CreateEncryptor`, and `CreateDecryptor` have zero source
matches. The removed V1/V2 stream-provider and legacy key-provider type names also have zero source
matches.

The final semantic ConfigurationException scan found 276 creations, 276 compliant messages, zero
pending changes, and zero unresolved creations. The Roslyn architecture gate independently checks the
factory/format rule and newline normalization.

## Related defects found during acceptance

- A batching integration oracle assumed six concurrently published messages must partition as 5+1
  despite a real 50-ms from-first boundary. The revised test establishes the size completion before
  publishing the tail and passed ten consecutive focused runs.
- The diagnostics cancellation case assumed two scheduler yields completed a cancellation
  continuation. It now awaits the result through a bounded hang guard and passes in isolation and all
  accepted profile runs.
- ActiveMQ exposed a real listener-registration race: a shared session could be published before its
  connection-fault listener existed. Listener registration now precedes context publication; the
  regression passed 50 consecutive isolated process runs and all three accepted profiles.

The full reasoning and scope of these corrections are in
`evidence/WP-F2-SERVICEBUS-A-PLUS-API/DEVIATIONS.md`.
