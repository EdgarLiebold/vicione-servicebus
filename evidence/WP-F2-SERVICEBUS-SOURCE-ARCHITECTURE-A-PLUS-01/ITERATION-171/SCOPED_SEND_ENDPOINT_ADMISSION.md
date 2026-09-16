# Iteration 171 — scoped send endpoint admission

## Result

This packet personally reads the complete scoped send endpoint and scope-applying send-pipe adapter
sources (69 lines) and the complete new owning test file (81 lines). The endpoint constructor
inherited validation for its wrapped endpoint but accepted a missing service scope. The adapter
also accepted a missing required service provider.

Both required scope dependencies now fail at their public construction boundaries with exact
parameter names. A caller pipe remains intentionally optional. The adapter adds the exact scope
provider only when the send context has none, preserves an existing provider, does not run the
typed caller pipe during general send-context configuration, and forwards that caller pipe on the
typed path.

| Admission | Files / lines | Manifest SHA-256 | Chain SHA-256 |
| --- | ---: | --- | --- |
| Source | 2 / 69 | `217eea28c0e0c55f6bf9e5cc92c090ab70917916f73fe555cb76b9e71321e05f` | `4c9331070a4b4fe45765964be2e2d471017719c8abc6fb3d5e2ce93c1a8e7fe6` |
| Tests | 1 / 81 | `ccc21c197bb04b2e970c7c06359010be77bcfeb8d110e11f1cbf7f21f1700fe5` | `aa72453bbcda39b1b6535e002e6af2fc96850e536fd22c102ffc9275c50c237d` |

Manifest hashes cover the ordinally sorted `path<TAB>content-sha256` records with a terminal
newline. Chain hashes extend the Iteration 170 source/test chains with the corresponding manifest
hash. The manifests contain:

| Kind | File | Content SHA-256 |
| --- | --- | --- |
| Source | `ScopedSendEndpoint.cs` | `5201de0e339ff86e1635b0568a5db3ff05b8746c14d452cf1e27cea0df43d754` |
| Source | `ScopedSendPipeAdapter.cs` | `d3e6a40602c2bd54eb1cdcec28ced25382df10f530791684adcaedb825518266` |
| Test | `ScopedSendEndpointTests.cs` | `61f8025516f8d1f1a8b3f4d693a6cdc791405506a558f016288ad0e0d048fca0` |

Cumulative personal source admission is 304/4,116 current C# files.

## Proof

`Constructors_RejectMissingRequiredDependenciesAndAllowAnOmittedCallerPipe` proves the inherited
endpoint guard, both newly explicit required scope/provider boundaries, their parameter names and
the intentionally optional caller pipe. `Pipe_AddsMissingScopePreservesExistingScopeAndForwardsTheTypedCallerPipeAsync`
proves missing-scope injection, existing-scope preservation, the general/typed two-phase contract,
and exact caller-pipe forwarding through the endpoint-created adapter.

The two requirement variants are embedded in `CoreRequirements.json`, final SHA-256
`d6ec07a3ecef001a32e50f73a85c3270f64e59b537127063a10b0db7621c6e81`.

Four successfully compiled single-cause mutants were killed and restored: remove either new
constructor guard, omit general-path scope injection, and drop the caller pipe while creating the
endpoint adapter. Both product sources were restored to the reviewed final bytes before the final
gates.

Final focused Cobertura is `/private/tmp/vicione-servicebus-iteration-171-final.cobertura.xml`,
SHA-256 `316a9c22206876f070d862c7685870cdf16bfea276e78647cac7c77cc13f58d0`.
Both target classes report 100% line and branch coverage. Every reported method has complexity and
CRAP 1. Unit sorted-display-name SHA-256 is
`0a9718c3d4224ddb7d7f7e993f67644d9aa0b1a8917cd1eb1ae25683b8cb8e48`.

| Gate | Result |
| --- | --- |
| Focused owning tests | 2/2 passed |
| Full Core Release | 4,808/4,808 passed with suite parallelism disabled |
| Full EF unit | 248/248 passed |
| Strict Release product/unit/EF/local builds | 0 warnings, 0 errors |
| Product/test format | Exit 0 |
| Unit requirement projection | 1/1 passed |
| Local requirement projection | 1/1 passed |
| Mutation probes | 4/4 compiled mutants killed |

Core remains serialized because Iteration 155 demonstrated a pre-existing parallel-suite race.
Protected `review/**`, `TestResults/**` and `vicione-legacy/**` trees were not enumerated, read or
modified. Intended tag:
`servicebus-a-plus-iteration-171-scoped-send-endpoint-admission-2026-09-16`.

Whole-fork personal reading, global API/naming/coverage and configured external-provider acceptance
remain open. Remote publication remains an independent delivery step and cannot pause or deactivate
the active goal.
