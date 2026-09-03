# V4 deterministic property-metadata validation

## Bound inputs

- Integration baseline commit: `2b31f21afaf51f6999e900b1987343b2909d09dd`
- Integration baseline tree: `ab7db47fc3b4d985184d552898776972bc4f494e`
- Protected review aggregate SHA-256:
  `371bf21331f0fc3316be271bce04ab37b3c54c50e13f443789d94c1f6eca1f18`
- V4 bundle SHA-256:
  `e8f28736562bf7c4fa8ffca4dfd662cd5105d3124e26d2ba424fe1ac0d192b87`
- V4 bundle head: `f8050928715e536b60c42d800d1cbb81c085818f`
- Semantic donor commit: `be8979abb6fc5340d5a09c0c3983f3974cd1238c`
- The PO-owned `review/**` tree remained unchanged and untracked.

## Integrated behavior

The former duplicate internal reflection helpers have one explicit public owner under
`ViciOne.ServiceBus.Metadata`. `PropertyAccessPolicy.PublicOnly` and `IncludeNonPublic` replace implicit
visibility choices. Read-only and read/write caches are case-insensitive, return stable cached instances,
and select the most-derived property deterministically when a name is hidden.

Accessor creation validates null input, declaring-type and property-type compatibility, instance versus
static shape, and indexers before execution. Unknown access-policy values fail closed. The final accessor
delegate is constructed once; no first caller starts background compilation. Public accessors on
reference-type declaring types use compiled expressions when dynamic code is available. Nonpublic,
no-dynamic-code and value-type declaring accessors use deterministic reflection.

Reflection fallback unwraps `TargetInvocationException` and rethrows the original user exception with
its stack preserved. Untyped setters validate values before both compiled and reflection execution:
nonnullable value properties reject null with `ArgumentNullException("value")`, incompatible boxed
values reject with `ArgumentException("value")`, and nullable value/reference inputs remain valid.
Core runtime wrappers adopt the same eager delegate and exception semantics. TypeCache, saga factory and
table formatting consumers now use the single metadata owner.

## Review quality and native corrections

The reviewer direction is strong: it correctly selects duplicate ownership, explicit access policy and
hidden compilation as important determinism risks. It is not safe as a literal transplant. Native review
and mutation work corrected five material defects or omissions:

1. Compiling an untyped setter whose declaring type is a value type mutates an unboxed copy, not the
   caller's boxed struct. Such accessors now use reflection.
2. Reflection invocation wrapped user getter/setter failures in `TargetInvocationException`. The original
   exception is now preserved.
3. A broad compilation catch could turn unrelated defects into silent fallback. Only known compilation
   failures select reflection.
4. Lazy `Task.Run`/interlocked delegate replacement made first-use behavior scheduler-dependent. Delegate
   construction is now synchronous and final.
5. Compiled and reflection untyped setters exposed different null/coercion exceptions. One explicit value
   validator now gives both paths the same public contract.

This supports the same A-/B+ review assessment used for the preceding donor packages: high-value
architecture and risk selection, with material runtime-semantic corrections required by the native owner.

## Executing evidence

- Final analyzer-active, non-incremental Release Abstractions build: zero warnings, zero errors.
- Final Abstractions test project: 272/272 passed, zero failed, zero skipped.
- Final analyzer-active, non-incremental Release Core build: zero warnings, zero errors.
- Final Core test project: 1,550/1,550 passed, zero failed, zero skipped.
- Final analyzer-active, non-incremental `ViciOne.ServiceBus.Tests.Unit.slnx` build: zero warnings, zero
  errors.
- Final canonical serialized Unit/Architecture profile: 3,174/3,174 passed, zero failed, zero skipped.
- Thirty-four direct facts across the four new metadata/runtime owners bind exact values, mutation of
  boxed and reference targets, declaring types, cache identity, exception type/message/parameter and
  original exception identity. Strengthened TypeCache and Architecture owners bind integration and the
  exact retired-versus-current full type names.
