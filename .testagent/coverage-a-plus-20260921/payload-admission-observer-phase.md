# Payload admission after send observers

## Product defects

`PayloadAdmissionTransportBoundary.Admit` checked that a send context was a physical transport
context only after `GetOrAddPayload` attached its admission marker. A rejected `SendContextProxy`
therefore left the marker on its underlying transport context, potentially causing a later send
to appear owned by another bus. The type check now runs before marker creation.

Event Hubs admitted a single send or every batch member before invoking `PreSendAsync`, but did
not recheck admission after observers completed. An observer could change `ContentType` while the
SDK event builder later emitted that changed value with bytes serialized by the original
serializer. Both producer paths now revalidate after `PreSendAsync`; the batch path validates
every context before entering the provider send path. An unreachable null check on the already
constructed batch array was removed.

## Behavioral regression evidence

- Five Core boundary cases exercise early serialization, another bus's marker, a rejected
  non-transport proxy, and absent or mismatched content type. They assert specific failures and
  that rejection does not serialize or attach unwanted state. The proxy side-effect assertion
  produced one real product failure out of five before the fix; all five passed afterward.
- The initial two Event Hubs local-emulator cases mutated content type in a `PreSend` observer
  for a single message or the second of two batch messages. Both ran against the actual provider
  path and failed before the fix because no exception was thrown. Both passed afterward with the
  exact `ConfigurationException`, observed mutation, pre/fault notifications, and no post
  notification. The red-team review then added a first-batch-member case and moved observer
  mutation after an asynchronous continuation; these later changes were not part of that red
  phase. The tests assert rejection; they do not directly count SDK provider calls.
- The full Core suite passed 6,267/6,267 with Microsoft CodeCoverage. The focused report is
  `artifacts/coverage-a-plus-20260922-8abfe1e8a/payload-boundary-core-green.cobertura.xml`,
  SHA-256 `c7be3180736c9f5eee137541c14dc1246f38847780c7099254b15d2bdb630ce3`.
  `Admit` moved from baseline CRAP 128.99 to 23.31 in this Core report, with 86.05% lines and
  81.82% branches.
- The final expanded Event Hubs local-emulator suite passed 53/53 with Microsoft CodeCoverage,
  zero skips, and empty Azurite/Event Hubs fixture findings, run `vicione-ed5bb797f9b6`. Its
  report is `artifacts/coverage-a-plus-20260922-8abfe1e8a/eventhubs-full-final4.cobertura.xml`,
  SHA-256 `5451502d61a6c9c30616f97d68932cfca9be0f8d7020fb6a6962634d1b87916b`.
  The single producer method moved from baseline CRAP 36.10 to 24.02, with 96.55% lines and
  70.83% branches. The batch method moved from 41.04 to 28.16, with 94.12% lines and 78.57%
  branches.

An earlier Event Hubs rerun passed 51/52; an existing two-rider test timed out waiting
for its first consumer. Its isolated retry received `EntityNotFound` for `multibus-eh1` immediately
after the emulator reported ready, although the entity is in the fixture configuration. The next
complete fixture run passed 52/52, before the first-member theory case was added. These emulator
startup symptoms are recorded as test-environment observations, not counted as green runs.

The first complete Unit/Architecture run passed 9,996/9,997; its sole failure was the
source-pattern architecture rule expecting the old single-send expression. The rule now checks
both admission calls around awaited observers in single and batch paths. A zero-warning Release
rebuild and its focused architecture check passed 4/4. The final complete Unit/Architecture gate
passed 9,997/9,997 with zero failures and skips after a zero-warning Release solution build.

The initial adversarial read-only re-review returned PASS after the test names and requirement
projection were narrowed to the behavior actually asserted. Its next review found that the batch
test omitted the first member and the architecture test did not check awaited observers; both
checks were added. Final read-only adversarial re-review returned PASS. Microsoft
`code-testing-agent` guided
the focused cases, `run-tests` guided .NET 10 MTP commands, and `coverage-analysis` and
`crap-score` guided hotspot selection and measurement. The last complete product-wide profile
remains `44d9e3254`; global A+ remains open.

## Exact source/test commit

Commit `0ddaa99282337cc50ba961a53d765b99cad95b89` was checked in a clean detached worktree
with no tracked source or test diff. Locked restores of the Core and Event Hubs test projects and
the Unit and complete Engineering solutions succeeded. The Release Unit solution and Event Hubs
test-project builds had zero warnings and errors. After the complete Engineering restore, the
exact Unit/Architecture gate passed 9,997/9,997, with no skips. Core Unit passed 6,267/6,267
with Microsoft CodeCoverage.
The Event Hubs local emulator passed 53/53 with Microsoft CodeCoverage, no skips, and empty
fixture findings, run `vicione-1261396b2900`.

The exact Core report is
`artifacts/coverage-a-plus-20260922-8abfe1e8a/exact-0ddaa9928/payload-admission-exact-0ddaa9928-core.cobertura.xml`,
SHA-256 `7d69b0fd142c92ba4c1bd21935e29f4b65da3c37ba5956403dc652c6ce7b18c7`.
It measures `Admit` at 37/43 lines, 81.82% branches, complexity 22, and CRAP 23.31.
The exact Event Hubs report is
`artifacts/coverage-a-plus-20260922-8abfe1e8a/exact-0ddaa9928/payload-admission-exact-0ddaa9928-eventhubs.cobertura.xml`,
SHA-256 `e7b288fbd7ede9dd57f9c19cbac125c734192e1dae738092880e0b610a61d606`.
It measures the single send at 28/29 lines, 70.83% branches, CRAP 24.02, and batch send at
32/34 lines, 78.57% branches, CRAP 28.16. The exact fixture findings are retained beside the
reports.

The first isolated Unit/Architecture run passed 9,996/9,997: its one architecture failure
evaluated an MTP property on an unrelated, not yet restored LocalIntegration project. The
property was empty before the complete Engineering restore and `true` afterward. A filtered
repeat passed 1/1. One sandboxed filtered attempt stopped before test execution because .NET's
named-pipe host could not bind its socket; the successful repeat and complete gate ran outside
that restriction. These diagnostic attempts are not counted as passing gates.
