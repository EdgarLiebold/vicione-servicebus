# V4 multibus routing, scoped reliability, and final-tail validation

Date: 2026-09-03

## Bound inputs and scope

- Package: reviewer integration 12/12, V4 checkpoint 011 and the complete remaining V4 tail.
- Architecture assignment: `PO-2026-09-03-SERVICEBUS-REVIEW-INTEGRATION-11` at local architecture
  commit `23aa7d969b1bba47987c4430de3fedc52049fbb0`.
- Product baseline: `a2c39b21c653ea09b61120e31e2bb3961d1eae34`, tree
  `ee100e64fb655dcfd7a78c57eb3afdf2144f8713`.
- Protected review aggregate SHA-256:
  `371bf21331f0fc3316be271bce04ab37b3c54c50e13f443789d94c1f6eca1f18`.
- V4 bundle SHA-256: `e8f28736562bf7c4fa8ffca4dfd662cd5105d3124e26d2ba424fe1ac0d192b87`.
- Semantic source: `9aa3921d64e570d7d0977bcbf71c26ad33503615`; every later V4 commit through
  final head `f8050928715e536b60c42d800d1cbb81c085818f` was separately reconciled.
- `review/**` remained read-only, unmodified, untracked, and unstaged throughout.

The current product already contained stronger native RabbitMQ, test reconstruction, concurrency,
resource-cache, retry, time, and lifecycle owners than several donor revisions. The V4 history was
therefore treated as semantic evidence, never as an authoritative replacement tree.

## Bus-owned routing and topology

Every materialized bus now owns an independent route table. Exact message routes win; an unambiguous
implemented-contract route can be inherited; multiple inherited candidates fail deterministically rather
than selecting by registration order. Duplicate equal routes are idempotent, conflicting duplicates fail,
and a materialized table is immutable.

`EndpointConvention` is only a bootstrap facade over the actively configured bus. It does not retain a
second process-global runtime route catalog. Send endpoint resolution, client factories, scoped providers,
outbox providers, and typed `BusInstance<TBus>` all forward the actual bus-owned route provider. A native
three-bus test exposed and then closed the last wrapper boundary: without forwarding the internal route
provider, buses B and C resolved request clients without their own routes.

Application-wide message conventions remain a separate bootstrap responsibility. The test application
registers correlation conventions once, before any runtime topology is created; the catalog then freezes.
The saga integration tests prove this contract without adding bus-local rules that could mask it.

## Scoped policy and reliability ownership

Message-data defaults are immutable per bus. Initializer conventions are registered in a single catalog
that freezes at materialization. Request clients resolve their actual `TBus` owner, and ambiguous or
unbound registrations fail rather than leaking the default bus.

Outbox consumer identity includes the bus owner. Scheduler state, time-zone resolution, and request state
are resolved from the active owner instead of process-static or default-bus state. Quartz recurring
commands use the owner-scoped time-zone resolver, and technical scheduling identity uses `TokenId` rather
than conflating the scheduler token with application correlation.

Send endpoint caches, receive endpoints, and producer caches now expose and execute their asynchronous
disposal boundaries. Assembly scanning is deterministic and synchronous at the configuration boundary;
the cache no longer starts hidden work whose completion escapes the caller.

## Testing, public surface, and source layout

Reusable harness implementation moved from the shipping runtime into the engineering-only
`ViciOne.ServiceBus.Testing` project. Azure Service Bus, Event Hubs, and RabbitMQ each have a matching
provider-testing project. The shipping solution contains none of these four projects; the Engineering
solution contains all four. Packaging produced exactly 19 shipping packages and no package whose name
ends in `.Testing`.

The terminal TestFramework ledger contains 147 entries: 9 executing capabilities moved into the native
testing library, 135 support-only capabilities retired to stronger native owners, and 3 obsolete build
artifacts retired. Its SHA-256 is
`55974b5ff3d17507b9529ab3ec311178b4dfb13748f58d6a3d5699f834cb1390`.
No old TestFramework project, NUnit dependency, duplicate verdict path, or legacy regression project was
reintroduced.

The V4 source-layout normalization was integrated as byte-preserving moves wherever possible: the final
index records 710 exact renames. Immediate repeated directory components were eliminated from `src`, and
the architecture gate inspects C# source paths rather than confusing deliberately named non-source build
assets with product layout. No non-build empty directory remains.

## Native test quality and corrections beyond the donor

The native owners cover positive, negative, boundary, ambiguity, concurrency, freeze, cancellation,
lifecycle, disposal, and integration behavior. They assert externally observable routes, provider
identity, exact exceptions, immutable snapshots, retained histories, and delivered scheduling metadata;
they do not duplicate implementation constants or treat timeouts as success.

Final regression uncovered three integration defects that a mechanical donor application would have
missed:

1. typed `BusInstance<TBus>` did not forward the internal route provider, so non-default request clients
   could not use their bus-owned routes;
2. two saga tests attempted to configure application-wide correlation after another topology had frozen
   the catalog; the conventions now live at deterministic assembly bootstrap;
3. a Quartz owner test was executable but absent from requirement projection; its exact requirement
   variant is now present.

The request-state-machine retry callbacks were also completed with explicit bounded redelivery so their
failure path is a valid reliability policy rather than a retry-free callback.

## Positive execution evidence

All test executables use xUnit 4 on Microsoft.Testing.Platform v2. The application executables were run
serially and directly after their Release build because the restricted environment denies the named-pipe
server used by `dotnet test`; this executes the same built MTP test app without weakening discovery,
minimum-count, timeout, failure, or zero-test gates.

| Scope | Result |
|---|---:|
| Core owner after final bootstrap cleanup | 1,613/1,613 passed, 0 skipped |
| Architecture owner | 167/167 passed, 0 skipped |
| Other 21 Unit executables | 1,510/1,510 passed, 0 skipped |
| Complete Unit/Architecture aggregate | 3,290/3,290 passed, 0 skipped |
| Shipping Release build | 0 warnings, 0 errors |
| Engineering Release build | 0 warnings, 0 errors |
| Locked restore graph | 67/67 projects restored |
| Shipping package set | 19 packages, 0 Testing packages |

The final Core CTRF report SHA-256 is
`573df9bf9b80775767b0a89aade759d9c4406ebee4e3880a4aba37416bc3953a`; the Architecture report is
`9e368c5d3a925e98216e8568b6ba3d227db0f9c014dd2a499f1cd3e7476369a1`.

No provider container was running at final freeze, so a new external-provider profile was not represented
as green. All provider unit owners executed without skips, and every LocalIntegration project compiled in
the Engineering build. Real-provider behavior proven by earlier V4 packages remains recorded in their
package-specific evidence and was not relabeled or duplicated here.

## Mutation and hygiene gates

Twenty-two independent, buildable, one-cause production mutations were executed. Every mutation made its
named native owner causally red and every target was restored before final positive execution. Full detail
is in `MUTATION_VALIDATION.md`.

- scoped full `dotnet format --verify-no-changes` passes for all 228 in-content changed/new C# files;
- all requirements JSON files parse and project in the complete native test run;
- the final index passes `git diff --check`;
- the protected review manifest and V4 bundle hashes still match;
- no protected review file is staged or modified;
- no non-build empty directory or immediately repeated `src` directory component remains;
- shipping and Engineering solution composition, dependency locks, and package boundaries are proven by
  native architecture tests.

V4 reviewer integration is technically complete at 12/12 packages (100%) and ready for its local product
and architecture freeze. V5, V5.1, and API-review work remain separate active goal phases. No remote
publication is authorized or implied by this evidence.
