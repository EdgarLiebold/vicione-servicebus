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

- One output root: compilation output under `artifacts/sdk`, packages under `artifacts/packages`, and
  everything a single test run writes under `artifacts/run-output/<run>/`, so two runs on one machine
  share no file.
- `.slnx` is the canonical solution; the mechanical configuration matrix is gone. There are two:
  `ViciOne.ServiceBus.slnx` for the product and `ViciOne.ServiceBus.Engineering.slnx` for the
  benchmarks, the diagnostics and their tests.
- `Directory.Build.targets` carries the late half of the build contract: six errors that refuse a
  project which drops its lock file or locked mode, packs without its licence or notice, targets a
  framework this product does not support, or reaches for `netstandard2.0` without being one of the
  three declared Roslyn compiler hosts.
- `build/verification/VERIFICATION_MODEL.json` is the single verification truth. It replaced a
  capability matrix and a not-executed inventory that described the same categories twice.
- The ActiveMQ publish topology is deployed to the broker. Resolving a destination name is a client
  side act and left the broker without the topic; `SessionContext.EnsureTopicExists` makes the broker
  hold it.
- Analyzers and code fixes are separate assemblies, so the analyzer no longer references
  `Microsoft.CodeAnalysis.Workspaces`, which a command line compilation does not provide. They still
  ship as the one package `ViciOne.ServiceBus.Analyzers`.
- Every project builds at the SDK warning level and on C# 14.
- The Apache-2.0 licence text moved from `LICENSE` to `LICENSE.txt` unchanged.

### Not yet done

Modern encryption, the Entity Framework outbox poison handling and the final public API, naming and
obsolete clean up are open and block a release.
