# Cumulative product coverage profile at de93663d4

## Exact-commit evidence

- Product/test commit: `de93663d462f3a140cd9ad910a75e45780a80941`.
  Since `f6d2ec3ec`, no `src` file changed. Commits `7505de640` and
  `de93663d4` add recurring-publish routing tests for both scheduler kinds,
  their requirement mappings, and changelog entries.
- The serial Release Unit/Architecture build completed with zero warnings and
  errors. Its log SHA-256 is
  `18892c2c4ed6ed08269c5345ae5f3ada406a8196db15655fd9ff15226e4f81bc`.
  The Core test DLL and all nine product DLLs in the fresh report embed the
  full product/test revision.
- The serial full Unit/Architecture gate passed **10,371/10,371** with zero
  failures and skips. Its log SHA-256 is
  `a528f13f1a8bc7e0e64cac09c74e6659591565309477022846f838aa3ba0e225`.
- The fresh Core Microsoft CodeCoverage run passed **6,397/6,397** with zero
  failures and skips. Its log SHA-256 is
  `67e5ab9ae4875ae727a4aa147e58fa7e2aaccf1606f043399a7c6f05431dd4bb`;
  its Cobertura SHA-256 is
  `be08badb1209444a1ce8a1ed83c6b74b86d074c7d6be9d9daf61f836bce92b76`.
- Read-only adversarial Red Team reviews of both final tests, their mappings,
  and corrected changelog claims returned **PASS**. The first runtime-publish
  review found an imprecise changelog sentence; it was corrected and passed
  re-review before commit.

## Cumulative result

| Measure | `de93663d4` | Previous `f6d2ec3ec` |
| --- | ---: | ---: |
| Line coverage | 84,118 / 93,507 = 89.9590% | 84,088 / 93,507 = 89.9270% |
| Conservative branch observation | 30,175 / 36,670 = 82.2880% | 30,162 / 36,670 = 82.2525% |
| Methods with CRAP > 30 | 26 / 25,994 | 28 / 25,994 |

Both `ScheduleRecurringPublishAsync(schedule, object, Type,
IPipe<SendContext>, token)` methods improve from 1/9 measured lines and
CRAP 31.28 to 6/9 lines and CRAP 7.33. A separate runtime-type test covers
the no-pipe overloads. For both the endpoint-backed and publish-backed
schedulers, the tests prove the resolved publish contract, exact command
type, schedule and payload identity, destination, payload URNs, forwarded
pipe and cancellation token, and matching returned handle. They assert real
public scheduler behavior at the sending boundary without a broker call.

## Merge and limits

`artifacts/coverage-a-plus-20260924-de93663d4/raw/` contains 47 parseable
reports for 32 product assemblies: 46 individually hash-verified inherited
reports and one fresh Core report. The intermediate `7505de640` Core report
is not merged; the fresh report at this commit includes both new tests.
Twelve broker fixture records are inherited; no broker fixture ran in this
iteration. Since `src` is unchanged, the new Core observations can merge
with prior observations. The JobSaga, Serialization, Retry, and RequestRate
source overlays remain in force. The fresh Core report adds one JobSaga
exclusion, yielding 14 stale JobSaga reports. Cobertura has no stable branch
identities, so the conservative result takes the largest covered count per
branch location. The capped-sum estimate is not used as the quality gate.

The merge is recorded in `analysis-47/summary.json` (SHA-256
`9994b70bb3b4779a2a100ba33ac31833dbb3806567be98d56705d6a8b1da1a3f`),
`analysis-47/methods.json` (`1da56ae4c8c1f7b6e3e97e445d5d8183446038ca4b28332d0e7b4e008f6da947`),
`provenance.json` (`a9cd6e4b5e67f96eb67fea433466ddd17eb7ab0b128e8993bbbdc20a13fbeffb`),
and `overlay-policy.json` (`bcd3d7ee86a130e2b82a61203ab5c8f098f0875bb64132bc9cd588020b7e280e`).

These tests do not run a broker or validate actual recurring delivery.
They cover command routing and forwarding at the public scheduler boundary.
The next CRAP 42 risks are the unobserved public
`ConsumeObserverConverter` callbacks, Azure Service Bus stream conversion,
and Azure Service Bus session-batching endpoint callback. The Microsoft
`code-testing-agent`, `coverage-analysis`, `test-gap-analysis`,
`assertion-quality`, and `run-tests` skills informed the tests, assertions,
commands, and profile.

Global A+ remains open: line and branch coverage are below A+, and 26 methods
remain above CRAP 30.
