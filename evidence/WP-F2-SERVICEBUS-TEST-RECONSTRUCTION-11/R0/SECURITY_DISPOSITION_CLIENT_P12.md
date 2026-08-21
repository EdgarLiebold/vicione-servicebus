# R0 security disposition — `client.p12`

Requirement `REQ-TEST-105`, Lead plan section 7, governing decision `PO-2026-08-20-05` item 6
(`vicione-architecture/governance/DECISIONS.md`, SHA-256
`91ac626a0ea648209f2cb06a323000665696d0dd1c60775a6730a208fc2a001d`): the file is not migrated;
R0 must prove its actual usage and security status; without product or deployment relation it is
removed as an unused test fixture; on an indication of genuinely usable key material the security
item stops for revocation or rotation; deletion alone may not claim a possible real key as handled.
Baseline commit `ae73c6da748e3bc3257dffa4971ee8680e086207`.
No secret value, password or derived tracking hash of a secret appears in this document.
The file hash below is explicitly required by the Lead plan as identity evidence.

## Subject

| Property | Value |
|---|---|
| Tracked path | `tests/Transports/ViciOne.ServiceBus.RabbitMqTransport.Tests/client.p12` |
| Size | 2797 bytes |
| SHA-256 | `ce1d251211441505ec80d66772213f4d70d2879ddf7c7524f91fabd6e4be7cab` |

## Evidence

### E1 — Container structure (no decryption performed)

`openssl asn1parse -inform DER` over the file shows a PKCS#12 `version 3` structure whose
`pkcs7-data` content carries the object identifier `1.2.840.113549.1.12.10.1.2`
(`pkcs8ShroudedKeyBag`) alongside an `encryptedData` certificate bag, with a SHA-1 MAC and
2048 iterations. The file therefore **does contain password-protected private key material**.
Neither the key nor the certificate metadata was decrypted: the password is not present in
the repository and was not guessed. Subject, issuer, validity and key usage are therefore
**unknown and unprovable from this repository alone**.

### E2 — Provenance

`git log --follow` resolves the file to three commits, the oldest being `9be1da20`
("Import MassTransit v8.5.10 source baseline", 2026-08-06). `MODIFICATIONS.md` line 48 records
"Binary certificate fixture; path renamed, bytes retained." The bytes originate unchanged from
the public upstream MassTransit v8.5.10 distribution. It was never generated for, nor issued
to, ViciOne infrastructure.

### E3 — Reference analysis (TLP-009: text search is candidate discovery only)

| Layer | Result |
|---|---|
| C# source, all of `src/**`, `tests/**`, `benchmarks/**` | no reference to `client.p12` |
| `ViciOne.ServiceBus.RabbitMqTransport.Tests.csproj` | no `Content`, `None`, `EmbeddedResource` or `CopyToOutputDirectory` item for it |
| At the upstream import commit `9be1da20` | `git grep` over `*.cs`, `*.csproj`, `*.md` finds no reference — it was already dead on arrival |
| Remaining references | `CHANGELIST.md`, `MODIFICATIONS.md`, `NOTICE`, `tools/identity/identity_rules.py`, and prior evidence JSON — all path bookkeeping, never a load path |

The file is consequently not deployed, not copied to any build output and not loadable by any
current test. The product's TLS client-certificate capability (`ClientCertificatePath`,
`ClientCertificatePassphrase`, `ClientCertificate` on `RabbitMqHostSettings`) is real and
remains a test obligation, but no inherited test proves it with this file.

## Disposition

`REMOVE_UNUSED_FIXTURE` — the file is deleted with the RabbitMQ cohort. Its removal loses no
test obligation, because no obligation ever loaded it. The TLS configuration obligations found
in `HostConfigurator_Specs.cs` and `ConsumerTimeout_Specs.cs` are dispositioned by cohort
`R0-BRK` and are configuration-level assertions independent of this file.

## Residual fact for Lead disposition

The Lead plan requires a security `QUESTION` for revocation or rotation "on any real-key
indication". The honest state is between the two branches and is reported rather than decided:

- It **is** private key material (E1), so the "no key material present" branch does not apply.
- It is **not** ViciOne-owned key material: it is a publicly published upstream test fixture
  (E2), so there is no ViciOne credential to revoke or rotate.
- Its certificate metadata **cannot be read** without the password (E1), so a real-CA-issued
  certificate cannot be positively excluded from inside this repository.

Deleting it from `HEAD` does not remove it from Git history, and the Lead plan states
explicitly that deletion alone is no proof of revocation.

**Recommendation:** remove from `HEAD` as an unused fixture, and record the residual as a
known-accepted item rather than raising a blocking `QUESTION` — the upstream provenance makes
a ViciOne-side revocation action meaningless. If the Lead prefers certainty over provenance
reasoning, the only closing evidence is an upstream check of the MassTransit v8.5.10
distribution for the same bytes, which is outside this repository and outside the slice.
