# Plan — C1a Abstractions MessageBody contract

## Frozen target

Implement exactly 17 new behavior and assurance Facts under
`tests2/ViciOne.ServiceBus.Abstractions.Tests`: 13 inherited replacements, two Base64
hardening cases, one concrete-type-set Assurance case, and one projection-comparison Assurance
case. Add 33 direct verifier-infrastructure cases and the project-driven architecture rows. The
unfiltered Unit lower bound is fixed at the completely reconciled cohort result of `146`.

The shared projection verifier is completed together with ordinary tests in
`tests2/Testing/ViciOne.ServiceBus.Tests.Infrastructure.Tests`. No test-framework behavior enters
the framework-neutral infrastructure library.

The Lead-owned files below are the sole detailed truth and must be byte/hash bound in the active
assignment:

- `C1A_ABSTRACTIONS_MESSAGE_BODY_IMPLEMENTATION_PLAN.md`;
- `C1A_R0_MIGRATION_PROJECTION.json`;
- `C1A_REQUIREMENT_COVERAGE_PROJECTION.json`.

## Implementation order

1. Extract the existing five-field comparison into the specified framework-neutral verifier;
   keep one ordinary xUnit assertion as the only verdict.
2. Refactor the existing architecture requirement Fact to that verifier without changing its
   six-entry semantics.
3. Add a dedicated executable infrastructure-test project and prove the verifier positively,
   negatively, and against false-green inputs.
4. Add the executable Abstractions test project, lock file, Unit/Engineering membership, and real
   evaluated-graph rules.
5. Add the six source-parallel test files with exactly the 17 named Facts and embed the immutable
   MessageBody requirement projection.
6. Update the native CI minimum to 146 and update `docs/build.md` in English.
7. Only the Lead runs restore, build, test, format, mutation, or any other .NET/MSBuild process.

## Acceptance

- product and inherited-test bytes unchanged;
- exact 51-to-13 R0 closure and exact 16 attributed-method projection;
- 17 new behavior/assurance Facts and 33 infrastructure-verifier cases discovered, green, and not
  skipped; the project-driven graph expansion remains present; unfiltered total at least 146;
- accepted foundation behavior unchanged;
- all specified MessageBody and architecture-foundation mutations fail from their own cause;
- full assertion, anti-pattern, smell, gap, graph, package, lockfile, and two-review closure;
- one clean technical commit/tree, evidence child, and remote ref.

Do not delete any inherited test file or change product behavior in this cohort.
