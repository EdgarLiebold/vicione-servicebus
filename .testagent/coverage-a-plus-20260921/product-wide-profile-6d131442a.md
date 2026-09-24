# Cumulative product coverage profile at 6d131442a

## Exact-commit evidence

- Product/test commit: `6d131442a6add1a67bc462bcf601469f8efa7a18`.
  Against `de93663d4`, one `src` file changed:
  `EndpointQosTopologyValidator.cs`. The commit moves per-endpoint resolution
  into a focused helper without changing its statements, adds a test for
  matching and conflicting endpoint/consumer QoS declarations in both input
  orders, maps the requirement, and updates the changelog.
- The serial Release Unit/Architecture build completed with zero warnings and
  errors. Its log SHA-256 is
  `9224149cde8f5160b348cfb4c5b0d685a938e9855d01daaee218f20e61fe46da`.
  The Core test DLL and all nine product DLLs in the fresh report embed the
  full product/test revision.
- The serial full Unit/Architecture gate passed **10,372/10,372** with zero
  failures and skips. Its log SHA-256 is
  `a077da09644e01be3e0d2db36f5a8b46225115e32a59e6eb1d147eee7835dafd`.
- The fresh Core Microsoft CodeCoverage run passed **6,398/6,398** with zero
  failures and skips. Its log SHA-256 is
  `5d99054fec90f8debe389c9335086f96339259fc202938244d994cc8cba5f88c`;
  its Cobertura SHA-256 is
  `ddf5d76f1847b4f90fbc5914a340252a2dbcf3a3c98400478c1f758d0f246c2d`.
- Read-only adversarial Red Team reviewed the final product refactor, test,
  requirement mapping, and changelog and returned **PASS**. Its first review
  found that an input-order mutant could survive the positive case. The test
  was strengthened for both orders and a single conflict diagnostic, then
  passed focused tests and Red Team re-review.

## Cumulative result

| Measure | `6d131442a` | Previous `de93663d4` |
| --- | ---: | ---: |
| Line coverage | 84,124 / 93,509 = 89.9635% | 84,118 / 93,507 = 89.9590% |
| Conservative branch observation | 30,182 / 36,670 = 82.3071% | 30,175 / 36,670 = 82.2880% |
| Methods with CRAP > 30 | 25 / 25,995 | 26 / 25,994 |

`EndpointQosTopologyValidator.Validate` changes from 34/36 measured lines
and CRAP 32.18 to 15/15 lines and CRAP 12. The extracted `ResolveEndpoint`
is 23/23 lines and CRAP 20. The new test proves that endpoint-owned QoS
remains the exact canonical instance regardless of declaration order when
both owners agree, and that a differing consumer-owned value produces one
cross-owner conflict diagnostic. The source refactor preserves grouping,
diagnostic order, and the returned frozen dictionary's ordinal comparer.

## Merge and limits

`artifacts/coverage-a-plus-20260924-6d131442a/raw/` contains 48 parseable
reports for 32 product assemblies: 47 individually hash-verified inherited
reports and one fresh Core report. Twelve broker fixture records are
inherited; no broker fixture ran in this iteration. For the changed QoS
source file, 40 older reports are excluded and only the fresh Core report is
used. The JobSaga, Serialization, Retry, and RequestRate source overlays
remain in force; their stale-report counts are respectively 15, 40, 40, and
44. Other source files are unchanged, so their earlier observations can
merge. Cobertura has no stable branch identities, so the conservative result
takes the largest covered count per branch location. The capped-sum estimate
is not used as the quality gate.

The merge is recorded in `analysis-48/summary.json` (SHA-256
`a9be08a97488b229768148065f21142276ca16ddb57bd57cc6f28a815729a425`),
`analysis-48/methods.json` (`fc992caa97c71be0e822abe164fa9f04d52950a136a119709f80deb549d56740`),
`provenance.json` (`68ecc2099f35bd2925489b50857fcb46f35a3985c66896e736e6864c845362bc`),
and `overlay-policy.json` (`24ded936143de47d76edd4278822194d3f3e174dccaaeb11afe9dcd8c4ccc04b`).

This iteration proves configuration resolution and diagnostics; it does not
start a broker endpoint. The Microsoft `code-testing-agent`,
`coverage-analysis`, `test-gap-analysis`, `assertion-quality`, and `run-tests`
skills informed the test, mutation review, commands, and profile.

Global A+ remains open: line and branch coverage are below A+, and 25 methods
remain above CRAP 30.
