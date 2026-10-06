# ServiceBus API repair plan

All phases remain incomplete until their exact native validation and adversarial patch review pass.

| Package | Findings | Regression obligations |
|---|---|---|
| 01 Azure client and credential boundaries | 007,008,011,015 | Both SDK delays; delay boundaries; external/internal/mixed owners and terminal shutdown; live session deadline; serialized secret-free probe and unchanged SDK URI |
| 02 Persistent identity and message data | 006,009,023 | Reserved S3 prefix; strict UTF16/UTF8 roundtrip; retained valid Unicode and persistent contract behavior |
| 03 EventHubs and request cancellation | 012,013,014,016,017 | Linked CTS active through completion; complete close under faults; correct timer boundaries; private settings snapshot; caller-owned unresolved waits |
| 04 Persistence | 018,021 | Actual DbContext transaction ownership; stable inbox namespace; independent bus owners; explicit retained-state migration disposition |
| 05 Core diagnostics and time | 004,019,022,027,028,029,030 | DI meter/logger lifetime; failed activity admission cleanup; readonly results; timer range; backward UTC and valid batch controls |
| 06 Endpoint and resource ownership | 031,032,033,034,035 | All endpoint contributions; lease-owned fallback filters; original operation and cleanup faults; activity-specific endpoint lookup; private definition owner preserving Bind equality |
| 07 Benchmarks | 024,025,026 | Valid mediator limits and JSON headers; unique consumed-ID observations; actual BDN Dry |
| 08 Contracts and gates | 001,002,003,005,010,020 | Actual completion XML; clean Unit prerequisite; reviewed final package API baseline; Empty Address and untrusted lookup XML; retained MultiBus DbContext guard |

Per package: inspect complete source/test closure → author regression → confirm original failing oracle → apply production fix → focused native validation → one-cause isolated countermutants → freeze exact code/test patch → separate adversarial review → address findings and refreeze if necessary.
Build/provider operations are sequential. Read-only research may proceed in parallel.
Final: strict shared profiles and package consumers, complete API-review ledger closure, integrated adversarial review, PO report.
No optional upgrade, unrelated modernization, weakened oracle, skip, polling-as-success or baseline update during diagnosis.

Package09 newly researched Azure paths: first compile and execute all six candidate oracles against unchanged causal product sources; promote only confirmed failures. Correct default formatter propagation and retained subscription validation if causal proof holds. Resource naming compatibility for nested queue destinations requires its own source/history/consumer assessment before choosing a naming fix. Each new code/test/projection delta requires exact freeze,mutants and adversarial review.

Package03 F012: install canonical ServiceBusSharedAdministrationLeaseTests (four theories /18cases); build strictly and execute originalclass (expected8red10green). Then four await-within-using production fixes, green18; four direct-return mutants plus linked-caller/lease and disposal/fault controls; actual rollback. Add four existing RequirementCoverage projections after Package09 shared projection review completes. Unfiltered Azure owner remains F038-red pending PO. Freeze all deltas and separate adversarial reviewer who did not author F012 tests.

Package08 docs subset: five fresh consumer modes. Empty checks exact empty exception, HasValue guard, deferred populated null, actual inline-only value and stored URI; packed XML must state guard/exception. Pre-auth authenticates untampered positive payload then positively records tampered selector key-B before exact AuthenticationTagMismatchException; packed callback XML must mark selector untrusted before authentication. EF single/separate positives and same DbContext negative normal named-bus registrations; packed XML must describe one DbContext type per bus. Execute immutable old packages first (expectedthreecausalXMLreds/twoEFpositivegreens), then fresh strictlybuiltpacked candidate, then three onecausepacked-XML counterreversions and rollback. Separate freeze/reviewer for every three-source/consumerdelta; integrated gates pending.

## Package03 EventHub implementation
Research→Plan→Implement inline under existing native signed owner. Install three reviewed fixtures and exactly ten requirement rows; 70 cases. First strict-build and run original closing10,policy28,producer pre-canceled8+positive16 (62 cases); later-canceled8 reserved for corrected end state to avoid deadline-based original proof. Four source fixes: always-await closing with ordered aggregate, validate provider limits and preserve inherited guards/whole-ms timer boundary, copy nine policy properties and options callback for completed ordinary Build, WaitAsync per caller in four deferred paths. Assert exact faults/token/state/route identity/SDK callback/options and normal public DI, release and join all gated operations. Then corrected70,mutants,fresh packages,whole owner,independent frozen adversarial reviewer and final integrated gates. No cloud-start or standalone-lazy-blob closure claimed.

