# ServiceBus API repair research

PO instruction: "füre den review zuende - erstelle einen für OP verständlichen bericht in stichpunkte und behebe alle Fehler / Findings vollständig A+ und führe danach einen Review aller behbenen Fehler aus.Prfe jede Codeänderung mit einem adversal Reviewer. Arbeite in effizienten Paketen"

Baseline: commit `5afd0d077788f594b268a5ac441e788d54d57a70`, tree `a69acad1df9167725c06e2dd127b7f927bbe4298`.
The original read-only review and its immutable source snapshot remain at `/private/tmp/vicione-servicebus-api-review-20261001`.
Its 35 confirmed findings and prior counterexamples are evidence, not production fixes or final assurance.
The original 37 fully read normative inputs were hash-revalidated before the new explicit maintenance order was appended.

Use the pinned Microsoft skills from revision `2124a6e3518b2120cda1c076d62ed79491b00693`.
The code-testing-generator capability is unavailable; execute Research → Plan → Implement with bounded read-only research support and sequential implementation/build ownership.
Reuse `SOURCE_TEST_PAIRING.json`; do not rerun project-wide pairing discovery.
Tests use xunit.v3.mtp-v2 4.0.0, Microsoft.Testing.Platform, net10.0, central locked packages and the existing infrastructure helpers.
Read the complete tracked target test-project closure before editing its tests; record hashes and distinguish helper lexical assertions from executed assertions.

Acceptance checklist:

- Complete file, exposed-symbol, package, XML, skill and contract closure; unreviewed entries stay open.
- Resolve every confirmed finding; isolated causal patches require source and compatibility review before production use.
- Add external behavioral oracles and positive controls; include fault, cancellation, boundary, ownership and concurrency cases as applicable.
- Every code change receives a separate read-only adversarial review bound to its exact patch hash. Changes invalidate that review.
- Run native focused tests while iterating; final required shared profiles, strict Release builds, locked restore, pack and package consumers run after integration.
- Perform a final adversarial review of the complete integrated repair and close every resulting finding.
- Produce a plain German bullet report for the PO, separating demonstrated fixes from pending evidence.

Package 01 targets F-SB-API-007, 008, 011 and 015: both SDK retry delays, caller-owned versus created messaging clients, current session expiration, credential-free checkpoint diagnostic URI.
SDK constraints: messaging base delay 1 ms..5 min inclusive. Administration has no IDisposable lifetime. Preserve the public owned-context constructor and caller-supplied client options.
Session renewal remains SDK-owned. Diagnostic projection must preserve the real SDK credential URI and remove query, fragment and userinfo only from emitted metadata.
The existing EventHubs test owner is a LocalIntegration project; do not classify its provider-dependent profile as hermetic Unit.

Product decision pending: safe migration of pre-existing CLR-derived EF Inbox identities requires explicit bus-owner mapping; generic legacy fallback must not suppress independent bus owners.

Package09 Azure topology research: existing complete38-file test-owner and35shared-input closure reused by sourcehash; new fixture derives from fully read REGRESSION_RESEARCH.cs and actual default formatter,subscription specification,named/typed topology,sendfilter andnormalDI-builder paths. Four methods/six cases include functioning typed formatter,empty formatter retention,rule-only/filter-only configuration,and distinct destination admission controls. The immutable-view candidate is omitted pending actual public-contract disposition. No wholeAPIclosure is claimed.

Package03 F012 research: Root FULL existing Azure owner/shared closure retained; independent helper fresh FULL76 has separate READ_CLOSURE. Four public shared administration routes incorrectly dispose linked cancellation registrations before inner task completion. New fixture uses actual public wrappers and gated ConnectionContext SPI, real SDK model properties, exact method/options/rule/filter/result/exception/token identities; no broker started. Eight pending cancellation cases, four result controls, four fault controls, two existing awaited-send controls. Source signature stays Task/Task<T>; custom SPI synchronous throw will now be observed as faulted Task, consistent with awaited SDK implementation.

