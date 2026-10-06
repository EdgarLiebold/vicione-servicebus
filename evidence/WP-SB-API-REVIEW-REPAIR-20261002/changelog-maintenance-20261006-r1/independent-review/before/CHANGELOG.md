# Changelog

ViciOne.ServiceBus is an unreleased fork of MassTransit 8.5.10. These records describe the source redesign,
removed capabilities, corrected defects, and changes needed by applications migrating from
MassTransit. The [live repository diff](license/repository_diff.py) compares all tracked source,
tests, and other files between the upstream `MassTransit/v8.5.10` tag and the
latest `main` commit.

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
- [Current review status](docs/quality-status.md): measured coverage, CRAP and open findings.

The detailed documents preserve the original chronological entries. The source-area overview
helps readers locate the changes relevant to each module. Git contains the exact patch and the
archived review artifacts.
