# Changelog

Product and release history. This file is not the Apache-2.0 section 4(b) record: that is the
generated [CHANGELIST.md](CHANGELIST.md), which lists every file changed against the upstream
baseline.

## Unreleased

ViciOne.ServiceBus has not been released. The repository is in a private development state, and the
entry below records what the current work changed for anyone reading the source.

### Removed

- The foreign licence check and the usage telemetry that reported host, bus, rider and endpoint data
  to a hard wired third party address on every bus start, together with their dependency injection
  and public API surface.
- The inert `TypeAttributes.Serializable` flag on the dynamically emitted message proxy and its
  `SYSLIB0050` suppression. The modern serializers are unaffected.

### Changed

- One output root: compilation output under `artifacts/sdk`, packages under `artifacts/packages`.
- `.slnx` is the canonical solution; the mechanical configuration matrix is gone.
- Analyzers and code fixes are separate assemblies, so the analyzer no longer references
  `Microsoft.CodeAnalysis.Workspaces`, which a command line compilation does not provide. They still
  ship as the one package `ViciOne.ServiceBus.Analyzers`.
- Every project builds at the SDK warning level and on C# 14.
- The Apache-2.0 licence text moved from `LICENSE` to `LICENSE.txt` unchanged.

### Not yet done

Modern encryption, the Entity Framework outbox poison handling and the final public API, naming and
obsolete clean up are open and block a release.
