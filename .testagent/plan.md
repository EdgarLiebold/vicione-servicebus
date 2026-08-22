# Plan — C1a Abstractions MessageBody contract

## Frozen target

Implement exactly 17 new parameterless Facts under
`tests2/Core/ViciOne.ServiceBus.Abstractions.Tests`: 13 inherited replacements, two Base64
hardening cases, one concrete-type-set Assurance case, and one projection-comparison Assurance
case. The unfiltered Unit lower bound is fixed before implementation at `91 + 17 = 108`.

The Lead-owned files below are the sole detailed truth and must be byte/hash bound in the active
assignment:

- `C1A_ABSTRACTIONS_MESSAGE_BODY_IMPLEMENTATION_PLAN.md`;
- `C1A_R0_MIGRATION_PROJECTION.json`;
- `C1A_REQUIREMENT_COVERAGE_PROJECTION.json`.

## Implementation order

1. Extract the existing F1b five-field comparison into the specified framework-neutral verifier;
   keep one ordinary xUnit assertion as the only verdict.
2. Refactor the existing F1b Fact to that verifier without changing its six-entry semantics.
3. Add the executable Abstractions test project, lock file, Unit/Engineering membership, and real
   evaluated-graph rules.
4. Add the six source-parallel test files with exactly the 17 named Facts and embed the immutable
   C1a requirement projection.
5. Update native CI minimum to 108 and update `docs/build.md` in English.
6. Stop for Lead execution. Only the Lead may run restore, build, test, format, or any other .NET or
   MSBuild process.

## Acceptance

- product and inherited-test bytes unchanged;
- exact 51-to-13 R0 closure and exact 16 attributed-method projection;
- 17 new Facts discovered, green, and not skipped; unfiltered total at least 108;
- accepted F1a/F1b behavior unchanged;
- all specified C1a mutations and all six accepted F1b mutations fail from their own cause;
- full assertion, anti-pattern, smell, gap, graph, package, lockfile, and two-review closure;
- one clean technical commit/tree, evidence child, and remote ref.

Do not delete any inherited test file or change product behavior in C1a.
