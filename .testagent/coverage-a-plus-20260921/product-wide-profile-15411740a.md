# T171 product-wide baseline — `15411740a`

The strict aggregate at `artifacts/t171-aggregate.json` has SHA-256
`26b66610f7ba655fb02520cdf86ae939d3553b7d3d739624c3f614a34a738646`.
It reports `status: complete`, `sameCommit: true`, 33 receipts from
`15411740a5cfd14a263eef48869bf0941dfb2dfc`, all 32 product assemblies,
and 14,210 passing test executions. The bound source tree is
`1f6e2e117a236f1b568e0f2f4df37611ecf15a01`; the test tree is
`706e2f12d8f1b8ce3517c057c8691688fe38b39b`.

| Measure | T171 |
| --- | ---: |
| Covered physical lines | 87,857 / 95,112 (92.37215%) |
| Conservatively covered branches | 32,094 / 37,744 (85.03073%) |
| Method identities | 26,266 |
| Methods with CRAP > 30 | 8 |

The eight reported methods over the CRAP gate are
`MessageSendContext.get_Body` (145.38), `MessageJournalCaptureFactory.CreateSend`
(89.06), `TransportBodyMaterializer.MetadataSnapshot.ChangedField` (54),
`TransportBodyMaterializer.Read` (48.12), `TypedDurableSender.CreateSerializedSend`
(38.15), `PayloadAdmissionTransportBoundary.Admit` (35.58),
`EntityFrameworkScopedBusContext.CreateStagedRecord` (35.41), and
`InMemoryReliableInboxContext.AddSendAsync` (33.87).

This is a historical baseline for the exact T171 commit. Subsequent source and
test edits need a fresh exact-commit profile before the product-wide A+ gate can
be claimed. The conservative branch rate aggregates independent reports and
is not an exact union of branch identities.
