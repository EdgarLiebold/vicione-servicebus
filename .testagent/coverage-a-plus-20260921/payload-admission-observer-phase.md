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
