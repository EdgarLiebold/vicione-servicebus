# Initialized scheduled-send contract slice

## Scope

- Exact source/test commit: `eb45d96ee`. Product source was unchanged.
- Four attributed test methods execute six cases through the three public values-based
  `MessageScheduler.ScheduleSendAsync<T>` overloads. Their four entries are in the Core requirement
  projection.

## Product behavior proved

- Values initialize a new message with both fields before the provider sees it. The provider
  receives the exact destination, due time, contract type, and nondefault cancellation token, and
  the returned handle preserves its identity and the initialized message.
- The default initializer turns `__Header_Tenant_Code` into `Tenant-Code`. Its header is visible on
  the provider's actual send context both without another pipe and together with the typed or
  untyped caller pipe. Each caller pipe changes that same context once.
- A pending task-valued property keeps provider dispatch pending. Completing the property yields
  the initialized value. Cancelling the caller while it is pending returns the exact cancellation
  token, leaves the value task unsettled, and never calls the provider.
- Eight required-argument cases cover all three overloads. A getter on the input values records
  whether initialization began; it remains untouched when invalid inputs are rejected.

## Verification

- Final focused Microsoft CodeCoverage run: 6/6 passed, zero failures and skips. Cobertura:
  `artifacts/coverage-a-plus-20260923-ac363722c/core-scheduling/initialized-focused-final.cobertura.xml`,
  SHA-256 `d8effeb8a49ab43afab584959bbd63eb9a92e42373ac098897e08834f2854799`.
- Complete current-byte Core CodeCoverage run: 6,356/6,356 passed, zero failures and skips.
  Cobertura:
  `artifacts/coverage-a-plus-20260923-ac363722c/core-scheduling/core-both-slices.cobertura.xml`,
  SHA-256 `71f63355a60231b3c2cfa107120780598a19e37aca9d86cc65e0241e533bf41b`.
- Complete current-byte Unit/Architecture solution: 10,169/10,169 passed, zero failures and skips.
  The requirement projection test passed in the same gate. The focused Release build completed
  without warnings or errors.
- Read-only adversarial review initially returned FAIL: the first version never generated a
  convention header and did not prove that invalid arguments were rejected before initialization.
  Both mutants were closed, the focused and complete gates were rerun, and the final review
  returned PASS with no remaining concrete finding for these variants.

## Focused method result

| Async `ScheduleSendAsync<T>` values path | Previous product-wide CRAP | Complete Core Lines | Reported Branches | Current CRAP |
| --- | ---: | ---: | ---: | ---: |
| No additional pipe | 20 | 7/7 | 4/4 | 4 |
| Typed send pipe | 42 | 9/9 | 6/6 | 6 |
| Untyped send pipe | 42 | 9/9 | 6/6 | 6 |

The previous CRAP values come from the 36-report `ac363722c` profile; the current values come
from the complete Core report at the new test bytes. These are targeted results, not a new
product-wide aggregate. The next connected risk area is the Saga scheduling activity, whose async
state machine was 0/10 lines and CRAP 72 in the last full profile. Global A+ remains open.
