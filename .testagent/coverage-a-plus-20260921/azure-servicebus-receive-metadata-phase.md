# Azure Service Bus received transport metadata

## Product defect

The Azure receive context persisted four of the five routing metadata fields that the Azure
send context already restores. It omitted the broker's `ReplyTo` value. A scheduled or otherwise
persisted receive delivery could therefore lose its reply destination, and a message carrying
only `ReplyTo` returned no transport-property bag at all.

`ServiceBusReceiveContext.GetTransportProperties()` now includes nonblank `ReplyTo` under the
existing `ASB-ReplyTo` key. The value comes from the broker-owned SDK property, not from an
application property with the same name. No new metadata key or public API was introduced.

## Hard regressions and review

- `ReceivedRoutingMetadata_SurvivesPersistedReplay` creates an SDK received message with all five
  routing values, including an application property forging `ASB-ReplyTo`. It checks the exact
  received bag and restores it through `AzureServiceBusSendContext.ReadPropertiesFrom` and
  `WritePropertiesTo`, checking every provider value after replay.
- `ReplyDestinationAlone_ProducesTransportProperties` proves that `ReplyTo` alone creates a bag.
- `BlankRoutingMetadata_DoesNotCreateTransportProperties` proves that whitespace-only metadata
  remains absent.

All three tests have requirement projections. Before the source fix, two failed for the product
reason: four entries instead of five, and a null bag for `ReplyTo` alone. The corrected Azure Unit
suite passed 223/223 with Microsoft CodeCoverage. The focused report
`artifacts/coverage-a-plus-20260922-8abfe1e8a/receive-metadata-green.cobertura.xml` has
SHA-256 `0f01ef49800f8d8102b91763c1f3eabc7ff2e4600c8526234f69a2aa184e7e40`.
The method moved from CRAP 156 at zero coverage in the last complete product profile to CRAP 14
with full reported line and branch coverage in this focused Azure Unit report. The direct
read-only adversarial review returned PASS. It found no other Azure metadata key that is
restored by the send context but omitted by this receive method. The replay test checks the
transport-property contract directly; it is not a complete Quartz datastore integration run.

The Microsoft `code-testing-agent` guided the focused red/green test addition, `run-tests`
guided the .NET 10 MTP commands, and `crap-score` guided the targeted method measurement.
The last complete product-wide profile remains `44d9e3254`; global A+ remains open.

## Broader gates on the source/test worktree

The Release Unit/Architecture build completed with zero warnings and errors. Its bounded-parallel
test gate passed 9,992/9,992 without failures or skips. The Azure Service Bus emulator Release
build also completed with zero warnings and errors. The official local emulator fixture passed
28/28 tests with Microsoft CodeCoverage, no skips, and empty fixture findings under run
`vicione-6e37ba8584da`. Its report is
`artifacts/coverage-a-plus-20260922-8abfe1e8a/receive-metadata-emulator.cobertura.xml`,
SHA-256 `05c0f3e8989dd4667f04ed1a4cfa202a8ab0cbf4996fc6d1f541c1546265fbff`.
That emulator profile does not exercise `GetTransportProperties`; the focused Azure Unit profile
above provides the method's direct coverage. Both profiles are retained separately.
