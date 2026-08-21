# Plan — F1b native requirement binding

F1a is accepted. F1b adds one small architecture reference cohort without changing product code,
migrating inherited behavior, deleting old tests, or creating another runner or verdict path.

## Implementation

1. Add a passive, framework-neutral `RequirementCoverageAttribute`.
2. Embed the immutable Lead-owned six-entry architecture projection in the architecture-test
   assembly.
3. Compare the projection with attributed compiled xUnit methods in one ordinary `[Fact]`.
4. Add one real ArchUnitNET core rule for the Abstractions-to-Core dependency direction.
5. Keep MTP as the sole process-verdict owner and xUnit as the sole test-verdict owner.

The projection contains only requirement ID, variant key, simple assembly name, `Type.FullName`,
and `MethodInfo.Name`. It is compared ordinally without normalization. R0 and
`VERIFICATION_MODEL.json` are never loaded or reproduced.

## Verification

The Lead alone runs locked restore, zero-warning Release builds, the unfiltered Unit profile,
format checks, static test-quality review, isolated one-cause mutations, and two independent
read-only reviews of one frozen commit and tree. The native lower bound rises from 89 to 91 so the
new comparison test and architecture rule cannot disappear behind the accepted F1a count.

F1b stops after this reference chain is accepted. Behavioral migration and inherited-test deletion
remain outside this checkpoint.
