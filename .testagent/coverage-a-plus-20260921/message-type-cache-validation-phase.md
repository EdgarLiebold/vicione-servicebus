# Message contract metadata validation

The exact product profile at `a0931a6bb` measured
`MessageTypeCache<T>.CheckIfValidMessageType` at CRAP 39.35
(30/36 covered lines, complexity 34). During the complete source read, two
product defects were found. Invalid infrastructure context types leaked an
eligible base interface through `GetMessageTypes()`. A foreign type named
`System.Text.Json.Nodes.JsonObject` gained the special framework exception.

`GetMessageTypes()` now stops when the root type is invalid. The special
exception now requires exact identity with the framework `JsonObject` type.
The validation sequence was split into reference shape, namespace, context
role, and generic shape methods without changing its diagnostic order.

The new tests assert exact contract lists, invalidity reasons, or absence of
metadata for the three infrastructure contexts, a closed correlation
interface, actual and foreign JSON types, namespace-less and CoreLib types,
an anonymous type, and `Fault<ConsumeContext>`. The latter proves that a
valid outer fault still retains its own and base fault contracts. The context
theory failed 3/779 against the original source; the foreign-JsonObject test
failed against the name-and-namespace exception before the identity fix.

Microsoft CodeCoverage Cobertura from the focused Abstractions run:
`artifacts/coverage-message-type-cache-20260924/raw/coverage.cobertura.xml`,
SHA-256 `630633acbc6334080b8a7cf94945d94843c64d54a52c08bc2aa5c8b9cc478e73`.
The changed source was measured with the current production bytes before
requirement annotations were added to the tests. The final Abstractions suite
passed 785/785, and the final Release Unit/Architecture gate passed
10,256/10,256 with no failures or skips. The final Release solution build
completed with zero warnings and errors.

| Method | Covered lines | Complexity | CRAP |
| --- | ---: | ---: | ---: |
| `CheckIfValidMessageType` | 8/8 | 8 | 8 |
| `CheckReferenceShape` | 7/7 | 6 | 6 |
| `CheckNamespace` | 18/18 | 12 | 12 |
| `CheckContextRole` | 6/6 | 6 | 6 |
| `CheckGenericShape` | 7/9 | 6 | 6.4 |

The nine new methods have one `RequirementCoverage` attribute each and nine
matching rows in `AbstractionsRequirements.json`. An independent read-only
adversarial review checked all 15 tests in the class, their 15 projection
rows, validation order, fault recursion, and JSON type identity. It returned
PASS with no remaining concrete finding.

These scores are focused method results from one Abstractions report. The
product-wide line, branch, and CRAP totals remain those of `a0931a6bb`
until another exact-commit, all-assembly profile is collected. Global A+
remains open.
