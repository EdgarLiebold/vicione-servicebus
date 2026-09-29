# T110 — transformation property contracts

Implementation commit: `f34880737`.

Scope: all ten Transformation C# source files were read, with the public
`TransformSpecification` and `TransformFilter` entry points and existing
Transformation tests. Corrections affect `DelegatePropertyProvider`,
`TransformPropertyInitializer` and `TransformPropertyConverter`. No source,
test or comment generator was used.

The original code evaluated a property transform despite an absent source,
let null provider/callback/initializer tasks fail as NullReferenceException,
called a raw provider under a pre-canceled token, and allowed nested conversion
to create and start work under a pre-canceled token. A null nested context also
failed through dereference. The implementation now rejects these states at the
owning boundary. It preserves the actual source metadata and inherited owner
token separately from the operation token passed to the nested initializer.

Eleven cases in ten new xUnit methods exercise absent and present-null input,
full source envelope, distinct cancellation tokens, null tasks, pending
completion, exact fault identity, pre-cancellation and null context. The first
nine cases were 3 pass/6 fail on the unchanged product. The Red Team's added
null-context case failed with NullReferenceException; its pre-canceled case
entered the recording initializer and remained pending, so the red run was
aborted after the causal failure was observed. After correction, the focused
Transformation suite passed 58/58, zero skipped. A zero-warning/error build
preceded the exact-commit complete Core run, which passed 7,071/7,071 with
zero failures or skips. Requirements JSON parsing and `git diff --check` pass.

The first independent read-only Red Team review found two P2 gaps: missing
pre-cancellation/null-context guards at nested conversion, and absent owner
token assertions in envelope tests. Both were corrected with separate-token
and call-count oracles; re-review returned **PASS**, no remaining concrete
P1/P2. Microsoft test-gap-analysis and assertion-quality checks find no
assertion-free or coverage-only new case: failures, side effects, identity,
metadata and timing are observed. The red-first defects and Red Team
counterprobes provide causal evidence for the changed guards.

The latest valid product-wide profile remains T97 at `3cb94a285`:
86,639/93,963 lines (92.20544%), 31,312/36,927 branches (84.79432%),
zero methods with CRAP above 30. This packet did not run a new product-wide
aggregate and makes no current global A+ claim. Per the PO's efficiency
direction, subsequent work uses larger connected behavior packets, focused
tests during edits, one complete affected-project run on a frozen packet, and
the 33-profile Line/Branch/CRAP measurement after roughly five such packets
or earlier for cross-provider/API changes or an A+ acceptance claim.
