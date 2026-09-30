# T165 send identity boundaries

Scope: custom serializer and observer mutations across the InMemory transport,
payload admission, and outgoing message journal. The existing product-wide
coverage and CRAP baseline is `product-wide-profile-096e8ecfa.md`; this
correctness packet did not rerun the 33-profile measurement.

## Reproduced failures

| Test | Red-first result | Corrected behavior |
| --- | --- | --- |
| `SuccessfulSend_JournalDoesNotRecordAnIdentityChangedAfterDeliveryAsync` | Journal appended metadata ID B after transport delivered ID A | Confirmed send remains successful; the journal discards the contradictory capture and restores context ID A |
| `InMemorySend_RejectsIdentityChangedWhileMaterializingBodyAsync` | Send succeeded after the body changed ID A to B | Transport rejects before exchange delivery |
| `BoundedAdmission_RejectsIdentityChangedBeforeProviderDispatchAsync` | Bounded envelope callback changed ID and send succeeded | First admission rejects before user observers or provider delivery |
| `ConfiguredAdmission_RejectsPreSendIdentityChangeAfterBodyIsCachedAsync` | Observer changed ID between two admission passes and send succeeded | Second admission rejects, restores ID A, and prevents provider delivery |

The first journal test uses the real in-memory bus and confirms the consumer
received ID A while the custom body is materialized twice. The admission tests
use configured payload limits, a real send endpoint, and delivery counters.
The bounded serializer produces a valid JSON envelope from the registered
serializer before changing the context ID in its public callback.

## Verification and review

- Focused journal integration class: 10/10 passed after the first two fixes.
- Final complete Core project: 7,398/7,398 passed, 0 failed, 0 skipped.
- Read-only adversarial review found and drove closure of the earlier admission
  callback and between-pass observer gaps. Final verdict: PASS, no concrete
  remaining P1/P2 in this diff.
- The existing 33-profile aggregate covers commit `096e8ecfa`, not this commit.
  The user's latest direction accepts its Line and Branch levels as A+ and
  prioritizes product bug discovery. Provider-specific external services were
  not rerun for this Core/InMemory packet.
