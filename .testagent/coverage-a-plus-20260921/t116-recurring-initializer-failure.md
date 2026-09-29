# T116 — Recurring initializer failure isolation

## Frozen packet

Implementation/test commit: `d71be83ce29b89bf492e2f896fd4abe17fc04b8f`.
Source tree: `0230be407c3400a3f35f8611502a91d042c9b447` (unchanged from T115).
Test tree: `34405f2669954f702e73c28234e8dd717d9e2584`.

Both recurring scheduler implementations have separate initializer paths for
no pipe, typed pipe and untyped pipe. The new test covers all twelve
combinations of scheduler command transport (endpoint/publish), destination
(explicit send/topology publish) and pipe form. On a property-read failure it
requires the identical exception instance, zero endpoint lookup, zero
commands and zero pipe effects; topology lookup occurs only when the publish
destination needs it. The same input then recovers and must produce one
command with the exact schedule, destination, payload, caller token and
appropriate pipe effect. Product source was not changed.

## Verification and adversarial review

- Focused final test: 12/12 passed, no failures or skips, on the frozen
  source/test bytes. Release test build passed with zero warnings/errors.
- Controlled counterchange: inserted an early endpoint resolution into only
  the no-pipe `EndpointRecurringMessageScheduler` initializer path. The new
  matrix failed exactly that case (11 passed, 1 failed), reporting the
  unexpected provider lookup. The counterchange was removed; `git diff`
  confirms no product-source change and the final focused run passed 12/12.
- Independent read-only Red Team initially found a P2 gap: the first matrix
  omitted the no-pipe overloads. Expansion from 8 to 12 cases closed it;
  the second review reported PASS with no remaining concrete P1/P2.
- Complete Core project: 7,111/7,111 passed, no failures or skips, on the
  frozen commit. The requirement mapping JSON parses and `git diff --check`
  passes.

Microsoft `code-testing-agent` Research → Plan → Implement was applied inline.
The existing Roslyn `find-untested-sources` pairing served as a static pointer;
`test-gap-analysis` and `assertion-quality` drove the exact-exception,
zero-effect and recovery oracles. `run-tests` supplied the SDK 10/MTP filter
syntax. A new coverage/CRAP measurement was deliberately deferred to the
agreed larger-packet checkpoint; T114 remains the latest complete 33-profile
measurement and global Line/Branch/CRAP A+ remains open.
