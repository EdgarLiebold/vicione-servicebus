# Iteration 170 — scoped client factory context admission

## Result

This packet personally reads the complete scoped client-factory context and scoped request-send
endpoint sources (139 lines) and the complete new owning test file (259 lines). Both public
constructors accepted missing required dependencies, permitting unusable objects to escape their
configuration boundary.

Both constructors now reject every missing dependency with its exact parameter name. The context
continues to forward all four state properties, all three pipe-connection shapes, both endpoint
resolution shapes, destination and consume-context identity, while wrapping each resolved endpoint
with the exact service scope. Both request send shapes preserve request data, caller cancellation,
underlying return semantics and the caller pipe inside a scope-applying adapter.

| Admission | Files / lines | Manifest SHA-256 | Chain SHA-256 |
| --- | ---: | --- | --- |
| Source | 2 / 139 | `7a8d891b830195f9ba0cd9818364c29c9f8abf7356c6e5dd693eb97b1138e923` | `57758963a0405c38bcf2af916ac00b04c6660a2c92a8f61fc8ea9b2990c7bdf8` |
| Tests | 1 / 259 | `a0e7d04db9823182793d486e11a48f0cc5fb8bbaf48f645034c77e651923ef4e` | `5110f6491b5b97cbefab1b60a55d51950ae717466cadd7e37b6c6d2dffebc8e3` |

Manifest hashes cover the ordinally sorted `path<TAB>content-sha256` records with a terminal
newline. Chain hashes extend the Iteration 169 source/test chains with the corresponding manifest
hash. The manifests contain:

| Kind | File | Content SHA-256 |
| --- | --- | --- |
| Source | `ScopedClientFactoryContext.cs` | `a8f23346055d3e43e75f2063527689a3403776316c0e7654fa3d9423fa41fbcc` |
| Source | `ScopedRequestSendEndpoint.cs` | `8b3ea37db641f180d21115b642ec769c03025526afd6fed1a16b07230a5edb6a` |
| Test | `ScopedClientFactoryContextTests.cs` | `143ae4def9ef44c4a3f303802845191164198cdeef123977ec31cd19fe18c5d4` |

Cumulative personal source admission is 302/4,116 current C# files.

## Proof

`Constructors_RejectEveryMissingDependency` proves all four required constructor parameters and
their exact names. `Context_ForwardsStatePipesAndBothEndpointResolutionShapes` proves exact state,
pipe, handle, request-id, destination and consume-context forwarding plus endpoint/scope wrapping.
`RequestEndpoint_ForwardsBothSendShapesWithScopeAndCancellationAsync` proves both request send
overloads, their distinct request ids and payload forms, exact caller token, underlying return
value, and both the original pipe and service-provider identity inside each scope adapter.

The three requirement variants are embedded in `CoreRequirements.json`, final SHA-256
`6d88a75999caa52b592b24e3859d57ef8706d074a9b878ba8e80b7924d99aa98`.

Six successfully compiled single-cause mutants were killed and restored: remove each of the four
constructor guards individually, and bypass the `ScopedSendPipeAdapter` independently in each of
the two request send overloads. Both product sources were restored to the reviewed final bytes
before the final gates.

Final focused Cobertura is `/private/tmp/vicione-servicebus-iteration-170-final.cobertura.xml`,
SHA-256 `30462497e1da6ca50c65f7884de4f4a7426397e722e0e9f4ff064111dfaacccb`.
Both target classes report 100% line and branch coverage. Every reported method has complexity and
CRAP 1. Unit sorted-display-name SHA-256 is
`3880af72d8b855c21bb207da4a90b4c492c5d86ad221b08cd3cf39b594f56545`.

| Gate | Result |
| --- | --- |
| Focused owning tests | 3/3 passed |
| Full Core Release | 4,806/4,806 passed with suite parallelism disabled |
| Full EF unit | 248/248 passed |
| Strict Release product/unit/EF/local builds | 0 warnings, 0 errors |
| Product/test format | Exit 0 |
| Unit requirement projection | 1/1 passed |
| Local requirement projection | 1/1 passed |
| Mutation probes | 6/6 compiled mutants killed |

Core remains serialized because Iteration 155 demonstrated a pre-existing parallel-suite race.
Protected `review/**`, `TestResults/**` and `vicione-legacy/**` trees were not enumerated, read or
modified. Intended tag:
`servicebus-a-plus-iteration-170-scoped-client-factory-context-admission-2026-09-16`.

Whole-fork personal reading, global API/naming/coverage and configured external-provider acceptance
remain open. Remote publication remains an independent delivery step and cannot pause or deactivate
the active goal.
