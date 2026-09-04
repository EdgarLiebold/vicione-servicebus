# ViciOne.ServiceBus A+ final closure validation

Date: 2026-09-04

## Bound scope

- Work package: `WP-F2-SERVICEBUS-A-PLUS-FINAL-CLOSURE-01`.
- Product baseline: commit `4362f6423553e2ade6df41dbb4804c44a28ad53d`, tree
  `b677aa015f3c68d8e6878a8f55c8895a6b588033`.
- Architecture decision: `PO-2026-09-04-01`.
- Immutable review source: `review/10_V5_API_Review/ChatGPT-API Usability Review-20260903-0838.md`.
- `review/**` remained unmodified and unstaged.

## Platform and dependency closure

`global.json` now selects only Microsoft Testing Platform. The workflow follows stable `10.0.x`;
documentation no longer requires a particular patch. Repository architecture tests reject an SDK
block, an exact workflow patch, and exact repository language-version pins. The analyzer projects use
`latest` only to opt the isolated `netstandard2.0` compiler boundary into the installed compiler;
there is no numbered language pin.

The isolated official stable SDK used for the final run was SDK 10.0.400, MSBuild 18.9.6, host and
runtimes 10.0.11 on `osx-x64`; see `SDK_RUNTIME.json`. Both Shipping and Engineering solutions restore
in locked mode and build in Release with zero warnings and errors.

All 52 direct package dispositions are in `DEPENDENCY_DISPOSITION.json`: 15 stable-compatible updates,
36 already-current packages, and removal of `Quartz.Extensions.Hosting` after Quartz 4 folded hosting
into the core package. The final official-feed queries reported zero outdated direct packages, zero
known vulnerable packages including transitives, and zero deprecated packages including transitives.
Quartz 4 required a real source migration to its `ValueTask` job lifecycle, generic builders,
`JobScope`, scheduler status, and builder-owned standalone job factory. All 89 Quartz tests pass as
part of the complete Unit/Architecture run.

## Durable Sender provider acceptance

The capability matrix advertises two supported dispatchers:

- InMemory retires only at in-process consumer completion, proven by its deterministic real pipeline
  tests from the preceding Durable Sender package.
- RabbitMQ retires at publisher-confirmed, persistent, mandatory route admission. The provider-owned
  dispatcher replays the exact stored body and identifiers and awaits the transport send task.

All other external bus transports remain startup-fail-closed as unsupported. The architecture owner
requires every future non-InMemory dispatcher to name a `real-*` acceptance environment, preventing a
mock or documentation-only capability claim.

The complete canonical real RabbitMQ suite passed 27/27 with zero skips, run identity
`vicione-6f6a993c30d2`. Its new acceptance cohort proves exact body/metadata, persistent retention after
sender shutdown, transport acceptance mode, mandatory unroutable rejection, and cancellation with no
publish. The restored focused rerun passed 3/3, run `vicione-4db121109552`. Mutations M01-M03 prove
that persistence, routing rejection, and acknowledgement waiting are independently observable.

## Startup, API, discoverability, and heritage

Eleven static configuration families are exhaustively inventoried in
`docs/static-configuration-validation.json`, each bound to source, exception, actionable fragment, and
executing positive/negative owner. Host lifecycle options now participate in `ValidateOnStart`; their
four invalid invariants fail through `IStartupValidator`, and a coherent policy passes (5/5).

`docs/api-surface.md` defines Application, Advanced SPI, Provider, Operations, and Testing ownership.
Advanced definition, binder, manual scheduler, host validator, and Durable Store/Dispatcher contracts
are hidden from default IntelliSense while remaining public only where a real extension capability
requires them. Testing packages stay out of Shipping. Four architecture tests enforce the layers,
preferred journeys, static inventory, and heritage disposition.

The 14 Developer Journeys restored and built only against eight freshly packed ViciOne NuGets with
zero warnings/errors. Their packed public API baseline contains 20,661 lines and SHA-256
`32022968b9d6779d0afa4c784a669f6c8d587f698caee332f4cf3b2fc06397bc`.

All 12 reviewed MassTransit-derived shapes have one exact terminal disposition in
`docs/mass-transit-heritage-disposition.json`. Industry messaging concepts and active application or
provider mechanisms are retained for current capability; advanced mechanisms are hidden; product
identity and legacy test/verification compatibility are removed. Production and preferred sample
artifacts contain no MassTransit identity.

## Final execution record

| Scope | Result |
|---|---:|
| Current SDK/runtime inventory | SDK 10.0.400; runtime 10.0.11; stable, no preview |
| Engineering locked restore | passed |
| Shipping locked restore | passed |
| Shipping Release build | 0 warnings, 0 errors |
| Engineering Release build | 0 warnings, 0 errors |
| Complete Unit/Architecture solution | 3,490/3,490 passed, 0 failed, 0 skipped |
| Complete RabbitMQ real-infrastructure solution | 27/27 passed, 0 failed, 0 skipped |
| Quartz owner within full suite | 89/89 passed |
| Host startup validation owner | 5/5 passed |
| API/discoverability/heritage owner | 4/4 passed |
| Repository graph owner | 15/15 passed |
| Package-only journeys | 14/14 compiled from 8 fresh packages |
| Packed public API baseline | 8 assemblies, 20,661 lines, SHA-256 recorded above |
| Direct dependency outdated audit | 0 |
| Vulnerability audit including transitives | 0 |
| Deprecation audit including transitives | 0 |
| Scoped `dotnet format --verify-no-changes` | exit 0 |
| One-cause mutations | 9/9 killed and restored |

The first complete Unit/Architecture attempt exposed one pre-existing process-global ActivityListener
race in a test lacking the repository's OpenTelemetry serial collection. Adding that test to the
established collection made the case pass five consecutive focused runs and the complete 3,490-test
rerun. This is recorded rather than concealing the diagnostic iteration.

## Verdict

Acceptance criteria `AC-AFC-001` through `AC-AFC-004` pass locally. Review gates 3, 13, 17, 22, 24,
and 25 are closed by current executing evidence. No cloud acceptance claim is made for a provider that
is not advertised as Durable Sender capable. No remote publication is included in this work package.
