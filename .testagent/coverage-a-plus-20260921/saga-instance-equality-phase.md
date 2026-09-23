# Saga instance wrapper equality

The last complete product-wide profile at `b6ffcfbdf` measured
`SagaInstance<TSaga>.Equals(object)` at 0/7 lines and CRAP 42. The public
wrapper is inheritable. A new behavior test uses two distinct saga states
with equal record values, a different state, null, an unrelated type, and a
derived wrapper in a dictionary keyed by the base wrapper type. Before the
product fix, the test failed at the typed base-to-derived equality assertion:
`Equals(SagaInstance<TSaga>)` accepted the derived wrapper while
`Equals(object)` rejected it. This red focused run was observed during
development; its console output was not archived.

The typed overload now requires the same runtime wrapper type after its null
and self checks, matching the existing object overload. Equal base wrappers
still share equality and a hash; derived wrappers are rejected in both
directions and do not match a base-wrapper dictionary key. The test carries a
matching `CoreRequirements.json` entry. The existing hash code follows the
possibly mutable saga state; this is a separate preexisting API property,
and internal saga indices use reference equality.

The focused post-fix test passed 1/1 during development; its console output
was not archived. Complete Core with Microsoft CodeCoverage
passed 6,376/6,376. Both `Equals` overloads have 9/9 covered lines, full
reported branch coverage, complexity 6, and CRAP 6 in the Core report. The
Engineering Release build passed with zero warnings and errors; the complete
Unit/Architecture gate passed 10,234/10,234 without failures or skips.

| Evidence | Result | SHA-256 |
| --- | --- | --- |
| `artifacts/saga-instance-equality/targeted.cobertura.xml` | Focused equality coverage after fix | `dd298fefa76041dfb5de2937b50e9b1c6bd977bca394b73b6c55f93b390257a8` |
| `artifacts/saga-instance-equality/core-coverage.log` | Complete Core 6,376/6,376 green | `2d293b5f03ca1e58d3c727ba28b4f9d0c168d3bff3f9e780aa121d916cdad85e` |
| `artifacts/saga-instance-equality/core.cobertura.xml` | Both `Equals` overloads 100% lines/branches, CRAP 6 | `f99e3dd0e36078b3867f8cd67da27a7ec4354b34e4da2d38e313964d75106d29` |
| `artifacts/saga-instance-equality/release-build.log` | Engineering Release build: zero warnings/errors | `bf791b86064ac3125e935bbd3d02dbd467aeb8e5875b3c943d34ab783f435fbc` |
| `artifacts/saga-instance-equality/unit-architecture-gate.log` | Full gate 10,234/10,234 green | `e844ee938b32e8f5730f6a8755869e8173dad0ec2b18deb07cd236ce5087d26f` |

Independent read-only adversarial review returned PASS for the defect and
assertions. The global A+ goal remains open; the last complete product-wide
profile predates this source change.
