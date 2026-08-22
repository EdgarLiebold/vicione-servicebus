# Status — native test reconstruction

## Preserved baseline

The accepted native foundation and MessageBody behavior are committed and remotely preserved at
`b3519291c65026fd3714929d9b721dc4e52ea01d`.

## Current source state

- the remainder of the Abstractions unit behavior is reconstructed in the source-owner project;
- host-dependent worker-id behavior is isolated in an independent LocalIntegration project;
- the framework-neutral Roslyn host and all 115 inherited Analyzer obligations are reconstructed;
- Analyzer behavior and CodeFix behavior have separate source-owner projects;
- the 91 message-contract obligations are preserved as 48 canonical scenarios and 91 native xUnit
  cases; applicable fixes run as separate native CodeFix cases;
- all 26 executing inherited SignalR behaviors are reconstructed without the inherited TestFramework;
- five greenfield SignalR hardening cases cover multi-target, empty-target, and MessagePack paths;
- all 67 inherited MessagePack obligations plus six mixed-fixture body obligations are replaced by 49
  source-owner cases covering direct serialization, body accessors, architecture, configuration,
  in-memory dispatch, and delayed redelivery;
- all nine inherited state-machine-visualizer obligations are replaced by seven source-owner behavior
  cases plus one requirement-projection case, covering exact Graphviz and Mermaid output, declarative,
  dynamic, request-derived, empty, and composite-event graphs;
- all 58 inherited Cron-expression obligations are replaced by deterministic source-owner cases;
  repeated spaces and tabs now preserve field alignment instead of silently shifting the schedule;
- all 17 inherited endpoint-name formatter obligations are replaced by 17 source-owner cases under
  `Configuration/EndpointNaming`, with literal naming oracles and exact reserved-type failures;
- 15 inherited/current-product MessageUrn obligations are mapped to 17 source-owner cases under the
  Abstractions root and `Attributes`; both runtime overloads now share input validation;
- all seven request-rate algorithm obligations are mapped to eight deterministic source-owner cases;
- UnitArchitecture: 670 predeclared total; acceptance run pending;
- LocalIntegration: 3 total, 3 passed, 0 failed, 0 skipped;
- Release builds: 0 warnings, 0 errors;
- inherited-test source: the fully replaced Abstractions NUnit project, endpoint-name, MessageUrn,
  and request-rate fixtures have been removed; the Abstractions compile-only usage examples now
  live in the non-packable `samples/OrderWorkflow` project; `MessageType_Specs.cs` remains because
  its array transport behavior is still open;
- locked restores, bounded formatter/analyzer gates, mapping closure, and full static review pass;
- targeted mutation review: passed for Abstractions, the Analyzer foundation, MessagePack, and the
  state-machine visualizer, including product
  behavior, code-fix output, requirement projection, omitted tests, fail-closed Roslyn compilation,
  the local host-derived worker-id contract, message-contract scenario closure, exact diagnostics,
  exact CodeFix output, and required source forms.
- the Cron cohort independently rejects its product regression, a missing requirement projection
  row, and removal of its test project from the Unit profile.
- the endpoint-name formatter cohort passed its focused 170-case project run, all four targeted
  product mutations, and its then-current 635-case unfiltered UnitArchitecture profile;
- the MessageUrn cohort passed its focused 99-case project run, all five targeted product
  mutations, and its then-current 652-case unfiltered UnitArchitecture profile;
- the request-rate cohort passed three consecutive focused 107-case runs, all six targeted product
  mutations, and its 660-case unfiltered UnitArchitecture profile;
- the reflection cohort adds eight source-owner cases, corrects the invalid missing-key gap, and
  leaves the ambiguous implemented-message-type obligation open for its topology cohort;
- the repository-wide retrospective [product-defect accommodation audit](../evidence/WP-F2-SERVICEBUS-TEST-RECONSTRUCTION-12/RETROSPECTIVE_PRODUCT_DEFECT_ACCOMMODATION_AUDIT.md)
  found no weakened, skipped, filtered, or defect-accommodating test in the reconstructed scope; it
  explicitly excludes not-yet-reconstructed product areas from that verdict.

## Remaining program

All other source owners, database/broker/cloud profiles, inherited-stack deletion, TestFramework
removal, and the atomic `tests2` to `tests` promotion remain open. No current result represents those
future cohorts as complete.
