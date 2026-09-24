# Cumulative product coverage profile at 6ecd8e617

## Exact-commit evidence

- Product/test commit: `6ecd8e617671083246c08f2e2727c1f2e03e4798`.
  Against `330681999`, one product source file changed:
  `ServiceBusBatchingExtensions.cs`. Its invalid-endpoint diagnostic now names
  the shared `IServiceBusEndpointConfigurator` contract that the callback
  actually accepts. The new tests exercise the public session-batching
  extension through a real `BatchOptions` instance, the broker session group
  selector, and both queue and subscription endpoint callbacks for four
  prefetch boundaries. Requirement mapping and changelog accompany the fix.
- The serial Release Unit/Architecture build completed with zero warnings and
  errors (`build.log` SHA-256
  `6e9e8e6f1eb51b1bb03b52c8e3ee8a339b8e8587dc6d0a57bac56209a41a27d9`).
  The Provider test DLL and all eight product DLLs in its fresh report embed
  the full product/test revision.
- The serial Unit/Architecture gate passed **10,395/10,395** with zero
  failures and skips (`gate.log` SHA-256
  `6f2de1d538a2051f63bd5fe653c061b772f4db1d52b7fce1a6b4849c5a3e5337`).
- The fresh Azure Service Bus Microsoft CodeCoverage run passed **336/336**
  with zero failures and skips (`asb-coverage.log` SHA-256
  `21e21506e591ed24e68165b6e905af8af45a3e03852f9ed9a6d8a0f7fb5eae4a`;
  Cobertura SHA-256
  `065cbeac795873350d5ad0dd4a91a8e831ce6b4e95225ae9a9f16195078531a9`).
- Read-only adversarial Red Team first found that the positive test covered
  only queue endpoints and that the negative test locked in the misleading
  diagnostic. The test and product diagnostic were corrected; focused and
  full Provider tests passed, and Red Team re-review returned **PASS**.

## Cumulative result

| Measure | `6ecd8e617` | Previous `330681999` |
| --- | ---: | ---: |
| Line coverage | 84,225 / 93,509 = 90.0715% | 84,196 / 93,509 = 90.0405% |
| Conservative branch observation | 30,221 / 36,670 = 82.4134% | 30,210 / 36,670 = 82.3834% |
| Methods with CRAP > 30 | 21 / 25,995 | 22 / 25,995 |

The session endpoint callback in `SetServiceBusSessionBatchOptions` moves
from 0/9 measured lines and CRAP 42 to 9/9 lines and CRAP 6. The outer
configuration method and its other lambdas are also fully covered in the
fresh report. The tests verify exact batch and endpoint limits, broker
session grouping rather than reply-session grouping, the zero/below/equal/
above-limit prefetch cases for both supported endpoint kinds, and rejection
of invalid options or another provider's endpoint before mutation.

## Merge and limits

`artifacts/coverage-a-plus-20260924-6ecd8e617/raw/` contains 51 parseable
reports for 32 product assemblies: 50 byte-verified inherited reports and
one fresh Azure Service Bus Provider report. Twelve broker fixture records
are inherited; no broker fixture ran in this iteration. For the changed
batching source file, three older reports are excluded and only the fresh
Provider report is used. The JobSaga, Serialization, Retry, RequestRate, and
QoS source overlays remain in force; their stale-report counts are
respectively 16, 42, 42, 47, and 42. Other product source files are
unchanged, so their earlier observations can merge. Cobertura has no stable
branch identities, so the conservative result takes the largest covered
count per branch location. The capped-sum estimate is not used as the quality
gate.

The merge is recorded in `analysis-51/summary.json` (SHA-256
`c8d9f3c253972da835771d5e0bdcbd3fa132a15aad29c7df16018ff1e8ee2cc9`),
`analysis-51/methods.json` (`e78130622373be5778fef5b6045651ccee89ce8a584e8dad863a07a9ce0f0321`),
`provenance.json` (`5f0c91f51c4ed198d293cef442dc6987e29181f59e8917a698f4781ddbafc0c9`),
and `overlay-policy.json` (`4696a0adf50e548e041ee2443ce5f230e01e7a49d4ff1efd7f56c488eea83148`).

The Microsoft `code-testing-agent`, `coverage-analysis`, `test-gap-analysis`,
`assertion-quality`, and `run-tests` skills informed test design, adversarial
review, commands, and measurement. Global A+ remains open: branch coverage
is below A+, and 21 methods remain above CRAP 30.
