# Keyed binding equality consistency

The last complete product-wide profile at `b6ffcfbdf` measured
`Bind<TKey,TValue>.Equals(object)` at 0/7 lines and CRAP 42; the typed overload
was also unhit at 0/5 lines. A first focused test checked the public binding
contract with value-equal but distinct records, different values, different
owner types, nulls, self-comparison, hash equality, and dictionary lookup.

Adversarial review found a product defect at the subclass boundary.
`Bind<TKey,TValue>` is inheritable. Before the fix, a base binding and a
derived binding with the same value were equal through `Equals(Bind<...>)` and
therefore matched as dictionary keys, but unequal through `Equals(object)`.
The new derived-binding assertions failed against that behavior. The product
fix makes typed equality require the same runtime type, matching the existing
object overload. The regression now checks both directions and dictionary
lookup. Equal base bindings continue to share a hash; unequal runtime types
may share a hash without violating the equality contract.

The test's `RequirementCoverage` entry is mirrored in
`CoreRequirements.json`. The focused post-fix run passed 1/1. Microsoft
CodeCoverage measured both `Equals` overloads at 7/7 lines and CRAP 6 in the
focused and complete Core runs; the complete Core suite passed 6,375/6,375.
The complete Engineering Release build passed with zero warnings and errors.
The full Unit/Architecture gate passed 10,229/10,229 without failures or
skips.

| Evidence | Result | SHA-256 |
| --- | --- | --- |
| `artifacts/bind-equality-identity/red-derived-equality.log` | Pre-fix subclass regression failed 0/1 at typed equality, as expected | `2f3388908997dfbc8858fc63408490efb1a31da21882b2c1291951e43d658d8a` |
| `artifacts/bind-equality-identity/coverage-product-fix.log` | Post-fix focused Microsoft CodeCoverage 1/1 green | `649410bdc8019c3bce4aa7bf2a3e9cc648d926499b00d952f66ada3fff50c98a` |
| `artifacts/bind-equality-identity/coverage-product-fix.cobertura.xml` | Typed and object `Equals` each 7/7 lines, CRAP 6 | `1943549c07562d50c9419c8a649f429d118f054b7db177ba029a01943a053a46` |
| `artifacts/bind-equality-identity/core-coverage-product-fix.log` | Complete Core 6,375/6,375 green | `dfffcbe52f351c1ac22a14adac9814ade5d63c5b53bb666e27a63b546f5d4229` |
| `artifacts/bind-equality-identity/core-coverage-product-fix.cobertura.xml` | Both `Equals` overloads 7/7 lines, CRAP 6 in complete Core | `caebd1be1fd8b530f28bdd5b38bf0bb8a77927b6468982afe2c2b07040adf524` |
| `artifacts/bind-equality-identity/release-build.log` | Engineering Release build: zero warnings/errors | `12065bf113ac93d208913931906dd5ff1245e16c11a200095c1a2e6d8c4f5277` |
| `artifacts/bind-equality-identity/unit-architecture-gate.log` | Complete Unit/Architecture gate 10,229/10,229 green | `62d7bd352006ebb3c1e31944e00914ca26d845377e0af597bd0bbb954b43e77b` |

Independent read-only adversarial review returned PASS for the defect fix and
assertions. The global A+ goal remains open; the last complete product-wide
profile predates this source change.
