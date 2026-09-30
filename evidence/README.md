# Retained migration records

This directory contains small obligation maps retained from the MassTransit
test migration. They identify which inherited test behavior moved to native
.NET tests, which support-only behavior was retired, and which external broker
checks remain pending. The 39 TSV maps are historical mappings; they do not
claim that every named external test has run against a real service. See
[`TODO.md`](../TODO.md) for the remaining external validation work and the
[current review status](../docs/quality-status.md) for the latest measured
ServiceBus checkpoint.

Architecture tests read selected maps and verify that target test methods or
owner files exist. The disposition tests check `closureEvidence` file paths
for existence; they do not validate the contents or freshness of historical
records. Rows that point to this index record archived migration provenance,
not row-specific proof. A passing architecture test must not be read as a
fresh execution of every mapped obligation.

The prior `native-tests/obligation-maps/plan.md`, `research.md` and
`status.md` were migration work journals. Their useful outcome is the native
test ownership and retirement reflected in the maps and the
[verification history](../docs/changelog/verification-history.md). The full
journals and previously tracked raw proofs remain in Git tag
`archive/servicebus-pre-review-cleanup-20260930`. Historical references in
those journals were not rebuilt as current repository paths.
Ignored local `.log` files are not covered by that tag and remain outside Git.

`WP-F2-SERVICEBUS-IDENTITY/*.json` and the retained final-core
`VALIDATION.md` are snapshots of earlier migrations, kept because the
disposition maps name them. They are not current source, API, or test verdicts.
The live identity check runs in CI; current test and coverage output is
generated under ignored `artifacts/`, with a compact published checkpoint
under [`docs/quality/`](../docs/quality/).