- The three inherited internal-reflection cases were removed only after their stronger native owners
  existed. No unrelated old test was removed; the now-empty old test directory was then removed.
- Requirements JSON parses and contains 220 Abstractions and 1,158 Core entries. Runtime projection passes
  in the complete profile.
- Scoped `dotnet format --verify-no-changes` passes for all 20 changed C# paths. `git diff --check`, stale
  owner, hidden async/threading, empty-directory and review-manifest gates pass.
- The protected review manifest verifies every listed file. Its aggregate and V4 bundle hashes remain the
  bound values above.
- An initial nonisolated analyzer build ended after 5:02 with exit 1 while reporting zero warnings and zero
  errors. A stale earlier ServiceBus build was removed and the identical build completed in 10 seconds with
  `--disable-build-servers`. This is recorded as local shared-build-server interference, not product
  evidence. MTP tests were run without that build-only switch.

## Independent one-cause mutations

Twenty-five buildable one-cause mutations were killed. Every target was restored before the final positive
builds and executions.

| ID | Single changed cause | Causal native owner |
| --- | --- | --- |
| M01 | `PublicOnly` includes nonpublic accessors | policy and TypeCache visibility rows fail |
| M02 | `IncludeNonPublic` excludes private accessors | private getter/setter rows fail |
| M03 | Accept an unknown access-policy value | both invalid-policy rows fail |
| M04 | Remove early property-type validation | mismatched property-type row fails |
| M05 | Remove target compatibility validation | unrelated-target row fails |
| M06 | Accept an indexer | indexer-construction row fails |
| M07 | Accept a static property | static-property row fails |
| M08 | Compile an untyped value-type declaring setter | boxed struct remains unchanged |
| M09 | Keep reflection getter exception wrapping | exact original getter exception row fails |
| M10 | Keep reflection setter exception wrapping | exact original setter exception row fails |
| M11 | Make both caches case-sensitive | case-insensitive lookup rows fail |
| M12 | Select the base rather than most-derived hidden property | exact declaring/type row fails |
| M13 | Ignore getter policy in the read/write cache | getter-policy row fails |
| M14 | Ignore setter policy in the read/write cache | setter-policy row fails |
| M15 | Ignore getter policy in the read-only cache | read-only policy row fails |
| M16 | Default TypeCache to nonpublic access | TypeCache owner fails |
| M17 | Return a default value from the runtime public getter | first-call exact-value row fails |
| M18 | Make the runtime public setter a no-op | first-call target-state row fails |
| M19 | Keep runtime private getter exception wrapping | exact original exception row fails |
| M20 | Keep runtime private setter exception wrapping | exact original exception row fails |
| M21 | Remove runtime read-property type validation | constructor mismatch row fails |
| M22 | Remove runtime write target-type validation | exact validation parameter row fails |
| M23 | Reintroduce one retired internal metadata type | assembly ownership boundary fails |
| M24 | Skip value validation in the generic setter | private reflection null row fails |
| M25 | Skip value validation in the untyped setter | null and incompatible boxed-value rows fail |

Restored SHA-256 values:

- property accessor factory: `a21d19194e617d29acc486fb240b343a4a11f37c806a2c32addd343fa6bf32d3`
- read-only property cache: `a77905761cd06d7fd129f9e85d5dd1494ffd419c2adece40487a1279b0f9891b`
- read/write property cache: `870df46f2fae2177f57beda3d5b86c746967b1eff7550eba4a596609b05fc0ff`
- TypeCache: `f67c944eb8394c2346b1f342208aa31bd713059115aa26d945bb84591f3fdf41`
- runtime read property: `7bedf36d4d62ec5b4faa908949455b4898c03f6715688af9435389f35a0259b4`
- runtime write property: `d7d2ea5a1bf2c6c0778751351c6a18b96a534aea36ae2f4175e654b37750e6aa`

This local package is the eighth of twelve semantic V4 reviewer packages. It raises V4 integration
progress to 8/12 (66.7%). Remote publication is not included or implied.