Package08 docs subset F005/F010/F020: Root FULL current public IMessageData/empty/deferred/inline handles, MessageDataExtensions/MessageDataPolicy, IEncryptionKeyProvider/AEScomplete path and existing public pre-auth consumer, EF registration XML and existing same/separate/single public registration consumer. These corrections preserve runtime/signatures/schema and address shipped XML contradictions. Fresh own consumer is an isolated three-product-package graph (EF/Core/Abstractions); no canonical Core/Abstractions test edits or false whole owner acceptance.

## Package03 EventHub F013/F014/F016/F017
Root personally reread the final three fixture drafts and four causal production sources; prior personal native owner and 35 shared files are hash-reused, 11 additional CI/import inputs read fully in current phase. PACKAGE03_EVENTHUB_LEAD_READ_CLOSURE.json binds 73 distinct current inputs. Read-only research dossier is supporting analysis, not transferred personal full-read evidence. No new project or dependencies.

EventHub native setup corrections: initial Policy draft omitted Azure.Messaging.EventHubs using (strict CS0246 build excluded). Original policy run9 F014 causal reds and10controls; its other9 failures only duplicate ReceivePipe setup, excluded from F016 proof. Closing mutant reversed aggregation revealed teardown rethrow masking body assertion; after joining, teardown records completed fault while body retains exact outcome/order assertions. R2 reruns all causal mutants on this corrected regression hash. No TimeoutException is treated as a kill.

F016 setup correction: Root fully read public registration factory/specification/host observation adapter. It captures exactly one context from normal production Build through public ConnectReceiveEndpointContext forwarding, preserving exact explicit MessageLimits payload before delegating; no private field locator or second context generation. Consumer asserts one context and exact MessageLimits identity. Four fixtures now belong to the existing native owner; new helper personally FULL read. Mutation r2 retains its startup fixture bytes and excludes these generation methods; no concurrent mutation of M.

## Package07 research
Three original benchmark sources and exact historical causal dossiers personally FULL read. F024 lacks both required mediator Limits; F025 passes null JSON Headers; F026 counts every callback and yields duplicate joins. Preserve all product guards and public signatures. Standalone compiled-tool/public-package oracle owned and fully read by Root; no canonical Benchmark.Tests edit or transferred whole-owner grade. Microsoft microbenchmarking skill and project/run, writing, comparison references applied; Dry verifies execution only.

## Package04 F018 public transaction ownership Research

Root personally read both bounded NuGet-only consumers, all25 public-route imports and complete factory before authoring. Native canonical36-file EF owner remains untouched/not personally FULL; no whole-owner grade inferred. See evidence PACKAGE04_F018_PLAN.json and ROOT_PUBLIC_READ_CLOSURE. Original16cases precede two-predicate product correction and fresh packed consumer/mutation proof.

2026-10-03T01:12:45.741603+00:00 PACKAGE05 F027/F028/F030: Root personal FULL22 public route sources+six bounded consumer inputs completed; 42-case NuGet-only regression plan bound in evidence/PACKAGE05_PLAN.json before copying/authoring. No canonical Core owner closure or edits claimed.

F018 canonical fixture author delta: Root personally FULL36 owner+57 shared/imports/CI completed before edit; actual SQLite outer transaction and borrowed same DbContext replace fake marker. Expected executionCount1 retained; no additional transaction + same actual ID/reference. Plan evidence PACKAGE04_F018_CANONICAL_FIXTURE_PLAN.json; valid complete UnitArchitecture410 rerun and independent adversary mandatory.


