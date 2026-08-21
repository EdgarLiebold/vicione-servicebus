# R0 — frozen research result

Wave R0 of `WP-F2-SERVICEBUS-TEST-RECONSTRUCTION-11`. Baseline commit
`ae73c6da748e3bc3257dffa4971ee8680e086207`, tree `e5897e7632be4f491e01d51221ee59081d4d2aa0`,
branch `test/servicebus-xunit4-mtp2-a-plus-v2`, worktree clean at first edit.

This document binds every R0 artefact by SHA-256. It is the level-1 binding the Lead record
attaches to: after acceptance, a ledger line may only change through a new checkpoint with a new
hash and commit.

## Census closure

| Fact | Value |
|---|---|
| Tracked files at the baseline | 5745, each bound by SHA-256 |
| Git paths minus read paths, and the reverse | both empty |
| Files read in depth by the twelve cohorts | 3377, every hash equal to the baseline manifest |
| Inherited identity anchors | 11 files, every path, SHA-256 and count equal to the Lead plan |
| Anchor identities | **3114**, and every one mapped to at least one obligation — 0 unmapped |
| Ledger rows | 3664, 0 malformed, 0 missing fields, 0 duplicate obligation ids |

## The ledger

| Row kind | Rows | Meaning |
|---|---:|---|
| `OBLIGATION` | 3367 | a behavioural test obligation; belongs to exactly one profile |
| `INFRASTRUCTURE_FILE` | 147 | a TestFramework file carrying no obligation |
| `ASSURANCE_PROMISE` | 116 | a promise of the removed Python platform, mapped to a native owner |
| `SHARED_CAPABILITY` | 34 | consumed by obligations in several profiles, in none itself |

Of the 3367 obligations, **2869 are backed by an inherited anchor identity** and **498 are newly
derived** from current product behaviour that the inherited estate never proved. The two counts are
kept separate and never merged into one figure, so the Lead-bound 3114 stays checkable.

## The frozen obligation set (level 1)

3672 entries of `obligationId` plus `variantKey`, 0 duplicates.

| Profile | Entries |
|---|---:|
| `UnitArchitecture` | 2766 |
| `LocalIntegration` | 778 |
| `External` | 128 |

## Artefacts

| Artefact | SHA-256 |
|---|---|
| `BASELINE_TRACKED_FILE_MANIFEST.tsv` | `39ee60242b912022eedb97bbf221f821e56e55717d0ebe23c7d0068fa9a9c6fe` |
| `COMBINED_SEMANTIC_LEDGER.jsonl` | `9bffccc6e15cf59aba7c060188919f4992630e6409c9ac1efb630551e5361836` |
| `OBLIGATION_SET_FROZEN.tsv` | `cd8d0cf22fbb4b63ad7aaf5e4dd7635a6506c7f39135636434fd465a74374609` |
| `ANCHOR_IDENTITY_TO_OBLIGATION.tsv` | `4c7f95cf761cd31b03f93a82f5f0dc75095f7a1ef189e4e8e775867ffb0df39a` |
| `SOURCE_OWNER_MAP.json` | `b17072bf490ee5c14ae274a901c15c0eb65012c14bc6c46df79638da116cb2e2` |
| `GIT_BASELINE_STATE.json` | `8da23732634d097ac320f3c44aab7efa6d8af5e71c793de80e29fd05cbcfc4a8` |
| `COHORT_READING_RULES.md` | `b3cb38fd24d2bd51a7ed716dcefda20b20e398292f3debca64b8ab7a47ccc0b1` |
| `FIND_UNTESTED_SOURCES.md` | `d49b1d35d5b419714aec9c668d204f151a6d0e953630b639ea48d54b039652b1` |
| `SKILL_BINDING.md` | `a14e26b12e13909ad1e3c9e7461dab2226a59fd0a029352ce27f1685a24beeb4` |
| `MTP_COMMAND_FORM_PROBE.md` | `fba52e3e91b5c633576639a03d89cc457cd2ea07c66cc070e6d84707bbccd9d4` |
| `SENTINEL_FEASIBILITY_PROBE.md` | `2cafbe83316f890165fc5cd032b372a430bcf63ee41bdd6bbb7b9b779eb31600` |
| `PROVENANCE_FLAG_PROBE.md` | `6d1340bf111f0d10b40fd288d444a26dfe374a32a0cb975f39db03fe020e6bbb` |
| `PARENT_IMPORT_PROBE.md` | `29c1dfa57c632c01fc73b7408256a4d5b0ee84fcf0e5f751dadddb9f2829e577` |
| `SECURITY_DISPOSITION_CLIENT_P12.md` | `998dea97516db53527226a2ac8447a862d4295c0de6f4b4540509ca38c1489e5` |
| `SECURITY_DISPOSITION_AZURE_SAS_FALLBACK.md` | `b454ad76d546896ceb915746cef8ebb0dc8bfa07d76dfd9dca1c096ced3f9115` |
| `PO_DECISIONS_R0.md` | `320633cf624914fe9de748d8e92c2307cf7e62fbabb202288505f0844978ad5b` |
| `INTEGRATOR_DECISIONS.md` | `bc1d2cc7ad528ffe80366e9ba89cd7cdac3cd0ed69cbaf96ee2a076bdb427764` |

### Cohort evidence

| Cohort | Read manifest | Ledger draft |
|---|---|---|
| `R0-BLD` | `fc62e6e25e4f57b3…` | `55ca194783908de7…` |
| `R0-BRK` | `6adcd3928db4cfd5…` | `0ada39f0e52f4c6f…` |
| `R0-CLOUD` | `e0d59965340ef150…` | `d1b4bafbdea92de6…` |
| `R0-CORE-A` | `d2e1700c18782f69…` | `eeabaeac54082c60…` |
| `R0-CORE-B` | `7f1b14a53a7f90ac…` | `a81f6f2527d50827…` |
| `R0-CORE-C` | `1ca94bcba3c382e9…` | `f6ed4c19440a9a57…` |
| `R0-CORE-D` | `44b26f76be79b2e3…` | `abeb2f1fe045a73f…` |
| `R0-PER` | `e5628f5dcbdfb20f…` | `c0c8ee8f30ce5481…` |
| `R0-PY` | `28796d2236d97304…` | `df16dcf279edf182…` |
| `R0-SML` | `0160c49e1e0d6a94…` | `86aaafba0af9e395…` |
| `R0-SQL` | `fd38c630d8bc993d…` | `25422af408664d9a…` |
| `R0-TF` | `5f1716d17664fae4…` | `0da00a2069b65974…` |

## Proven in advance, before any implementation

| Mechanism | Result |
|---|---|
| In-process completeness sentinel | buildable against `xunit.v3.extensibility.core` alone; a removed obligation marker with unchanged test count exits non-zero through a native cleanup failure |
| `RequireCompleteObligationSet` provenance | fail-closed in all seven cases, including a project that grants itself the flag |
| Mandatory parent import | missing parent fails at evaluation; the forbidden `Exists` guard silently drops every repository-wide rule and stays green |
| Exact MTP command form | `dotnet test` with the SDK 10 opt-in; the minimum-count lock is reachable no other way |

## Open items carried into the checkpoint

Nine technical questions for the Lead, listed in `INTEGRATOR_DECISIONS.md`, of which one is a start
blocker: `global.json` is outside the write scope and no release-relevant profile run is possible
without editing it. Three Product Owner decisions are already taken and recorded in
`PO_DECISIONS_R0.md`.
