# T174 product-wide profile — `7664edbb0`

The strict aggregate is `artifacts/t174-aggregate.json`, SHA-256
`d2fe924b1e74c2ddfdd89689ca2eedb7ec1a5c783eac7d0a5dc7e628ea7c3ee5`.
It reports `status: complete`, `sameCommit: true`, 33 verified receipts from
`7664edbb00db5751d5e713b676b3982973c078c1`, all 32 product assemblies,
and 14,228 passing test executions. The source tree is
`67180a9d2a99bb36fd586662a5fb3a1edcb1e82a`; the test tree is
`5131bf0d591196b4069613c38e6454aeb5ca5bb0`.

| Measure | T174 |
| --- | ---: |
| Covered physical lines | 88,007 / 95,295 (92.35217%) |
| Conservatively covered branches | 32,186 / 37,832 (85.07613%) |
| Method identities | 26,306 |
| Methods with CRAP > 30 | 4 |

The four remaining methods above the CRAP gate are:

| Method | Covered lines | Complexity | CRAP |
| --- | ---: | ---: | ---: |
| `MessageSendContext.get_Body` | 42/60 | 66 | 183.612 |
| `PayloadAdmissionTransportBoundary.<AdmitCore>b__1` | 7/16 | 16 | 61.5625 |
| `MessageJournalCaptureFactory.CreateSend` | 36/48 | 36 | 56.25 |
| `MetadataSnapshot.ChangedIdentityField` | 7/7 | 36 | 36 |

The unit solution at this commit passed 12,325/12,325, zero failures or
skips. The receipt runner first failed because the sandbox could not reach
NuGet's security-data endpoint (`NU1900`) and the MTP test host could not bind
its local named pipe. The successful exact-commit run used cached packages with
`NuGetAudit=false` only in the measurement environment and authorized local
process access for MTP and broker fixtures. That environment change is not a
security-audit result; security auditing remains a separate gate.

This profile establishes the current ranking, not the final A+ gate. The
conservative branch count is not an exact union of branch identities. Product
and test changes after this commit require a fresh exact-commit profile.
