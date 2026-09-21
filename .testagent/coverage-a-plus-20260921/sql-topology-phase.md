# Generic SQL topology A+ slice

## Product defects fixed

- Queue and topic subscriptions compared nested topology entities by object reference. Equivalent
  declarations created as separate objects were therefore treated as different subscriptions.
  Equality and hash codes now use the corresponding logical queue and topic comparers.
- SQL broker diagnostics omitted a queue's `MaxDeliveryCount`, hiding a broker-relevant setting.
- Public SQL publish-topology extensions dereferenced a missing configurator or message-type
  collection instead of rejecting it at the API boundary.
- An explicit type sequence containing a later null entry could register earlier entries before
  failing downstream. The sequence is now completely validated before the first registration.

## Hard behavior evidence

- SQL Unit/Contract project: 147/147 passed.
- Complete Release Unit/Architecture profile: 9,797/9,797 passed, 0 failed, 0 skipped.
- Release build in the complete run: 0 warnings and 0 errors.
- Requirement projections cover logical identity and hash compatibility, every identity field,
  exact case sensitivity, complete diagnostic keys and values, both namespace-scan branches,
  invalid contracts, namespace isolation, callback identity, input ordering, and atomic failures.

## Coverage and CRAP

Fresh focused report:
`artifacts/coverage-a-plus-20260921-sql-topology-reviewed`.

- Generic SQL focused lines: 876/1,969.
- Generic SQL focused branches: 368/787.
- All six topology hotspots selected from the global baseline are below CRAP 30.

| Baseline hotspot | Baseline CRAP | Reviewed CRAP | Reviewed coverage |
| --- | ---: | ---: | --- |
| `QueueEntity.QueueEntityEqualityComparer.Equals` | 272 | 16 | 100% lines, 14/16 branches |
| `QueueSubscriptionEntityEqualityComparer.Equals` | 210 | 14 | 100% lines, 14/14 branches |
| `TopicSubscriptionEntityEqualityComparer.Equals` | 210 | 14 | 100% lines, 14/14 branches |
| `AddPublishMessageTypesFromNamespaceContaining` | 156 | 12.11 | 90.91% lines, 10/12 branches |
| `TopicEntity.NameEqualityComparer.Equals` | 72 | 8 | 100% lines, 8/8 branches |
| `SqlBrokerTopology.Probe` | 72 | 8 | 100% lines, 8/8 branches |

The focused report observes five assemblies. It does not replace the next fresh 32-assembly
product-wide aggregate. The remaining configuration, receiver, PostgreSQL, and SQL Server hotspots
stay open for the next SQL slice.

## Microsoft grade-tests assessment

All nine added tests were graded against the xUnit guidance from the Microsoft `dotnet/skills`
test-analysis extension. Distribution: **9 A, 0 B, 0 C, 0 D, 0 F**. The accompanying
test-anti-pattern review found 0 Critical, 0 High, 0 Medium, and 0 Low findings.

| Test | Grade | Note |
| --- | --- | --- |
| `QueueComparer_UsesExactTypeNameIdleTimeoutAndDeliveryLimit` | A | Equality, hash, null, subtype, and every broker setting have distinct controls. |
| `NameComparers_UseExactTypeAndNameOnly` | A | Deliberate lifecycle exclusion, subtype safety, nulls, and case-sensitive names are distinguished. |
| `QueueSubscriptionComparer_UsesLogicalEntitiesTypeAndRoutingContract` | A | Separate equivalent objects and every nested queue, topic, type, and routing discriminator are checked. |
| `TopicSubscriptionComparer_UsesLogicalEntitiesTypeAndRoutingContract` | A | Separate equivalent objects, hash compatibility, nulls, type, endpoints, and routing are checked. |
| `BrokerProbe_ProjectsEveryEntityAndBrokerSetting` | A | Exact scopes, keys, types, values, null omission, and all entity kinds are asserted. |
| `NamespaceScan_PublishesOnlyValidFilteredContractsAndInvokesCallbacks` | A | Valid contracts, a classified invalid delegate, an open generic, filter, and callbacks are distinguished. |
| `NamespaceScan_WithoutFilterStillRejectsInvalidContracts` | A | The normal scan path proves validity filtering and excludes a valid type from another namespace. |
| `ExplicitTypes_PreserveOrderAndCallbackIdentity` | A | Exact caller order and callback identity are asserted. |
| `Registration_RejectsEveryMissingRequiredInput` | A | Exact parameters and zero partial registration prove public, atomic failure behavior. |

## Pseudo-mutation discriminators

- Restoring object-reference comparison fails the separate-equivalent-object assertions.
- Replacing the nested queue comparer with a name-only comparer fails the lifecycle controls.
- Omitting any entity, identity field, diagnostic field, or adding an unexpected probe field fails
  the exact negative or key-set assertion.
- Using case-insensitive entity names fails the case-only controls.
- Removing message-contract validation admits the closed delegate and fails both scan paths.
- Scanning the complete assembly admits `SqlAddressTests` from another namespace and fails the
  namespace-boundary assertion.
- Removing prevalidation, validating during registration, or accepting null entries fails the
  exception parameter and empty-recorder assertions.
- Reordering types, skipping callbacks, or passing the wrong callback type fails exact sequence
  assertions.

## Adversarial review

The read-only review first found the null-entry partial-registration defect and four test-oracle
gaps: a candidate excluded too early by type classification, an untested default scan branch,
missing case-only name controls, and a missing namespace-boundary control. Product and tests were
corrected after each finding. The final read-only re-review returned **PASS with no further concrete
findings**.
