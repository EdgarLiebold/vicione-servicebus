# Iteration 174 — scoped consume context provider admission

## Result

This packet personally reads the complete scoped bus/consume provider contracts and the complete
scoped, typed and setter implementations (5 files / 220 lines), plus the complete new owning test
file (198 lines). Public constructors and push methods accepted missing dependencies, the scoped
provider synchronized on its publicly reachable instance while publishing unsynchronized state,
and the typed provider disposed its global context before its local context and repeated disposal
work on every call.

All public construction and push boundaries now reject missing values with exact parameter names
before invoking collaborators. Scoped context publication uses a private synchronization object and
volatile reads/writes, nested contexts restore their predecessor, and the unavailable sentinel is
not advertised as an available context. Typed publication validates before changing global state,
disposes local state before global state, and makes combined disposal idempotent. Both setter forms
resolve through the exact supplied scope and forward the exact consume context.

| Admission | Files / lines | Manifest SHA-256 | Chain SHA-256 |
| --- | ---: | --- | --- |
| Source | 5 / 220 | `d20edec7b57f8e21aa637f81fa04d52c70e495a3beed0f2f2021453c33e80ea4` | `660b42b5e49fea889f3548cff99ca5d093774daffd09f1b41d6a5ffb13a0fe9e` |
| Tests | 1 / 198 | `46b64bf0450b59c05b60a1f3d93c8b580bb7d10a00664945a3dd504c070476f9` | `328a8f75039453a2337e36d4ddd0a3f2c7adaa0fe6412e905a4433a8cd045cce` |

Manifest hashes cover the ordinally sorted `path<TAB>content-sha256` records with a terminal
newline. Chain hashes extend the Iteration 173 source/test chains with the corresponding manifest
hash. The manifests contain:

| Kind | File | Content SHA-256 |
| --- | --- | --- |
| Source | `IScopedBusContextProvider.cs` | `d75cf7367c82882f5f8c10ed63cbb7ad24dc6cd186cf668767197a1451fb38ee` |
| Source | `IScopedConsumeContextProvider.cs` | `eb4d05948707211ac9310f6b5971b600d9d33f053dafdfc461f76907fb94340e` |
| Source | `ScopedConsumeContextProvider.cs` | `3ceb61899c90a4d06b368e9b0d1b43a12ac0627a1482208d9383a4a5077e3250` |
| Source | `SetScopedConsumeContext.cs` | `4f735df4d9a2af91666aeb14116e735c242ecee28738d762a1d57fe9565439c3` |
| Source | `TypedScopedConsumeContextProvider.cs` | `7edee90c912b8ae223badc96f457ed11defe1a708f1d282f8069e3c937e3c119` |
| Test | `ScopedConsumeContextProviderTests.cs` | `42cb19db13d21e7a8a5c9ea7a0f1e6b5cdd72ee5589a0db208b99c09c2e195af` |

Cumulative personal source admission is 314/4,116 current C# files.

## Proof

`ConstructorsAndPushMethods_RejectMissingDependenciesBeforeSideEffects` proves every new public
boundary, exact parameter names, and that typed/setter collaborators remain untouched on rejected
input. `ScopedProvider_PublishesAndRestoresNestedContextsAndHidesUnavailableContext` proves empty,
sentinel, first and nested states plus exact LIFO restoration. `TypedProvider_DisposesLocalBeforeGlobalExactlyOnce`
proves exact dual publication, local-before-global teardown and repeated-dispose idempotence.
`Setters_ResolveFromExactScopeAndForwardContext` proves both setter variants use the supplied
scope/provider/context identities and return working lifetimes.

The four requirement variants are embedded in `CoreRequirements.json`, final SHA-256
`56167b6ee84605f8dbff74ea76b0b28226e5fad20f1559ce4336c22e169e28d0`.

Six successfully compiled single-cause mutants were killed and restored: remove the typed global
constructor guard; remove typed push validation; reverse typed disposal order; remove the untyped
setter constructor guard; remove its scope guard; and remove the base provider context guard. A
local build-server stall was terminated and the same mutation was then compiled with build servers
disabled; infrastructure interruption is not counted as a mutation result. All product sources
were restored to the reviewed final bytes before the final gates.

Final focused Cobertura is `/private/tmp/vicione-servicebus-iteration-174-final.cobertura.xml`,
SHA-256 `99a3f7e74dcec77b6b5e8fd66b3db63bec20855803c4e83916775df21878d083`.
All six executable target classes (including nested lifetime types) report 100% line and branch
coverage. Maximum constructor/method complexity and CRAP is 4 for idempotent combined disposal;
all other reported methods are at most 2. Unit sorted-display-name SHA-256 is
`31dcef170c1956e2665c04cdbd1a14d4e2bedd930be6c7d51ab37e42c3066424`.

| Gate | Result |
| --- | --- |
| Focused owning tests | 4/4 passed |
| Full Core Release | 4,818/4,818 passed with suite parallelism disabled |
| Full EF unit | 248/248 passed |
| Strict Release product/unit/EF/local builds | 0 warnings, 0 errors |
| Product/test format | Exit 0 |
| Unit requirement projection | 1/1 passed |
| Local requirement projection | 1/1 passed |
| Mutation probes | 6/6 compiled mutants killed |

Core remains serialized because Iteration 155 demonstrated a pre-existing parallel-suite race.
The legacy non-null `GetContext()` annotation remains coordinated fork-wide nullability work because
empty providers deliberately return no context and multiple provider/transport packages consume
that contract; the attempted isolated annotation change was fully reverted before final gates.
Protected `review/**`, `TestResults/**` and `vicione-legacy/**` trees were not enumerated, read or
modified. Intended tag:
`servicebus-a-plus-iteration-174-scoped-consume-context-provider-admission-2026-09-16`.

Whole-fork personal reading, global API/naming/nullability/coverage and configured external-provider
acceptance remain open. Remote publication remains an independent delivery step and cannot pause or
deactivate the active goal.