## Package07 plan
Implement three narrow source fixes from hash-verified historical candidates. Author standalone public compiled-DLL oracle after this Research/Plan, strict rebuild both original/corrected tools, assert all parameter tuple results and latency identity/count/completion/timestamp semantics including gated concurrent duplicates. Execute real BDN Dry on corrected source, restore one-cause mutants, native rollback; bind actual compiled tools, packages, lock/cache/runtime and raw BDN reports. Freeze all changes for independent adversarial reviewer. Final whole integrated profiles remain pending.

## Package04 F018 public transaction ownership Plan

Root personally read both bounded NuGet-only consumers, all25 public-route imports and complete factory before authoring. Native canonical36-file EF owner remains untouched/not personally FULL; no whole-owner grade inferred. See evidence PACKAGE04_F018_PLAN.json and ROOT_PUBLIC_READ_CLOSURE. Original16cases precede two-predicate product correction and fresh packed consumer/mutation proof.

2026-10-03T01:12:45.741603+00:00 PACKAGE05 F027/F028/F030: Root personal FULL22 public route sources+six bounded consumer inputs completed; 42-case NuGet-only regression plan bound in evidence/PACKAGE05_PLAN.json before copying/authoring. No canonical Core owner closure or edits claimed.

F018 canonical fixture author delta: Root personally FULL36 owner+57 shared/imports/CI completed before edit; actual SQLite outer transaction and borrowed same DbContext replace fake marker. Expected executionCount1 retained; no additional transaction + same actual ID/reference. Plan evidence PACKAGE04_F018_CANONICAL_FIXTURE_PLAN.json; valid complete UnitArchitecture410 rerun and independent adversary mandatory.


Package05 F030 bus diagnostic supplemental plan.md: see /Users/edgar.liebold/Downloads/ViciOne Suite 2.0/repositories/vicione-servicebus/evidence/WP-SB-API-REVIEW-REPAIR-20261002/PACKAGE05_F030_DIAGNOSTIC_OWNER_PLAN.json. Eight new isolated NuGet scenarios, typed two-bus same-endpoint grouped/ungrouped, old diagnostic red and valid controls, exact bus identity string; startup PO decision remains open. No canonical Core edits.


Package10 F022 trace owner repair: /Users/edgar.liebold/Downloads/ViciOne Suite 2.0/repositories/vicione-servicebus/evidence/WP-SB-API-REVIEW-REPAIR-20261002/PACKAGE10_F022_SOURCE_PLAN.json. Root8sourcesFULL, retain+disposeownActivity on poststart formatterfault in admission and delivery; native public proof plan10cases, real InMemory worker/receiver/store, Fault/Cancellation controls, compiled mutants and independent adversary required.


Package11–16 continuation (current evidence navigation; actual preauthor plans remain immutable in evidence): F004 journal scope Package11; F039 separate root graph Package12; F033 Core Package13; F031 endpoint contributions Package14; F033 SignalR Package15; F019 logger origin/lifetime Package16. Each has its own source/public-owner preauthor plan and complete bounded owner closure, actual selected NuGet consumers, compiled single-effect counterproofs and independent frozen review. No canonical Core766/Abstractions117/Bench15 owner closure transfer. The current ledger/acceptance truth is SERVICEBUS_API_REVIEW_AND_REPAIR/STATE.json and each PACKAGE*_ROOT_ACCEPTANCE.json.
Package16 authoritative preauthor Research→Plan: PACKAGE16_F019_SOURCE_PLAN.json and PACKAGE16_F019_PUBLIC_OWNER_PLAN.json; final owner-before-native read R5.32 fresh-process cases cover real Generic Host/default/dynamic/explicit/two owners, messages, exact logging markers, disposal, borrowed identity, sibling flows, fault/pre-cancel/meter controls. Eight one-effect strict variants; store actual tasks, release gates and attempt all cleanup; guard failure is never PASS or universal quiescence. Freeze45inputs/240effects/44receipts/4selectedpackagechains/34Microsoftbindings now awaits final independent review. Only then mark scoped finding repaired; all integrated/API final gates remain pending.

