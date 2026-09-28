# T97 complete product-wide measurement — `3cb94a285`

The strict aggregate `artifacts/t97-aggregate.json` accepted 33 fresh
receipts from the exact source/test commit
`3cb94a285513dcfe43f1ba97a73f2bb9606af95c`, without `--partial`.
All 32 product assemblies, every required unit and local integration project,
and the no-AVX2 and scalar-fallback profiles are present. All 13,604 test
executions passed with no failures or skips. Failed preliminary fixture
attempts (missing local configuration and outage control) produced no accepted
receipt and are excluded. The corresponding profiles passed under the
canonical fixture runner.
The source tree is `73747bce9a35fb2943522a3586c5da65234daf37`; the test
tree is `20dcad01a6d4b72fb821469a8be6d563ddc8c9b4`.

| Measure | T85 frozen commit | T97 frozen commit | Change |
| --- | ---: | ---: | ---: |
| Passed test executions | 13,500 | 13,604 | +104 |
| Covered physical lines | 86,545/93,965 | 86,639/93,963 | +94/-2 |
| Line rate | 92.10344% | 92.20544% | +0.10200 points |
| Conservatively covered branches | 31,287/36,933 | 31,312/36,927 | +25/-6 |
| Conservative branch rate | 84.71286% | 84.79432% | +0.08147 points |
| Method identities | 26,094 | 26,094 | unchanged |
| Methods with CRAP > 30 | 0 | 0 | unchanged |

The conservative branch figure is not an exact union of branch identities
across independent Cobertura reports. The aggregate observed 2,776 tracked
C# source files; neither the observed-file count nor static source/test
pairing implies every file has or lacks a direct test. Global Line and Branch
A+ remain open. The highest observed CRAP remains 30.

Twelve broker fixture runs supplied the local integration profiles: Azurite
twice, LocalStack three times, RabbitMQ, PostgreSQL with SQL Server, Azurite
with Event Hubs, ActiveMQ with Artemis and controlled outage, Azure Service
Bus, PostgreSQL, and SQL Server. The fixture logs and teardown findings are
separate from receipt hashing, as in T85; the aggregate verifies the local
test execution and report, not the broker-log bytes.

## Work cadence after this checkpoint

Continue in larger coherent code-area packets. Run targeted behavior and
regression tests during edits, then the complete affected test project on the
frozen packet commit. Run the 33-profile aggregate after roughly 20–30 such
packets, or earlier when a shared runtime, test runner, coverage settings, or
provider boundary changes enough to invalidate the local picture. Each test
must distinguish a real product outcome, fault, boundary, or regression; a
coverage increase alone is not admission evidence.

An independent read-only Red Team reran the strict aggregate and obtained
byte-identical JSON. It checked all receipt hashes and exact commit/tree
bindings, zero failures/skips in every log, all project and assembly lists,
and the published figures. It found no concrete P1/P2 discrepancy. The two
invalid preliminary directories have no receipt and were excluded.
