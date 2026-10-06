# Changelog

ViciOne.ServiceBus is an unreleased fork of MassTransit 8.5.10. These records describe the source redesign,
removed capabilities, corrected defects, and changes needed by applications migrating from
MassTransit. The [live repository diff](license/repository_diff.py) compares all tracked source,
tests, and other files between the upstream `MassTransit/v8.5.10` tag and the
latest `main` commit.

## Unreleased — API review completion, 6 October 2026

The source API review and repairs are complete within their recorded contract and
local validation boundaries: all 234 confirmed audit findings have a bounded
acceptance, with no confirmed repair pending. The current inventory accounts for
18,600 exposed source symbols; the contract register also retains 833 accessor
representations, which are counted separately.

The final local checks passed 13,460 unit tests across 23 modules and 166 selected
Event Hubs tests, and verified all 31 delivery packages. Real provider and cloud
workflows, the excluded emulator tests, and ARM64 operation with 512 MiB without
swap remain separate validation work. This is an unreleased source checkpoint,
not a production or universal A+ certification.

See the [completed review corrections and verification boundaries](docs/changelog/product-fixes.md#api-review-completion-6-october-2026)
and the [final bounded acceptance](evidence/WP-SB-API-REVIEW-REPAIR-20261002/PACKAGE551_FINAL234_CURRENT18600_LOCAL_GATES_ROOT_ACCEPTANCE_R1.json).
Earlier entries and coverage pages retain their dated checkpoints.

## By topic

- [Source changes by area](docs/changelog/source-areas.md): identity, API, core messaging,
  reliability, transports, workflows, diagnostics, build and test structure.
- [Product defects corrected during source review](docs/changelog/product-fixes.md):
  the detailed chronological defect record since 6 September 2026.
- [Verification and regression tests](docs/changelog/verification-history.md):
  behavior, failure, boundary, and concurrency checks added during review.
- [Removed capabilities](docs/changelog/removed-capabilities.md) and
  [other changes](docs/changelog/other-changes.md).
- [Migration from MassTransit-style APIs](docs/changelog/migration-from-masstransit.md):
  changed call forms, moved namespaces, and new capability packages.
- [Dated coverage and review checkpoints](docs/quality-status.md): measured coverage,
  CRAP and historical open-finding records; the API audit completion is recorded above.

The detailed documents preserve the original chronological entries. The source-area overview
helps readers locate the changes relevant to each module. Git contains the exact patch and the
archived review artifacts.
