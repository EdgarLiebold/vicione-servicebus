# R0 integrator decision log

Every cohort question is first held against the frozen contract. A question that the Development
Slice, the Lead plan or a bound PO decision already answers is decided here and **not** sent to the
Lead; only a genuine stop class travels. Each row names the authority it was decided from.

## Decided within the slice

### D-01 · Solution membership of `Benchmarks.Tests` and `Diagnostics.Tests` (cohort R0-BLD, F-16)

**Question:** do these two projects belong to `Tests.Unit.slnx`, to `Engineering.slnx`, or both?
Section 8 names three profiles, section 4 names five solutions, and Engineering is not among the
three.

**Decision: both, and that is not a contradiction.** The two roles are different things.
*Profile* membership is what section 8 governs, and it arises from the **three profile solutions**
only — `Tests.Unit`, `Tests.LocalIntegration`, `Tests.External`. `Engineering.slnx` is described in
section 5 as "ein kleiner, gezielter Arbeitsgraph für Benchmarks und das Diagnosewerkzeug", a
working graph, never a profile. A project therefore carries exactly one profile membership while
still appearing in a working graph.

Both projects land in the `UnitArchitecture` profile, i.e. `Tests.Unit.slnx`, and both also appear
in `Engineering.slnx`. The plan states each of these directly: `REQ-TEST-104` requires all 44
Diagnostics obligations to "execute in UnitArchitecture", and the section 5 owner table assigns the
benchmark owner "hermetische Benchmarkgültigkeit".

**Consequence for the architecture test:** the rule "every executable test project belongs to
exactly one profile" must count membership across the three **profile** solutions only. A rule that
counted all five would fail on the correct layout — the same class of error as a package gate that
matches by name prefix.

Authority: Lead plan sections 4, 5, 8; `REQ-TEST-104`. Class: `WORK_ORDER_WITHIN_SLICE`.

### D-02 · `client.p12`

Decided in `SECURITY_DISPOSITION_CLIENT_P12.md` against `PO-2026-08-20-05` item 6. Removed as an
unused fixture; the residual reported, not silently closed.

## Verified cohort findings

### V-01 · The shipped TestFramework package declares NUnit as a public dependency (R0-BLD, F-11)

Independently re-measured rather than accepted. `src/Directory.Build.props:23` sets
`<IsPackable>True</IsPackable>` for everything under `src/`, so
`src/ViciOne.ServiceBus.TestFramework` is a **shipped package**. Its csproj carries
`<PackageReference Include="NUnit" />` with **no** `PrivateAssets`, while `NUnit.Analyzers`
immediately below it does carry `PrivateAssets=all`. The distinction was understood and not applied
to NUnit. `PackageTags` and `Description` additionally advertise NUnit.

**Not a new blocker, and deliberately not raised as one.** `PO-2026-08-20-01` already turned the
TestFramework into test-internal infrastructure, and wave F2 removes the project outright. The
finding is recorded because it changes the *justification* of that removal: it is not only
structural tidiness, it ends a shipped product package that pulls a test framework into every
consumer's dependency graph. The root Markdown files and `NuGet.README.md` must lose the
corresponding advertising in the same wave.

## Deferred with a named owner — not Lead questions

### DF-01 · Testcontainers port stability across container restart (R0-BLD, F-07)

Whether the mapped host port survives a restart decides whether the inherited HAProxy relay is
reproduced or dropped. This is a **measurement**, not an assumption, and it belongs to the
LocalIntegration fixture design in wave C3. It does not block R0 closure and is not sent to the
Lead as a question. Owner: C3 broker fixture design, to be measured before the ActiveMQ fixture is
written.

## Genuine stop classes — these do travel to the Lead

### Q-01 · `global.json` is outside the write scope (integrator, measured)

Stop class `WRITE_SCOPE_CHANGE`. Evidence `MTP_COMMAND_FORM_PROBE.md`. Without the SDK 10 opt-in no
release-relevant profile run is possible, and the file is in none of the 91 write scopes.

### Q-02 · A non-terminal disposition has no home in the vocabulary (R0-BLD, F-08)

