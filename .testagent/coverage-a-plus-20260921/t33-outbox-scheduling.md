# T33: outbox scheduling lifecycle — open

Base: `9bc5ff1dd76b485dbbc120a2a561eaa1387e512b`.
Authoritative complete measurement remains T32 at `791e29af4`.
T32 retains 32 line-gap identities and 127 missed lines in
InMemoryOutboxMessageSchedulerContext. This packet targets product contracts,
not one test per measured identity.

## Tests and assertions

`MessageForms_PreserveScheduleAndCheckpointLifecycleAsync` has 60 cases:
explicit destination, input destination and publish, each with ten typed,
runtime/declared-contract and initializer/pipe forms, each committed or rolled
back to a checkpoint. Real MessageScheduler transforms messages and resolves
publish destinations. A recording provider executes the real send pipe on a
real SendContext and returns distinct controlled tokens.

Assertions check exact contract, body fields, message identity where applicable,
initializer header, typed/untyped pipe effect, due time, destination, caller
token and returned schedule token. The real outbox checkpoint path must cancel
only its own schedule, preserve independent control work and discard the queued
control cancellation. Actual commit runs twice, with exactly one cancellation;
post-commit cancellation executes immediately. Provider inventory is checked
after every lifecycle phase.

`ProviderFailure_PreservesCauseWithoutPhantomCleanupAsync` has 18 cases across
all three routes and typed/declared/initialized forms, with provider exception
or provider cancellation. Failure happens before provider acceptance. Require
the original exception identity or exact provider cancellation token, no phantom
cleanup, retained control work and successful subsequent scheduling/cleanup.

Both methods are mapped to REQ-VSB-INMEMORY-OUTBOX-SCHEDULER in CoreRequirements.
Microsoft code-testing-agent focused workflow and run-tests syntax apply.
The source, existing neighboring tests and helper contracts were manually read;
the plan and its read-only adversarial corrections are in the local
artifacts/t33-outbox-scheduling-plan.md.

## Focused evidence

- Initial lifecycle matrix: 60/60 passed, no skips, exit 0.
- Full class: 78/78 passed, no skips, exit 0.
- Verify-only formatting: exit 0, no changes.
- Read-only adversarial implementation review found no concrete blocker and
  confirmed distinct product oracles rather than forwarding-call assertions.
- Three isolated mutations detected, each exit 2:
  - Invert the tracking guard: 30 rollback cases and 18 recovery cases fail on
    missing cancellation; 30 commit controls pass.
  - Omit checkpoint cancellation discard: 30 rollback cases detect two
    cancellations instead of one; the other 48 cases pass.
  - Drop the explicit-destination typed pipe: exactly route 0/form 1 in both
    lifecycle modes reports the missing typed header; 76 cases pass.
- All mutations manually restored; isolated src diff is clean. Final restored
  control: 78/78 passed, no skips, exit 0.

| Local file | SHA-256 |
| --- | --- |
| artifacts/t33-scheduling-all.log | 67b5b302ea536c6bb35328be51a28233635709775d63ce9a1bec31b7bfafbbf6 |
| artifacts/t33-format.log | e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855 |
| InMemoryOutboxSchedulingLifecycleTests.cs | 3036673729382c61637cd657a937037fcfefda2f18221f3deb610b931a28344a |
| Isolated artifacts/t33-tracking-mutant.log | 4c5e7b906feb23a33d8c6ec7aa68fb3af384540e353fb254503cb9675153456a |
| Isolated artifacts/t33-discard-mutant.log | 4c4e4e685245ebdac9fd73e6009b4d82c5eacae232ea55c2e80178224587aa48 |
| Isolated artifacts/t33-pipe-mutant.log | 223fb8f089ce983e580678239fad14ab2079ab26ba1f91744340abdeb6d28d60 |
| Isolated artifacts/t33-restored.log | 112b781940082e146583192a3a18b8904ea9b558ab9557806f053c909a531348 |

## Limits and remaining gates

Recording-provider evidence proves transformation and outbox lifecycle, not
broker persistence or transport serialization. It proves immediate cancellation
after commit, not newly scheduled work after commit. Provider completion is not
held pending, so these tests do not establish delayed-provider await behavior.
Ordinary full discard and retry checkpoint discard have different lifecycles;
these tests deliberately use the latter for rollback followed by commit.

Finish canonical CHANGELIST, commit,
all 33 fresh profiles, aggregate/delta review and authorized push. No new full
coverage result or A+ acceptance is claimed by this focused pass.
