# Azure Service Bus subscription topology admission · 20.09.2026

Product and test commit: `ed2ba8205563bb0f6511d155ba8f8f785a034ca8`. The complete changed source and test files were read manually. The source area also included the complete topic, queue, subscription, collection, topology, publish-builder, consume-specification, and broker-reconciliation paths before edits. No comments were changed by script.

## Product defects corrected

- Subscription options, initial rules, SQL parameters, and correlation filters were retained by reference. Later caller or getter mutation could change the stored broker declaration. The entity now owns and exposes defensive SDK snapshots.
- Subscription equality missed status and rule/filter differences. Matching broker names could silently reuse incompatible declarations. The comparison now covers the request-bearing options and rules, while the stable hash uses broker identity.
- Forwarding collections treated the destination as part of the broker subscription name, so one `(topic, subscription)` could target multiple destinations. Direct, queue-forwarding, and topic-forwarding collections could also reuse that broker identity across relationship kinds. The builder now rejects both conflicts.
- Subscription options could name a different source topic or forwarding destination than the supplied handles. The builder now validates the source and copies the canonical destination into a previously unset `ForwardTo` without mutating caller options. Null, empty, and whitespace inputs are covered.
- Destination partitioning depended on declaration order and could change before a conflicting relationship was rejected. The builder propagates partitioning through already declared forwarding relationships after successful admission and terminates on cyclic graphs.
- Topic, queue, subscription, and relationship indexes were case-sensitive although Azure resource names are case-insensitive. All broker-identity comparisons and hashes now use one ordinal-ignore-case rule.
- Session-enabled queue or subscription sources with autoforwarding were admitted although Azure rejects them. Direct and relationship builder paths now fail before topology state changes.

The tests use the public builder, assert the retained broker state or exact conflict, and were observed red before each corresponding fix. The 18 new requirement variants are mapped in `AzureServiceBusRequirements.json`; the projection test passes. Microsoft `dotnet/skills` used: `code-testing-agent` before test edits, `run-tests` for .NET 10 MTP commands, `test-gap-analysis` and `assertion-quality` for behavioral counterchecks, and `coverage-analysis` for Cobertura/CRAP review. Read-only adversarial review found the cross-kind identity, wrong target, late partitioning, case, blank target, and session-source cases; the exact-commit review returned PASS without a concrete blocker.

## Exact-commit gates

- Locked restore: successful for Azure Service Bus unit and local integration projects.
- Release unit suite on `ed2ba8205`: **142/142 passed**, zero failed, zero skipped. Cobertura SHA-256: `797e57e398e741b2ca2128b0dd3045601f531561fbd10a1e99911ca4cce6ecaf`.
- In that unit report, the topology builder has **91/91 lines**, **29/30 branches**, max CRAP **14**. The eight edited topology source files each have max CRAP below **30**; `SubscriptionEntity` max is **23.06**. This one-project report does not constitute a product-wide provider union.
- Canonical Service Bus emulator runner on the exact commit: **25/25 passed**, zero failed, zero skipped. Run `vicione-4a46ce35e5d8` has empty `fixture-findings.json`; broker log SHA-256 `5ff833d122711c241e053977cc4b946efe456f06f9f25110325d69353db97310`.
- First-read checker at this commit: **4,211 current `src` paths**, 4,113 Git-convention read paths, 98 current-byte-attested raw remainder paths, and no unproven first-read paths. This is the user's first-read convention, not a claim that the detailed A+ review is complete.

The broader provider-union line/branch/CRAP and public-API A+ gates remain open. The adjacent `ServiceBusConnectionContext.CreateTopicSubscriptionAsync` reconciliation path updates only a subset of changed subscription options on an existing broker entity; it is the next separate product area for review and tests.
