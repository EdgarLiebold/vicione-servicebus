# Packed public API baseline

Date: 2026-09-04

The retained generator at `tools/public-api-baseline/PublicApiBaseline.cs` loads only assemblies restored from the
isolated package-consumer graph. It records package and assembly SHA-256 values followed by sorted externally visible
types, constructors, methods, properties, fields, events, generic constraints, parameter modifiers, default values,
return types, and selected visibility/editor-browsing metadata.

| Property | Result |
|---|---:|
| Fresh ViciOne packages in the developer-journey feed | 8 |
| Restored ViciOne assemblies inventoried | 8 |
| Baseline lines | 20,659 |
| Baseline bytes | 2,619,542 |
| Baseline SHA-256 | `188dd228f462707da25bef8ce0953be2a0356c7cc1d97e862306701006c950a5` |

The generated artifact is `artifacts/verification/v5-api-public-baseline.txt`. Build artifacts remain intentionally
untracked; the deterministic generator, locked dependency graph, architecture test, and this result binding are tracked.
The baseline contains the typed durable sender, typed operator results, quarantine pagination, shared persistence
identity, and coherent builder surface introduced by this closure.
