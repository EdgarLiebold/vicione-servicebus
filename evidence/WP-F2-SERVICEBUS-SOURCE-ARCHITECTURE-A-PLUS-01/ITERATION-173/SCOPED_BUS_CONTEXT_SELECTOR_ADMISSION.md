# Iteration 173 — scoped bus context selector admission

## Result

This packet personally reads the complete normal and deferred scoped-bus context selector sources
(106 lines) and the complete new owning test file (177 lines). The public normal selector accepted
every missing required dependency and both selectors accepted bindings whose required values were
missing. Those failures could occur only after selector work had started, producing downstream or
incidental exceptions instead of stable construction-boundary failures.

Every required selector dependency and bound value now fails before context inspection with its
exact public parameter name. Normal, ambient-transaction and buffered selectors preserve the
intended priority: bus-specific consume context, then global consume context, then root bus
context. The exact selected context and ambient-context identities are proven for all three paths.

| Admission | Files / lines | Manifest SHA-256 | Chain SHA-256 |
| --- | ---: | --- | --- |
| Source | 2 / 106 | `71ec3d884321addf2b67b913b3eb3fded723f4619c97e13e1bd17dbe07e3f2de` | `c0165e8af9b9819b6b5210c18cb0491ab652e150ab841ea01a921b78321f0cb8` |
| Tests | 1 / 177 | `a35a388a2e09198959a60cd6aa347332fcfcc9e1bda38781b5d4d0b5d239126e` | `135717f9793dce424ada8c065251e610d20f71b3c51d8ea25607bad7a95d0cd3` |

Manifest hashes cover the ordinally sorted `path<TAB>content-sha256` records with a terminal
newline. Chain hashes extend the Iteration 172 source/test chains with the corresponding manifest
hash. The manifests contain:

| Kind | File | Content SHA-256 |
| --- | --- | --- |
| Source | `DeferredBusScopedContextProviders.cs` | `e3f09addde1154d716487c62d40e8d61a5ed69e3f2e88190d60fd99a5876bf40` |
| Source | `ScopedBusContextProvider.cs` | `21e2a0ddbe039c6b5ddff36a9505a981e8fccf5c9228163deb3f6b1eac2fd781` |
| Test | `ScopedBusContextProviderTests.cs` | `f655a70e6240d9274a878423a47f11b63a9724a2f633ee810a65dbabd61587c1` |

Cumulative personal source admission is 309/4,116 current C# files.

## Proof

`Constructors_RejectMissingBindingsProvidersAndBoundValues` proves the normal selector's five
direct dependencies, both required bound values, and the deferred selectors' direct and bound
dependencies. A deliberately throwing context provider proves bound client-factory validation
happens before context inspection. Null deferred bus bindings and null bound bus values both
produce the stable `bus` boundary. `NormalSelector_PrefersSpecificThenGlobalThenRootContext` and
`DeferredSelectors_PreferSpecificThenGlobalThenRootContext` prove exact selector type and context
identity for every precedence branch.

The three requirement variants are embedded in `CoreRequirements.json`, final SHA-256
`f440ac16a72bf83ff5ed79dbcf3ed701b89034dbcf6454f9373dd030c85a53b5`.

Five successfully compiled single-cause mutants were killed and restored: remove the normal
selector's `bus` guard; remove its bound client-factory guard; remove the deferred selector's bound
consume-context-provider guard; negate normal bus-specific context detection; and negate deferred
bus-specific context detection. The first guard mutant initially exposed a masked downstream
exception, so the owning test was strengthened to select a path on which the missing bus cannot be
hidden, after which the mutant was killed. All product sources were restored to the reviewed final
bytes before the final gates.

Final focused Cobertura is `/private/tmp/vicione-servicebus-iteration-173-final.cobertura.xml`,
SHA-256 `3b6c845ccce517cce40f82e15fa9714e075dc60c2c3190616a065ecfaff0d6bb`.
All four target selector classes report 100% line and branch coverage. Constructor complexity and
CRAP are 4 for the normal and deferred base selectors and 2 for each derived deferred selector;
maximum CRAP is 4. Unit sorted-display-name SHA-256 is
`113ca66368417aed4f5376842a9393839604dfad46ebb7f29e7f7ec8fdc81980`.

| Gate | Result |
| --- | --- |
| Focused owning tests | 3/3 passed |
| Full Core Release | 4,814/4,814 passed with suite parallelism disabled |
| Full EF unit | 248/248 passed |
| Strict Release product/unit/EF/local builds | 0 warnings, 0 errors |
| Product/test format | Exit 0 |
| Unit requirement projection | 1/1 passed |
| Local requirement projection | 1/1 passed |
| Mutation probes | 5/5 compiled mutants killed |

Core remains serialized because Iteration 155 demonstrated a pre-existing parallel-suite race.
Protected `review/**`, `TestResults/**` and `vicione-legacy/**` trees were not enumerated, read or
modified. Intended tag:
`servicebus-a-plus-iteration-173-scoped-bus-context-selectors-admission-2026-09-16`.

Whole-fork personal reading, global API/naming/coverage and configured external-provider acceptance
remain open. Remote publication remains an independent delivery step and cannot pause or deactivate
the active goal.