Paket17 F029: Matrixdefault/typed Size/TimeFromFirst/TimeFromLast ±UTC → Matrix; Forcedhandle2 → Matrix; rawequal2 → Matrix; realSystem → SystemSize; firstprovider → ProviderSnapshot; dedup → Dedup; cancelmember/sole → Cancel; primary/cleanup exactorder → Faults; separateelapsed/UTC timerhelper → Helper. GleicheOriginal-/KorrekturAssertions, strikteBuilds/fresh selectedNuget/CoreAbsLockCacheDLL/PDB;3gezielteSingleEffects+positiveControls/baseline/rollback;jedeSource/Publicowner/runnerÄnderung unabhängigesadversarialReview; gemeinsame Abschlussprofile bleiben offen.


F035 package18 planned boundedowner30:22 known-definition controls and8 source-derived endpoint lifetime qualification controls. Ordinary default/typed actualDI InMemory real2business+2terminal; Bind type/equality/lastalias/repeatedlazy; actual disposalcounts/gates/DIdependencyorder/borrowedvalue/exactsolefault. NoNativeyet; source plan choice privateunkeyedowner variants vs unique key actualDIowned definition still research. BeforeNative Rootwholefreshconsumer incllock, independentadversarialeachcodechange, then original/fixed/strictsingleeffect+positive/rollback boundpackages/loadedPE-PDB. Global wholeAPIreview+A+ andcanonicaltests pending.


Package18 F035 R2: 46 bounded public cases per PACKAGE18_F035_PUBLIC_OWNER_PLAN_R2.json; known AddDefinition ownership plus endpoint sibling qualification, caller-owned controls, asynchronous gate/dependency order, default alias baseline and optional disposed-capture. Root whole owner eight files read before any compiler/native behavior gate; lock is pending restore. Previous and corrected receive the same oracle bodies. No canonical Core test owner authority inferred.


Paket19 F034: Research→Plan vor Autorenschaft, PACKAGE19_F034_PREAUTHOR_PLAN.json. Root37 Quellen persönlich FULL/3 eigene exakte Paket18-Reuse;32 geplante öffentliche Fälle, sameNuGet-only owner/oracles, finiteEndpointmultiset vorStart, echte konkrete RoutingSlip-Completion und inverse Kompensation; default/typed/mixed/caller/custom-selector/definition/repeated/cancellation controls. Jede Source-/Owner-/Runneränderung adversarial; Native original/fixed/compiled-singlecause+positive/rollback, dann Freeze+Finalreview. Keine kanonischen Coretests bearbeitet/benotet.

Package19 separate escaped transport candidate:10 public default/typed finite actual topology identity plus canonical/short queue sends. Original/current F034 ownership cases remain unchanged. No timeout failure is a causal qualifier. See PACKAGE19_TRANSPORT_PREAUTHOR_PLAN.json. Root implementation and whole owner reread; independent adversarial preflight before behavior.


Paket19 R4 owner refinement: fresh paired generation retains34 cases; business InputAddress last entity decoded exactly once; definition expected multisets include intentionally separately registered fluent endpoints; full finite callback multisets printed before exact assertions. No source repair credited to these fixture corrections. See PACKAGE19_F034_OWNER_REFINEMENT_PREAUTHOR_PLAN_R4.json. Original R3 owners/packages/evidence preserved.


Paket19 R5: Same41paired publiccases per PACKAGE19_F034_F041_OWNER_PREAUTHOR_PLAN_R5.json and PAired_CAUSALITY_PLAN. F034 baseline uses same final Core/Abs with old Courier only. Eight F041 default/typed divergent success+inversecompensation/literal/aligned controls, public lower configure callback exact captured buscontext+registeredProbe identity and13/3 QoS; strict3/5/6 finite endpointmultisets. Core F04014separatefrozenmatrix remains immutable. Native/regression/singleeffect mutations/freeze/final independent review required.