The inherited verification model carries `NOT_DUE_DEFECTIVE_IMPORTED_ASSURANCE` — an inherited case
that is itself unsound. Section 9 offers four terminal dispositions and two non-terminal ones
(`BLOCKED`, `QUESTION`), none of which fits "the inherited assurance exists but is defective, so
reproducing it would reproduce the defect". Deciding this alone would be a `PROOF_OR_GATE_CHANGE`.

### Q-03 · Artemis identities inside a required anchor (R0-BLD, F-09)

`docs/build.md` documents Artemis three times as "not a required gate", yet three `("artemis")`
identities sit inside the **required** `activemq` anchor and `artemis` appears in that run's
`brokers` array. Section 9 forbids adopting an anchor difference as the new truth, so this is
reported for Lead disposition rather than resolved locally.

---

## Cohort R0-PY intake

### D-03 · Anchor-identity → obligationId mapping is an R0 deliverable (R0-PY, Q8)

**Question:** nothing in the plan explicitly produces a per-cohort mapping from the 3114 anchor
identities to `obligationId`s, yet section 12.2 item 9 requires every ledger obligation to be tied
through the sentinel to an actually executed test identity.

**Decision: R0 produces it, as part of the frozen level-1 set.** The requirement is unsatisfiable
without it, and every cohort is already deriving exactly this correspondence in its
`RECONCILIATION.md`. What is missing is only that it be machine-readable and frozen rather than
prose. The integrator emits `ANCHOR_IDENTITY_TO_OBLIGATION.tsv` over all eleven anchors, and its
SHA-256 is bound in the checkpoint together with the obligation set.

This is work ordering inside the slice, not a proof change: it adds no obligation, weakens no gate
and changes no disposition vocabulary. Class: `WORK_ORDER_WITHIN_SLICE`.

### D-04 · Two disposition vocabularies, both correct (R0-PY, Q9)

The tooling-assurance register uses `NATIVE_MTP` / `MSBUILD_RULE` / `XUNIT_ARCHITECTURE_TEST` /
`RETAINED_ENGINEERING_TOOL` / `REMOVED_WITH_RATIONALE`, from Lead plan section 3.1. The semantic
ledger uses the four terminal dispositions of section 9. These are different registers answering
different questions — "which native owner takes over this assurance promise" versus "what happened
to this inherited behavioural obligation" — and the plan defines both. No reconciliation needed;
the apparent conflict with `COHORT_READING_RULES.md` §5 is that the rules file describes the
semantic ledger only. Recorded so no reviewer reads it as drift.

### V-02 · Ten of the eleven bound anchors document a generator that does not exist

