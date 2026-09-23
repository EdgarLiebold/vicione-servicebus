# Amazon SNS relative topic address

The last exact product-wide profile at `b6ffcfbdf` measured the public
`AmazonSqsEndpointAddress.TopicAddress` getter at 0/8 lines. An address parsed
from `topic:orders` under the `production` host scope names the SNS entity
`production_orders`. Before the fix, the getter produced
`topic:production/production_orders`; the transport's own parser could not
resolve that value back to the same entity. The new scoped roundtrip case failed
against the original implementation.

The getter now removes the exact scope prefix before producing a relative
`topic:` URI. It preserves the canonical lifetime options and rejects a queue
or an explicitly named topic that cannot be represented relative to the host
scope. Its public XML documentation and the changelog record the new boundary.
The root-scope and production-scope theory cases assert the exact URI and
reparsed entity, type, durability, and automatic deletion. The two negative
tests check queue and ambiguous explicit-topic behavior. The requirement JSON
matches all three test method attributes.

The focused address class passed 19/19. The complete Amazon SQS Release project
passed 211/211 with Microsoft CodeCoverage; `get_TopicAddress` measured 100%
line coverage, 100% reported branch coverage, complexity 6, and CRAP 6. The
Engineering Release build passed with zero warnings and errors. A first full
Unit/Architecture gate encountered a 30-second timeout waiting for a Quartz job
attempt while an Amazon SQS coverage run was started concurrently. The Quartz
fault test class passed 2/2 in isolation. The final serial gate passed
10,233/10,233. Concurrent test load is a possible cause of the timeout, not a
proven cause; the failed run remains archived.

| Evidence | Result | SHA-256 |
| --- | --- | --- |
| `artifacts/coverage-a-plus-20260923-topic-address/amazon-sqs-coverage.log` | Amazon SQS Release coverage 211/211 green | `d78bc00a06fe9cf92596a73ad8776c28bb0353504ba296313f220b6ef3f84a2c` |
| `artifacts/coverage-a-plus-20260923-topic-address/amazon-sqs.cobertura.xml` | Selected getter 100% lines/branches, CRAP 6 | `158783cb42c1cc0d3677ff4b02b2912ec4c1bd1f47765777bc4cb8a9e641a398` |
| `artifacts/coverage-a-plus-20260923-topic-address/release-build.log` | Engineering Release build: zero warnings/errors | `b314c4b458c5c6ac8ed5718aa5c39c34b9ae21633a286b263e010df497abf429` |
| `artifacts/coverage-a-plus-20260923-topic-address/unit-architecture-gate.log` | First gate: Quartz fault-test timeout; retained as failed evidence | `3d281075be0fb24915be124fbe74cb408ee2463f2c81ad99a7852c2184deb339` |
| `artifacts/coverage-a-plus-20260923-topic-address/unit-architecture-gate-final.log` | Final serial gate 10,233/10,233 green | `29243bdb8664f43c384f2c158648b08ca4efcca311237aae0fba4640d207c280` |

Independent read-only adversarial review found no further product defect in the
slice. The public URI changes for scoped topics; no production call site in this
repository uses the getter. The previous URI did not roundtrip through this
parser. The global A+ goal remains open; the latest full product profile still
predates this source change.