Package05 F030 bus diagnostic supplemental research.md: see /Users/edgar.liebold/Downloads/ViciOne Suite 2.0/repositories/vicione-servicebus/evidence/WP-SB-API-REVIEW-REPAIR-20261002/PACKAGE05_F030_DIAGNOSTIC_OWNER_PLAN.json. Eight new isolated NuGet scenarios, typed two-bus same-endpoint grouped/ungrouped, old diagnostic red and valid controls, exact bus identity string; startup PO decision remains open. No canonical Core edits.


Package10 F022 trace owner repair: /Users/edgar.liebold/Downloads/ViciOne Suite 2.0/repositories/vicione-servicebus/evidence/WP-SB-API-REVIEW-REPAIR-20261002/PACKAGE10_F022_SOURCE_PLAN.json. Root8sourcesFULL, retain+disposeownActivity on poststart formatterfault in admission and delivery; native public proof plan10cases, real InMemory worker/receiver/store, Fault/Cancellation controls, compiled mutants and independent adversary required.


Package11–16 continuation (current evidence navigation; actual preauthor plans remain immutable in evidence): F004 journal scope Package11; F039 separate root graph Package12; F033 Core Package13; F031 endpoint contributions Package14; F033 SignalR Package15; F019 logger origin/lifetime Package16. Each has its own source/public-owner preauthor plan and complete bounded owner closure, actual selected NuGet consumers, compiled single-effect counterproofs and independent frozen review. No canonical Core766/Abstractions117/Bench15 owner closure transfer. The current ledger/acceptance truth is SERVICEBUS_API_REVIEW_AND_REPAIR/STATE.json and each PACKAGE*_ROOT_ACCEPTANCE.json.
Package16 Root personally FULL47 current sources before two-file authoring (44 fresh/3 exact own reuse), plus complete new8 authored bodies/34-entry lock and all fresh corrections before Native. Additional actual publicHandlerExtensions and four integrated frame sources read FULL. Automatic factory-origin follows Messages/category, immutable identity refresh preserves explicit usable context and explicit-null/no-service identity. Actual Microsoft Logging/Hosting/DI10.0.12 runtime is package/cache/copied DLL bound; exact SDK source remains unread. Research support is readonly.

Paket17 F029: vor Rootautorschaft dokumentierter Source/Publicownerplan E/PACKAGE17_F029_SOURCE_OWNER_PLAN.json SHA25a984f63d437849a64a480915f00c18e72c8fb85e17c1b35a81df837f30ed0c. 66 Rootpersönliche Produkt-/PublicrouteFULL; kein Transfer von Core766. RohUTC-Reihenfolgeguard ist kausaler Fehler, relativeTimer vorhanden. Neuer begrenzter öffentlicher owner mit 26 benannten Fällen (Matrix12,Forced2,equal2,System/providerSnapshot/dedup,cancel2,faultpolicy4,clockhelper).


F035 package18 preauthor research: Root20 bounded public/product/SPI bodies FULL (16fresh4exactownReuse), actualDI10.0.12 ServiceProviderEngineScope239lines primaryweb FULLtext (no pinnedDLL/raw-source hash claim). ResourcehiddeninsideordinaryBind untracked. Parameterizedendpoint sibling UNVERIFIED. New publicOwner30names inventory E/PACKAGE18_F035_PUBLIC_OWNER_PLAN.json, all explicit userrequirements remain global checklist. No canonicalCore766 owner transfer.


Paket19 F034: Research→Plan vor Autorenschaft, PACKAGE19_F034_PREAUTHOR_PLAN.json. Root37 Quellen persönlich FULL/3 eigene exakte Paket18-Reuse;32 geplante öffentliche Fälle, sameNuGet-only owner/oracles, finiteEndpointmultiset vorStart, echte konkrete RoutingSlip-Completion und inverse Kompensation; default/typed/mixed/caller/custom-selector/definition/repeated/cancellation controls. Jede Source-/Owner-/Runneränderung adversarial; Native original/fixed/compiled-singlecause+positive/rollback, dann Freeze+Finalreview. Keine kanonischen Coretests bearbeitet/benotet.
