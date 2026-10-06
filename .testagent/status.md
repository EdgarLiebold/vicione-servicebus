# ServiceBus API repair status

IN_PROGRESS; no whole-API, A+ or release acceptance.
Current:26/35 original findings scoped repaired with independent adversarial reviews and Root acceptance;39 findings confirmed including additional036/037/038/039. F033 Core+SignalR is scoped accepted. F019 corrected32 cases pass,13 original causal reds/19 positives/0 guards,8 strictly compiled single-effect mutants killed with green controls and32baseline/rollback; final adversarial review pending, not yet accepted. Original team FULL4814/6209; exposed semantic symbols CLOSED0. Migration/naming/startup product decisions remain pending only for their affected parts.

Historical checkpoints below are retained; their counts and pending reviews describe earlier stages. Current authoritative per-finding state and immutable acceptance evidence are in SERVICEBUS_API_REVIEW_AND_REPAIR/STATE.json and PACKAGE*_ROOT_ACCEPTANCE.json.


IN_PROGRESS; no whole-API or release acceptance.
Fifteen original findings (005,006,007,008,009,010,011,012,013,014,015,016,017,020,023) have implemented, regression- and mutation-verified, separately adversarially reviewed patches. Integrated final gates remain pending.
Package02 identity: strict production build, five fresh public-package modes, five actual SQLite persistence modes, nine isolated mutants killed and rollback green. Independent review PASS within its frozen scope; unchanged Abstractions 979/979 and Core 7446/7446 native cases pass, zero skipped.
Package01: 424/424 Azure Unit cases,4/4 EventHub probe cases,17/17 single-cause mutants killed; terminal rollback green. Four fresh package-consumer modes pass, actual packages/DLLs hash-bound. The corrected EventHub URI oracle was separately reviewed in Package02 S3.
Package02 S3: original defect red,26/26 complete owner green,3/3 single-cause mutants killed,rollback green,fresh package consumer pass; no confirmed regression in separate review.
The whole API review remains PARTIAL; no exposed symbol has complete semantic closure. Read-only whole-family research continues against the immutable original source.
All checks use actual native receipts; compile failures and zero-discovery attempts are excluded from causal mutant counts. Original source, raw coverage binaries and historical logs remain immutable.
F021 retained-state migration policy is awaiting the PO decision; unrelated corrections continue.
Three additional Azure findings (036,037,038) are confirmed by six originally failing native regression cases with positive controls. F036/F037 repairs now pass five targeted cases; four mutants were killed and independent adversarial review accepted the scoped repair. The complete current Azure owner runs 448 cases: 447 pass, one fails, zero skipped. The remaining F038 automatic subscription-name collision awaits the PO naming/migration choice. Its assertion is retained without weakening or skipping; no whole Azure acceptance is claimed.

Package03 Azure administration lease: 18/18 targeted cases,8/8 causal mutants killed,rollback green, independent adversarial review accepted; shared final gates pending.
Package08 documentation (005,010,020): three XML-only fixes; five fresh public NuGet consumer modes pass,three logical XML member counterreversions killed,rollback green,package/cache/XML/runtime DLL hashes verified. Scoped adversarial review accepted; shared final gates pending.

Package03 EventHub (013,014,016,017): focused70/70 and complete existing owner182/182 native zero skips. Thirty causal mutants killed; fresh final-input R3 nineteen remove the historical Policy binding gap; separate settings eleven and all rollback controls pass. Independent adversarial review accepted exact Freeze827aff, no blockers. Fresh standalone NuGet-only public consumer70 cases is still under validation/review; shared integrated gates remain pending.

EventHub NuGet-only supplement: final70/70cases and exact package/cache/runtime binding independently reviewed ACCEPT_SCOPED (a8d740...). BenchmarkPkg07 actual96nativeOwner/7consumerModes/BDNDry12/9mutants green, adversary pending. F018 two actual ownership predicates corrected; original6causalred10positive vsfreshcorrected16green, mutations/review pending. Original-source FULL3588/6209; no overall A+ or CLOSED-symbol claim.

PACKAGE05 F027/F028/F030: original42=10causalred32positive; corrected freshNuGet42green;17real compiledmutants killed, finalrollback42green, M6209 originalrestored. Freeze2911be2c… assigned independent adversary. F018 freshoriginalsource→pack16 supplement proves6causalred10controls, sourcePDB supplement independently pendingacceptance; canonicalvalidEF410/409/1oldmarkerfixture pendingRoot24/36owner+57/57sharedclosure. Overall review18originalscopedaccepted, noWholeA+.

Aktueller Abschluss Paket16 F019: Root akzeptiert nach unabhängigem Finalreview,32 öffentliche Fälle grün,13 ursprüngliche kausale Fehler/19 Vergleichsfälle,8 kompilierte Einzelfehler erkannt mit positiven Kontrollen und32 Rückbaukontrollen. PACKAGE16_F019_ROOT_ACCEPTANCE.json. Gemeinsamer API-/Profilabschluss bleibt offen.


F029 Package17 ROOT_ACCEPTED_TARGETED_SHARED_FINAL_PENDING: finalpublic26green;original7realterminal-caused Null assertionreds19positive0guards;3strictcompiled singleeffects red+green;baseline26/rollback26;47inputs130effect32receipt4Nugetchains20actualMicrosoft10.0.12bindings11PE-PDB images10435checksum matches independentlyaudited. RootFULL currentwholepublicowner inclgate/queue beforeNative +7original+3mutant failureblocks +finalReviewTXT/JSON. RootAcceptance /Users/edgar.liebold/Downloads/ViciOne Suite 2.0/repositories/vicione-servicebus/evidence/WP-SB-API-REVIEW-REPAIR-20261002/PACKAGE17_F029_ROOT_ACCEPTANCE.json SHA60427461fb2023af441fc905fc379d342024f5e3d87eb044d18ab1219a225e1b. Originalscoped27/35;wholeAPI/CLOSED/A+/canonicalCore766/shared integratedtests stillpending. Historicalcompiler/capacity/ordering+CollectionModified guards excluded.