Independently re-measured. `tools/ci/verify.py` contains **zero** occurrences of `record-expected`.
Ten of the eleven anchor files nevertheless open with "Generated by `tools/ci/verify.py
--record-expected` from a clean run of that category"; only `diagnostics.txt` names the real
generator, `tools/ci/record_expected.py`.

The anchors are byte-bound in Lead plan section 9, so this must **not** be corrected: rewriting a
header changes the file bytes and breaks the bound SHA-256. It is reported so that nobody later
tries to re-derive an anchor from its own header and concludes the platform is broken. It also
explains the 3-versus-4 header-line difference already visible in the identity counts.

### V-03 · R0's own evidence directory turns an inherited gate red

`change_list.classify()` counts untracked, non-ignored files as `Added`, so the inherited
`tools/identity` suite is red while this work package runs — measured by R0-PY with two data points
(9 untracked → 1240, 10 → 1241, against a documented 1231 for the committed tree).

Nothing tracked is affected and no product file changed. Recorded for honesty and because it is a
concrete instance of `TLP-012`: an assurance tool whose correctness silently depends on the
worktree being pristine. The successor architecture must not inherit that property — a gate that
changes its verdict because someone wrote a file elsewhere is not a gate. It also means any baseline
comparison run against the inherited Python platform is only valid from a clean tree.

### Q-04 · Broker-log assurance has no named successor (R0-PY, F-04)

`assert_one_refusal_per_vhost` and the broker-log capture with digest are **behavioural product
assurances proved from the broker's own log**. Section 12 of the Lead plan names no successor
mechanism for them. This is the highest silent-loss risk R0-PY found and it is exactly what
`GUA-GOV-001` forbids losing without an explicit decision. Travels to the Lead.

### Q-05 · A retained Python engineering tool would have no proof at all (R0-PY, F-07/F-08)

The API-surface tools and the identity detector are the only retention-worthy Python. But section
3.1 requires a retained tool's correctness to be proven by "einen source-eigenen xUnit-/MTP-Testowner
oder eine native MSBuild-Regel", and forbids its Python self-tests. A tool retained under those two
rules simultaneously ends up with no proof whatsoever. Either it is ported, or it is replaced by a
native analyzer, or the rule needs a third branch. Travels to the Lead as a `PROOF_OR_GATE_CHANGE`.

### Q-06 · One tracked Python file lives under `evidence/**` (R0-PY, Q7)

`evidence/WP-F2-SERVICEBUS-CI-BASELINE-03/records/0077/api-surface/collect_assemblies.py` is the
only tracked `.py` outside `tools/`. Section 12.2 item 12 forbids Python test modules "im finalen
Baum", and this file is in the final tree — but it is also frozen evidence of a completed work
package, which `TLP-010` forbids rewriting. The two rules point in opposite directions for exactly
one file. It is not a test module and interprets no test result, so the technically correct answer
is almost certainly "the ban targets executable assurance paths, not frozen historical evidence" —
but stating that myself would be reading a gate rule down, so it travels.

---

## Cross-cohort resolutions

### X-01 · The 16 unbacked anchor identities are backed — two cohorts met from opposite sides

Cohort `R0-CORE-B` reported 16 anchor identities in `ContainerTests/Future_Specs.cs` whose
`[TestFixture]` declarations inherit every test method from base fixtures it could not read, and
placed those bases "under `tests/…/TestFramework/`". Cohort `R0-TF`, working the other direction,
independently identified exactly 16 inherited cases and gave each a full obligation row with its
resolved saga-repository variant list.

Verified by the integrator: there is **no** `tests/ViciOne.ServiceBus.Tests/TestFramework/`
directory — `git ls-files` returns zero paths for it. `ContainerTests/Future_Specs.cs` imports
`TestFramework.ForkJoint.Tests` and `TestFramework.Futures.Tests`, which resolve to
`src/ViciOne.ServiceBus.TestFramework/ForkJoint/Tests/` and
`src/ViciOne.ServiceBus.TestFramework/Futures/Tests/` — inside cohort `R0-TF`'s scope all along.

`R0-CORE-B` had the location wrong and the substance right. The identities are covered by
`OBL-R0-TF-0148…0163`; no gap remains, and the two cohorts corroborate each other rather than
overlap. Recorded because a reviewer reading `R0-CORE-B/RECONCILIATION.md` alone would otherwise
conclude 16 identities were lost.

This also confirms the census had to cross the `src/` boundary: an inherited test estate whose base
fixtures live inside a **packable product project** cannot be enumerated from `tests/**` alone.

### X-02 · Manifest column order — two formats, deliberately not unified now

`COHORT_READING_RULES.md` §2 prescribes `path<TAB>sha256`; the integrator's
`BASELINE_TRACKED_FILE_MANIFEST.tsv` is `sha256<TAB>path`, because it is generated straight from
`git ls-files -z | sort -z | xargs shasum -a 256`, which sorts by path and emits hash first. Three
cohorts flagged the difference.

The cohorts followed the rules file, which is correct behaviour. The integrator's reconciliation
reads both orders and reported **2202 files, zero hash mismatches, zero unknown paths** across all
seven manifests available at that point, so the difference is presentational and has no effect on
the closure proof. Unifying the format now would invalidate manifests that are already produced and
correct, for no gain. The convention is recorded here instead, and the promotion to a single format
belongs in F1 when the evidence tooling is written once.

---

## Cohort R0-SML and R0-BRK intake

### D-05 · Namespace of the new support projects (R0-SML, Q9)

**Measured, not accepted as reported.** `namespace ViciOne.ServiceBus.Testing` is already declared
by **61 files across five shipped product projects** — 54 in `src/ViciOne.ServiceBus`, plus
ActiveMq, AmazonSqs, Azure Service Bus Core and RabbitMq — and carries **62 distinct type names**,
including `ITestHarness`, `BusTestHarness`, `InMemoryTestHarness`, `ConsumerTestHarness`,
`SagaTestHarness`, the whole `*MessageList` / `*Filter` family, and `AsyncTestHarness` itself.

The Lead plan names the **project** `ViciOne.ServiceBus.Testing`; it does not prescribe a root
namespace. By .NET convention those are the same string, and that convention is what creates the
problem: the new non-packable support assembly would merge its types into a namespace that five
**shipped** assemblies already populate.

**Decision: the project keeps the mandated name, and its root namespace is set explicitly so it does
not merge into the product namespace.** Reasons, in order of weight:

1. Section 6 item 7 requires the two `Testing` projects to stay out of every product, package and
   publish graph. A namespace shared with five shipped assemblies works directly against that
   boundary: a reader can no longer tell from the namespace whether a type ships.
2. Any future type-name reuse becomes a `CS0104` ambiguity in every test project that references
   both, and the 62 occupied names live in an assembly this team does not own.
3. No obligation, anchor identity or plan requirement depends on the namespace string.

Today's concrete collision risk is low — the planned members (`TestConfigurationProvider`,
`TestConfigurationValidator`, `TestInfrastructureOptions`, `AzureTestOptions`, `AwsTestOptions`,
`RabbitMqTestOptions`, `SqlTestOptions`) collide with none of the 62. The decision is taken for the
boundary, not to dodge a present error. The measured 62-name list is F1 input regardless of which
namespace is chosen. Class: `IMPLEMENTATION_DETAIL_WITHIN_SLICE`; reported to the Lead as an
observation because it touches the section 6 item 7 boundary.

### V-04 · `client.p12` — a second, independent reading reaches the same facts

Cohort `R0-BRK` re-derived the file's status without access to my disposition and agrees on every
fact: tracked, 2797 bytes, `sha256 ce1d2512…`, PKCS#12 v3 containing a `pkcs8ShroudedKeyBag`, not
referenced by any test, source or build file, not copied to output, its only plausible historic
consumer `Security_Specs.cs` entirely commented out, and deletion from `HEAD` revoking nothing
because the blob is in history from the MassTransit import.

It differs from me on the **conclusion**: it recommends `QUESTION` under the second branch of
section 7, where I recommended removal plus a recorded residual. The disagreement is honest and
narrow — it turns on whether an encrypted key of unreadable provenance counts as "Hinweis auf real
verwendbares Schlüsselmaterial". Both readings go to the Lead unmerged, because picking my own over
an independent second reading would be exactly the kind of quiet resolution the disposition rules
forbid.

### Q-07 · An inherited test can delete a virtual host on a foreign broker (R0-BRK)

`Failure_Specs.DeleteVirtualHost` falls back **silently** to management port `15672` and then issues
`DELETE api/vhosts/<name>`. Combined with the four fixtures that fall back to `guest`/`guest` or
`admin`/`admin` on `localhost`, an inherited test run against a developer machine can destroy a
virtual host that belongs to somebody else. This is the `TLP-008` failure class — destroying a
caller-owned resource — expressed in the broker layer instead of the filesystem.

It changes nothing about the rebuild plan (these fixtures are replaced by owned Testcontainers
fixtures with run-unique names), but it is a live hazard for anyone who runs the inherited estate
for census purposes in the meantime, so it is reported rather than filed.

### Q-08 · Quorum queues are configured and never verified (R0-BRK)

`SetQuorumQueue` appears three times in `JobConsumer_Specs` and `JobDistributionStrategy_Specs`, and
no obligation ever reads `x-queue-type` back. A rewrite that silently dropped quorum configuration
would stay green. This is precisely the `GUA-GOV-001` loss shape, and it is a **gap obligation**, not
an inherited one — the inherited estate never proved it either.

### Q-09 · Only 3 of 466 broker obligations use a run-unique entity name (R0-BRK)

About 184 RabbitMQ obligations execute `DELETE api/vhosts/test` followed by `PUT` in their
`[OneTimeSetUp]`; the sole thing preventing mutual demolition inside one broker is
`[assembly: LevelOfParallelism(1)]`. ActiveMQ has no virtual host at all — its `Clean` deletes every
queue and topic on the broker. Five fixtures already escaped to private virtual hosts, and each of
them documents measured cross-fixture damage.

The design consequence for wave C3 is concrete and is recorded now so it is not rediscovered later:
**one broker per owning fixture** is the only construction that removes both hazards, and the
inherited `[assembly: LevelOfParallelism(1)]` must not be carried over as the isolation mechanism.

### V-05 · `MCA0002` is declared, documented and never reported (R0-SML)

The analyzer declares the diagnostic, describes it and lists it in `SupportedDiagnostics`, but
`ReportDiagnostic` is never called with it, and two anchor identities pin its absence **by name**.
The inherited estate therefore encodes the defect as expected behaviour. Whether `MCA0002` should
start firing is a product decision, so it travels; what R0 records is that the rebuilt analyzer
obligations must not silently inherit "this diagnostic never fires" as if it were a contract.

---

## Cohort R0-PER intake

### D-06 · Azure.Table and DynamoDb identities are new to the anchor set, and stay visibly separate

Measured by the cohort and consistent with the model: **no anchor file carries a single identity for
`Azure.Table` or `DynamoDbIntegration`** — zero matches for their namespaces across all eleven
files. `VERIFICATION_MODEL.json` classifies both as `REAL_EPHEMERAL_CLOUD` with `runs: []`. So they
have inherited **source** but no inherited **evidence**.

**Decision: their 35 identities enter the combined ledger as gap-side obligations and are never
folded into the 3114.** The Lead binds 3114 as the inherited identity total; silently growing it
would destroy the only number a reviewer can check the census against. The frozen level-1 set will
therefore carry two visibly separate counts — inherited-anchored and newly derived — and their sum,
never a single merged figure. Class: `WORK_ORDER_WITHIN_SLICE`.

This also answers the open question `R0-TF/Q-1` from the other direction: an owner deriving inherited
cases without an anchor file is a real state of this repository, not a census defect.

### Q-10 · Two obligations are not provable in any profile — not even against real cloud

`OBL-R0-PER-0523` (DynamoDB TTL deletion) and `OBL-R0-PER-0603` (S3 lifecycle deletion) both depend
on a **service-side sweeper with a delay of up to 48 hours**. No emulator runs the sweeper, and the
real service does not run it inside any bounded test window. They are therefore not `External`
work waiting for wave C4b — they are not provable in `UnitArchitecture`, `LocalIntegration` **or**
`External`.

Section 9 offers no terminal disposition for "the product behaviour is real, observable in
production, and structurally unprovable in a test". Forcing them into `External` would leave two
obligations permanently `NOT_EXECUTED` and block the freeze by design; deleting them would lose a
real product behaviour, which `GUA-GOV-001` forbids. This is the same vocabulary gap as `Q-02` and
travels with it as a `PROOF_OR_GATE_CHANGE`.

The technically honest shape — assert the configuration that instructs the service, and record the
sweeper's effect as a documented limitation rather than a test — is a recommendation, not a decision
Team 1 may take.

### V-06 · Two inherited cases that no mutation can make red

`Container_Specs` for optimistic and pessimistic concurrency have **identical bodies and identically
empty assertions**: swapping the two modes turns nothing red. Alongside them, `EnableSensitiveData`
paths, `Is.Not.Null` on a freshly constructed object (18 EF identities) and 53 declared bodies with
no `Assert.` at all bring the measured total to **86 of 249 inherited identities carrying no
falsifiable claim — 35 %**.

Recorded as the sharpest evidence so far for why the plan forbids count equality as a replacement
proof. A rebuild that reproduced these 86 identities one-for-one would reproduce 86 tests that
cannot fail.

### V-07 · Three product behaviours the inherited estate never observed

Each becomes a gap obligation, and each is the kind that a rewrite would never notice was missing:

1. **No test ever runs a migration**, although every fixture configures `MigrationsAssembly`.
2. **No test observes an isolation level**, although the product uses four and the configurator
   silently downgrades `Serializable` to `ReadCommitted` depending on call order.
3. **`InboxCleanupService` has no test at all.**

Together with the model shape itself being unasserted (keys, unique indexes, foreign keys, column
lengths), these are the highest-value hermetic gaps in the persistence area — provable without any
infrastructure, and currently unproven.

---

## Cohort R0-CORE-C intake

### V-08 · The generated Protobuf descriptor still carries the pre-rename identity

Cohort `R0-CORE-C` reported that `TradeBookedViciOneServiceBus.proto.cs` cannot be reproduced from
the checked-in `.proto`. The integrator's first check appeared to contradict it and was wrong: a
plain text scan of the C# source finds `Events/TradeBookedViciOneServiceBus.proto` and looks
consistent. That is the readable half. Decoding the base64 `FileDescriptor` blob — the serialized
`FileDescriptorProto` protobuf actually uses at runtime — gives:

| Layer | Identity |
|---|---|
| Readable string in the generated C# | `Events/TradeBookedViciOneServiceBus.proto` |
| **Encoded FileDescriptor** | `Events/TradeBookedMT.proto`, `Messages.Events.TradeBookedMT`, `TradeBookedMT.AdditionalDataEntry` |

The file is marked `DO NOT EDIT`. During the MassTransit→ViciOne rename its readable parts were
hand-edited and the descriptor blob was never regenerated.

Two consequences, neither of them cosmetic:

1. **The runtime type identity is still the pre-rename one.** Anything that routes, versions or
   contracts on the protobuf type name sees `Messages.Events.TradeBookedMT`.
2. **Regenerating it is a product-visible change, not a cleanup.** Running `protoc` over the
   checked-in `.proto` would produce a different descriptor name, so "fixing" the inconsistency
   changes wire-level identity. That makes it a `CONTRACT_OR_PUBLIC_API_CHANGE` decision, not
   tidying, and it travels to the Lead rather than being repaired inside the test reconstruction.

Recorded also as a method note: the cohort checked the encoded artefact and the integrator checked
the readable one. The readable one is the half that a rename tool touches, which is exactly why it
proves nothing about generated code.

### Q-11 · `AsyncTestHarness.BeginTestScope()` loses its only caller with the TestFramework

`src/ViciOne.ServiceBus/Testing/AsyncTestHarness.cs` lines 54–56 document that the framework
lifecycle calls `BeginTestScope()` once, "in ViciOne.ServiceBus.TestFramework" — the only occurrence
of `TestFramework` across all 85 files of `src/ViciOne.ServiceBus/Testing/`.

The stale sentence is the editorial correction the plan authorises. The **behavioural** point is
larger and is not editorial: `BeginTestScope()` is what gives each test its own `TestTimeout`
budget, and after F2 it has no caller at all. If the rebuilt xUnit lifecycle does not call it per
test, every test sharing a fixture instance silently shares one budget, and a late test fails for a
reason it did not cause — a false red that looks exactly like a real one.

This is an F1 design obligation, recorded now so it is not discovered as flakiness in C1. Only the
first of the two documented sentences may be edited; the sentence stating that the type carries no
test-framework dependency is a true statement about the product API and stays.

### D-07 · 67 identities move anchor category under the section 5 owner rule

67 identities are bound to the `core` anchor but belong to `ViciOne.ServiceBus.MessagePack.Tests`
under the section 5 owner table, because MessagePack has no dedicated inherited test project.

**Decision: the move is recorded per file and the anchor is not rewritten.** The anchors are
byte-bound; an identity changing owner in the target structure is expected and is exactly what the
owner table produces. What must stay reconcilable is that all 1873 `core` identities remain
accounted for — 67 of them under a different target owner. The frozen level-1 set therefore carries
the target owner per obligation alongside its inherited anchor category, so neither view is lost.
Class: `WORK_ORDER_WITHIN_SLICE`.

---

## D-08 · Profile and disposition normalization before the freeze

A contract check over all ten ledger drafts available at this point returned **2911 rows, 0 malformed
JSON lines, 0 rows with a missing field, 0 rows with an extra field, 0 duplicate `obligationId`s.**
The 19-field contract held across every cohort.

Two vocabularies did **not** hold, and both turn out to be real distinctions rather than sloppiness.

### Profiles — 2155 of 2911 rows canonical, 756 not

The plan defines exactly three: `UnitArchitecture`, `LocalIntegration`, `External`. Twelve distinct
values appeared. Grouped by cause:

| Cause | Rows | Example |
|---|---:|---|
| Two-level hermetic split the plan does not have | 464 | `Unit`, `Component` (R0-CORE-C) |
| Shared capability consumed from several profiles | 116 | `Unit;LocalIntegration;External` (R0-TF) |
| Row is not a test at all | 80 | `<empty>`, `n/a` (R0-PY assurance promises, R0-BLD model promises) |
| Canonical value plus explanatory prose | 6 | `UnitArchitecture (the capability itself is hermetic; …)` |
| Deliberately outside all three | 2 | the two `DIAGNOSTIC_ONLY` measurement scenarios |

The drift exposes that the ledger carries **three row kinds**, which the field set never separated:
an *obligation* belongs to exactly one profile; a *shared capability* belongs to none and is consumed
by obligations that do; an *assurance promise* of a removed tool is not a test at all.

**Decision: normalize with an explicit, auditable mapping, and add a `rowKind` discriminator.**
`Unit` and `Component` → `UnitArchitecture`; canonical-plus-prose → the canonical token with the
prose moved to `notes`; multi-value, empty and `n/a` → `NOT_PROFILE_BOUND` together with
`rowKind = SHARED_CAPABILITY` or `ASSURANCE_PROMISE`.

The cohort files are **not** rewritten. Normalization produces a new merged artefact that carries
`profileAsReported` beside `profile`, so every change is visible and reversible. Rewriting a
cohort's own evidence in place would destroy the independent reading it represents.

### Dispositions — one invented token

Beyond the plan's four terminal and two non-terminal dispositions, and the separate tooling-register
vocabulary already accepted in `D-04`, exactly one token was invented:
`PROPOSED_REMOVED_NO_CAPABILITY` (11 rows, R0-TF, which flagged it itself).

It is not a semantic-ledger disposition at all: it marks a **TestFramework infrastructure file that
carries no obligation**, which section 10 does provide for ("ohne benötigte Fähigkeit → begründet
entfernen"). It becomes `rowKind = INFRASTRUCTURE_FILE` with disposition `REMOVED_WITH_RATIONALE`,
and each of the 11 keeps its technical reason. No obligation is removed by this mapping — that is
the point of separating the row kinds before, not after, the freeze.

### Why this had to happen before the freeze, not after

The frozen level-1 set is what the sentinel compares against for the rest of the work package. A set
that mixes 756 non-canonical profile values with 2155 canonical ones would make the profile-membership
architecture rule (section 12.2 item 1) unimplementable, and every later disagreement about it would
be a re-litigation of the census instead of a fact.

---

## Cohort R0-SQL intake

### V-09 · The 0-of-17 / 0-of-14 pairing score was a heuristic artefact, as suspected

Resolved from the real code, not from the score. Only 3 of 31 provider types are ever named in a
test; the other 28 are reached through extension methods (`UsingPostgres` ×21, `UsingSqlServer` ×8),
through DI (`AddPostgresMigrationHostedService` → `ISqlTransportDatabaseMigrator`) and through a
factory chain of virtual returns (`HostSettings.CreateConnectionContextFactory` →
`ConnectionContextFactory` → `DbConnectionContext` → `ClientContext` → `SqlStatements`). **No
provider file is genuinely unexercised.**

This closes the caveat recorded in `FIND_UNTESTED_SOURCES.md` for two of the five suspect projects
and confirms the caveat itself was necessary: a static pairing score of 0 % meant nothing here.

### V-10 · Product security finding — the account password is interpolated into DDL statement text

Independently re-measured in product code:

```
PostgreSQL  CREATE USER "{1}" WITH PASSWORD '{2}';
SQL Server  CREATE LOGIN {0} WITH PASSWORD = '{1}';
```

**One correction to the cohort's wording, which changes what the fix would have to be.** These are
DDL statements and neither engine accepts parameters for them, so "should have used parameters" is
not available. The two real defects are that a quote character in the password is not escaped, and
that the password reaches the statement text and therefore `pg_stat_activity`, the SQL Server plan
cache and any statement logging.

Product code is outside this package's write scope and product behaviour is unchanged by contract,
so this is **reported, not fixed** — a `PRODUCT_LEGAL_OR_RISK_DECISION` for the Lead and the PO.
Its severity depends on who can supply that password, which is a deployment question R0 cannot
answer from the repository.

### V-11 · Product defects that make inherited tests unfalsifiable on one provider

Four findings in product code, each of which makes a test green for the wrong reason on SQL Server:

1. Five SQL Server stored procedures use `RETURN` where Dapper reads a result set, so `DeleteMessage`
   **always reports false**.
2. `RemoveOrphanedMessages` **never executes** on SQL Server — missing `CommandType.StoredProcedure`.
3. Dead-letter maintenance **never throttles** on SQL Server: its procedure returns nothing, so the
   `null < int` comparison is always false.
4. The dead-letter metric condition is inverted.

These belong to the same class as the finding that optimistic and pessimistic `Container_Specs` are
byte-identical: the inherited estate encodes broken behaviour as expected behaviour. The rebuilt
obligations must assert the intended semantics, and where the product does not deliver it the
result is a **red test and a product finding**, never an assertion trimmed to match.

### Q-12 · An inherited test drops databases that other cohorts are using

`MigrationHostedService.StartAsync` calls `EnsureDeletedAsync`, which **drops the shared transport
database** (carrying the bus outbox) and the two persistence databases used by the job consumer,
seven times per provider. Combined with `R0-BRK`'s virtual-host demolition and the seven fixed
entity names in one shared long-lived database, this is the third independent instance of the same
class: the inherited estate owns shared mutable infrastructure and relies on serial execution to
survive it.

Recorded together so the C2/C3 fixture design answers all three at once with owned, run-scoped
infrastructure rather than three separate patches.

### V-12 · Seven provider differences the rewrite would lose first

The sharpest: `SqlSubscriptionType.Pattern` is a POSIX regular expression (`~`) on PostgreSQL and a
`LIKE` pattern on SQL Server — which is the actual reason the inherited pattern spec is
PostgreSQL-only, a fact that reads like an oversight until the two implementations are compared.
Also: sub-second delays truncate to zero on SQL Server, unlock delay rounds up to one second,
re-declaring a queue clears `auto_delete` on PostgreSQL but keeps it on SQL Server, queue-name
uniqueness is a constraint on PostgreSQL and only a non-unique index on SQL Server, and
`unlock_message` never clears `lock_id` on PostgreSQL, which makes the requeue procedures inert
there.

Every one of these is a behavioural obligation that no anchor identity covers today.

---

## D-09 · `.gitignore` excluded the three contract deliverables

`.gitignore` line 50 carried `.testagent/`, so `.testagent/plan.md`, `.testagent/research.md` and
`.testagent/status.md` — three paths the team manifest names explicitly as write scope, and which
`REQ-TEST-102` requires to bind the Lead-plan SHA-256 and carry the final `Requirement | Evidence`
table — could never enter a commit. A write scope naming files that cannot be committed is
self-contradictory, and a hash binding inside an uncommittable file proves nothing.

`.gitignore` is itself in the write scope, so the resolution is granted. Class:
`WORK_ORDER_WITHIN_SLICE`.

**The first attempt was wrong and is recorded because the error is instructive.** Replacing
`.testagent/` with `.testagent/*` plus three negations produced the intended effect at the
repository root — and silently widened the rule elsewhere. A pattern without an internal slash
matches at **every** depth; `.testagent/*` contains one and is therefore anchored to the root. Two
files of the cancelled `WP-F2-SERVICEBUS-A-PLUS-RECOVERY-03` package, frozen since 19.08.2026 under
`evidence/…/record-0112/.testagent/`, became visible and committable — foreign evidence that
`TLP-010` forbids touching.

The corrected form keeps every level excluded and re-includes only the root, then only the three
named files:

```
**/.testagent/
!/.testagent/
/.testagent/*
!/.testagent/plan.md
!/.testagent/research.md
!/.testagent/status.md
```

Measured, all four cases, rather than reasoned: the three deliverables are trackable, scratch inside
the root directory stays ignored, and the nested directory under prior evidence is ignored again as
before. The lesson is the one `TLP-015` states in general — a control has to be validated at the
boundary that consumes it, not by reading the token.
