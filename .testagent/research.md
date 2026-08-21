# Research — ViciOne.ServiceBus test reconstruction

Work package `WP-F2-SERVICEBUS-TEST-RECONSTRUCTION-11`.
Baseline `ae73c6da748e3bc3257dffa4971ee8680e086207`, tree `e5897e7632be4f491e01d51221ee59081d4d2aa0`.
Full evidence: `evidence/WP-F2-SERVICEBUS-TEST-RECONSTRUCTION-11/R0/`, bound by
`R0_FROZEN_RESULT.md`.

This file records only what is currently effective. Superseded findings are not carried here.

## 1. Reading closure

The complete inherited estate was read, not sampled. 5745 tracked files are bound by SHA-256 in
`BASELINE_TRACKED_FILE_MANIFEST.tsv`; Git paths minus read paths and read paths minus Git paths are
both empty. Twelve cohorts produced read manifests holding **3377 physical lines**, of which **1 is a header line**
in `R0-CORE-C/READ_MANIFEST.tsv`. They therefore carry **3376 file-read records over 3329 distinct
paths**; the remaining **47 records are intentional cross-cohort rereads**, where a second cohort had to
read a file another cohort also owned. Unknown-shape lines: 0. Paths outside the tracked baseline: 0.
Hash mismatches: 0. Missing mandatory paths: 0. The scope covers the inherited test projects, the
packable TestFramework, the Python tooling, the benchmark and verification models, the build
infrastructure and, for every cohort, the owning product project. These values are stated separately and
never collapsed into one figure — an earlier version of this file reported the 3377 physical lines as
'files read', which was wrong.

Two cohort self-corrections are recorded rather than hidden: one agent wrote a working file to the
repository root through a relative path after `chdir` and relocated it; the same agent declared a
second slip on its own. The worktree carried zero tracked modifications throughout.

## 2. Identity reconciliation

The eleven inherited anchors under `build/verification/expected/` were re-hashed: every path,
SHA-256 and per-category identity count equals the Lead plan, total **3114**. Every one of the 3114
is mapped to at least one obligation in `ANCHOR_IDENTITY_TO_OBLIGATION.tsv`; **zero unmapped**.

Reconciliation required modelling three identity-expansion rules the anchors encode implicitly:
generic fixture type arguments (`Fixture<PostgresTestDbParameters>.Method`), `[TestCase]` argument
lists appearing inside the identity string, and `[TestCase(TestName=…)]` aliases that are not method
names. Cohorts derived their filtered subsets themselves rather than adopting given numbers; one
found 12 identities declared under the root namespace inside a subdirectory's files, which a naive
namespace filter loses.

Four cohorts partition the 1873 `core` identities exactly: 415 + 470 + 454 + 534.

Two owners have **no anchor at all** — `Azure.Table` and `DynamoDbIntegration` — and three projects
(`Azure.ServiceBus.Core`, `EventHubIntegration`, `AmazonSqsTransport`) have neither anchor nor
verification category. Their 246 inherited tests have no recorded green run anywhere in this
repository. That is a property of the inherited estate, not a census gap.

## 3. Combined obligation ledger

`COMBINED_SEMANTIC_LEDGER.jsonl`, 3664 rows, 0 duplicate ids, all 19 contract fields present. Nine rows that
declared themselves helper capabilities, project files or lock files were reclassified out of `OBLIGATION`
during the correction, and three cross-owner fixture invariants became shared capabilities proven inside
the owning integration projects; no anchor identity lost its backing.

| Row kind | Rows |
|---|---:|
| `OBLIGATION` | 3358 |
| `INFRASTRUCTURE_FILE` | 149 |
| `ASSURANCE_PROMISE` | 116 |
| `SHARED_CAPABILITY` | 41 |

Of the obligations, 2869 are anchor-backed and 489 are newly derived from current product behaviour.
The two counts stay separate so the Lead-bound 3114 remains checkable.

## 4. Quality of the inherited estate

Measured per cohort, not estimated. Roughly one third of inherited tests cannot fail:

- 86 of 249 persistence identities carry no falsifiable claim; 76 of 459 container/middleware cases
  have no assertion expression at all; 66 assertion-free obligations among the root-level core files;
  20 in the saga area plus 14 that assert that something happened but not what.
- Optimistic and pessimistic concurrency `Container_Specs` are byte-identical with empty assertions:
  swapping the two modes turns nothing red.
- `FaultRescue_Specs` assigns a check result and never asserts it, while the pipe body under test is
  commented out.

Four product defects are encoded as expected behaviour: a SQL Server delete that always reports
false, an orphan cleanup that never executes there, dead-letter maintenance that never throttles, and
an inverted dead-letter metric condition. One analyzer diagnostic is declared and never reported,
with two inherited identities pinning its absence by name.

Shared mutable infrastructure is the second theme: about 184 RabbitMQ obligations delete and recreate
one virtual host in setup, protected only by assembly-level serialization; ActiveMQ has no virtual
host and its clean deletes every queue and topic on the broker; a migration hosted service drops
databases other cohorts use; only 3 of 466 broker obligations use a run-unique entity name.

## 5. Configuration and credentials

One correct pattern already exists — a fail-closed contract reading eight environment variables with
no fallback — and is the model for the single typed configuration owner.

Two security items are dispositioned in their own documents: the inherited `client.p12`, an
unreferenced upstream fixture containing an encrypted private key, and a committed Azure shared
access key whose namespace the Product Owner confirms no longer exists. The structural defect behind
the second one governs the rebuild: a missing environment variable must stop the run, never select a
fallback credential.

## 6. Mechanisms proven before implementation

The completeness sentinel, the fail-closed provenance property, the mandatory parent import and the
exact MTP command form were each measured with a positive control and a discriminating negative
probe. Details and exit codes in the four probe documents.

## 7. Skills

The twelve slice-bound skills are hash-bound in `SKILL_BINDING.md`. Three are read and applied with
their rules recorded; the remaining nine are declared unread with the wave in which they fall due.
`find-untested-sources` was executed exactly once as required; its result is a static pairing
heuristic and closes no obligation — two of its five zero-pairing projects were confirmed as fully
exercised through extension methods, dependency injection and virtual factory chains.
