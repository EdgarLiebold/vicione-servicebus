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
- UnitArchitecture: 453 total, 453 passed, 0 failed, 0 skipped;
- LocalIntegration: 3 total, 3 passed, 0 failed, 0 skipped;
- Release builds: 0 warnings, 0 errors;
- product C# behavior and inherited-test source: unchanged;
- locked restores, bounded formatter/analyzer gates, mapping closure, and full static review pass;
- targeted mutation review: passed for Abstractions and the Analyzer foundation, including product
  behavior, code-fix output, requirement projection, omitted tests, fail-closed Roslyn compilation,
  the local host-derived worker-id contract, message-contract scenario closure, exact diagnostics,
  exact CodeFix output, and required source forms.

## Remaining program

All other source owners, database/broker/cloud profiles, inherited-stack deletion, TestFramework
removal, and the atomic `tests2` to `tests` promotion remain open. No current result represents those
future cohorts as complete.
