# Iteration 190 status

Status: admitted locally; commit, annotated tag and publication checkpoint pending.

- Scope: 23 current saga repository/query/policy source files, 1,314 physical lines.
- Personal-read coverage: 533/4,118 current source files (12.943%).
- Product corrections: explicit constructor/argument boundaries, deterministic null-task/null-result
  failures, cancellation preflight for cached loads, parameter-identity-safe query/state expression
  rewriting, and transparent task/fault/cancellation forwarding.
- New evidence: 35 unique requirement variants, 35 test methods and 56 expanded cases.
- Focused classes: 6/6, 18/18 and 32/32 passed; Saga namespace regression 83/83 passed.
- Complete Core: 5,174/5,174 passed, 0 failed, 0 skipped.
- EF unit: 249/249 passed; Core/EF/EF-local requirement projections: 1/1 each.
- Release builds: Core test, EF unit and EF-local projects passed with 0 warnings and 0 errors.
- Mutation proof: 8/8 compiled isolated single-cause mutants killed and restored.
- Admitted coverage: 268/269 executable lines; 89/94 branches; maximum method CRAP 6.
- Remaining dispositions: one visitor-invariant throw line, one reflection-impossible property arm,
  two expression-visitor invariant arms and one private unused negation arm.
- Final Cobertura SHA-256:
  `b157144af7775c7d479164c1c3010b4424ecf6cc75d91a30363bb53db437a3ef`.
- Core requirements SHA-256:
  `a01d2af3e91315e230a0633bd374db9d6c21ead2f741b9f07baa8ab3d5daab14`.
- Sorted display-name SHA-256 across 5,174 tests:
  `3ef33df56a1c6ff93c5bf2cb842e0bca42d458912fbf77a62dc4f3c0efe61175`.
- Source manifest / chain:
  `63b534372331e20d640574311e816e0bc6d53c28b97c67345f679375584fc410` /
  `3fadd7497731910cda193f51a18282dc1d1c8d615011e78200c49dc5bd332b52`.
- Test manifest / chain:
  `ac99dbbed650369dde95a892d83771aab460ff5ee1c25e5b5522954592d0176b` /
  `66bcfafcc04e21b207163014aa150d694fa37f3945e83921cd86af9ecbd948dd`.

Remote publication remains queued until the security gate receives exact confirmation for the
destination and payload. Work continues locally without pausing the active goal.
