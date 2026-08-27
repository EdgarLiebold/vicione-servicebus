# Entity Framework Core / cache cleanup correction 05

## Frozen subject

- Technical commit: `763de920e75e1e3c8f342ca0693be4cd02c9afcb`
- Technical tree: `5c85b31535bdee19aa2acec1d86cb12001904006`
- Parent: `6faa01140ffc2a614785096bf573edd58071c394`
- Authorizing architecture commit: `e9624a1909c349118089f6b92d3259ebe4742422`

The correction closes the two cache defects found by the independent review of correction 04. It
does not change the accepted EF Core retry contract, its 90-to-156 terminal disposition, or any
provider boundary.

## Product result

`NodeTracker<TValue>` now keeps cleanup ownership explicit and single-flight:

1. While a reserved cleanup is queued, producers may segment values only while an unused ring
   bucket remains. They never wrap onto a live bucket.
2. Scheduling occurs outside the tracker lock. A scheduler accepts the callback by returning
   `true`; rejection (`false`) or an exception keeps ownership with the caller and executes the
   reserved pass inline.
3. Callback execution is idempotent, including the hostile case in which a scheduler invokes the
   callback and then throws.
4. A requested follow-up remains reserved across the scheduler handoff, so a reentrant producer
   cannot create a second cleanup owner.
5. Observer failure cannot strand a reservation that was already published by the state machine.

The production constructor still uses the thread pool. The injectable `TrySchedule` boundary is
internal test support only; it does not expand the public API.

## Positive execution

All commands ran from the repository root at the frozen technical commit.

- Locked Engineering restore: exit `0`.
- Engineering Release build: exit `0`, `0` warnings, `0` errors.
- Unfiltered Unit/Architecture profile: `1895/1895`, `0` failed, `0` skipped.
- Focused cache-capacity class: `14/14`, `0` failed, `0` skipped; CTRF is bound.
- LocalIntegration against fresh run-scoped PostgreSQL and Azurite resources: `70/70`, `0`
  failed, `0` skipped; run identity `vicione-4c5b4d94a34e`.

The first manual LocalIntegration invocation intentionally remains under `diagnostics/`: it omitted
the workflow-owned `VICIONE_TESTS__Profile=LocalIntegration` setting and failed closed before using
credentials or endpoints. No product or test source was changed to make the corrected invocation
pass.

## Mutation closure

`MUTATION_MANIFEST.json` binds four independently reconstructed exact replacements. Each mutant
built successfully and its owning test failed with exit `2` for the expected cause:

- M16: live ring bucket overwrite — 32 values expected, 29 visible;
- M17: rejected/throwing scheduler loses ownership — all four variants fail bounded convergence;
- M18: follow-up reservation released before handoff — three schedulers observed instead of two;
- M19: observer failure strands the reservation — expected queued cleanup is absent.

The target file was restored after every run to SHA-256
`8e27b1577ada29bbddb89e9b111ff6a3ae31a4f638374a51d0ea82bf03af7675`, which is the byte content
in the frozen technical tree. The mutation worktree is clean.

## Superseded diagnostic labels

Correction 04 is immutable. Its `FAIL_CLOSED_DIAGNOSTICS.md` omitted the compression suffix in two
display labels. The canonical paths, already present and hash-bound there, are:

- `positive/fail-closed-stale-workflow-floor.log.gz`
- `positive/fail-closed-lost-cleanup-signal.log.gz`

This section supersedes only those two labels; it does not rewrite or reinterpret the earlier raw
evidence.
