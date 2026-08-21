# R0 convergence 05 — R0-C04-01 to R0-C04-04

Under reslice `0014` (`OPT-B-DEFER-TO-C2`, revision 0003, byte-identical to 0002) and
`DIR-A0071-R0-CONVERGENCE-04`. No candidate commit hash appears here. Evidence bound by record
`0013` is untouched.

## R0-C04-01 — the Quartz row is Quartz again, and the API rule has its own obligation

`OBL-R0-PER-0307` is restored byte-for-byte to its pre-addition state; the word "database" no
longer occurs in it. The compatibility-API behaviour moved to a dedicated obligation:

`OBL-R0-BLD-0105` — `UnitArchitecture`, target `tests2/Architecture/ViciOne.ServiceBus.Architecture.Tests`,
owner `tests2/Testing/ViciOne.ServiceBus.Testing`, no variants. It asserts that the canonical product
**and test-support** assembly surfaces, including non-public metadata where visible, carry no type,
member, constant or compatibility API with the removed fixed-database-name identity.

## R0-C04-02 — the EF fixture contract exists now, and is planned for C2

`OBL-R0-PER-0119` — `UnitArchitecture`, target
`tests2/Persistence/ViciOne.ServiceBus.EntityFrameworkCoreIntegration.Tests`, one theory with exactly
the three fixture variants `PostgresTestDbParameters`, `SqlServerTestDbParameters` and
`SqlServerResiliencyTestDbParameters`. Pure name resolution, no database connection. The mapping
entry is no longer `BLOCKED_NO_COHESIVE_CARRIER`; it is bound and non-terminal, like every other
not-yet-implemented R0 obligation.

The Team accepts the correction of its own recommendation: having proved that every existing
candidate would become a mixed test, recommending option A was inconsistent with that proof.

## R0-C04-03 — the consistency table is complete and lossless

`PROOF_MAPPING_TABLE.md` is generated from the effective ledger with all eight required columns and
**no character truncation**. Table delimiters are escaped as `\|` without altering content. All 21
mapping rows were verified to resolve byte-for-byte back to their ledger row; zero mismatches, zero
unresolved obligations, zero unresolved variants.

## R0-C04-04 — the false claims

The blank line at end of file in the previous table is gone. Both claims are now measured rather
than asserted, and `status.md` states that question `0013` was resolved by reslice `0014` through
`OPT-B-DEFER-TO-C2`, naming both new obligations.

## Corrected invariants, measured from the generated ledger and list

| Invariant | Required | Measured |
|---|---:|---:|
| total executable entries | 3667 | 3667 |
| `UnitArchitecture` | 2766 | 2766 |
| `LocalIntegration` | 773 | 773 |
| `External` | 128 | 128 |
| executable target projects | 41 | 41 |
| anchor identities | 3114 | 3114 |
| architecture cohort | 21 | 21 |
| EF Core Unit cohort | 55 | 55 |
| EF Core LocalIntegration cohort | 127 | 127 |

The four new entries are product-gap obligations and add no inherited anchor identity.
