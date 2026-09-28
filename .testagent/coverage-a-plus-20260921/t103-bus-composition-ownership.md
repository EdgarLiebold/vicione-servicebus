# T103 — bus composition ownership at startup

Implementation commits: `5d93fb898` and `a34fe1f07`. Product code is
unchanged. T97 `3cb94a285` remains the latest global Line/Branch/CRAP
checkpoint; this packet makes no new global grade claim.

The Roslyn static pairing scan classified `BusCompositionValidation.cs` as
unpaired, but the existing `BusCompositionStartupValidationTests` exercise its
host behavior without naming the internal type directly. This classification
was treated as a navigation hint, not as proof of missing tests. Two contract
gaps remained: combined duplicate transport/limits registration on one bus
beside a healthy bus, and simultaneous duplicate owners of all five reliable
messaging components.

The new real-DI tests assert exact ownership diagnostics for the invalid
default bus, successful validation of the unrelated typed bus, and zero
materialization of the invalid bus or any conflicting durable owner factory.
The tests make startup preflight order observable, rather than merely
expecting an exception. No product defect was found in this packet.

Test-gap and assertion review: both new tests check concrete exception type,
distinct cause strings, count and bus identity, forbidden factory side effects,
and the positive neighbor outcome where applicable. Neither is assertion-free
or limited to a null/trivial check. A controlled `count == 1` to `count >= 1`
mutation failed both new tests. Red Team found an initial P2 oracle gap: store
factory counters did not prove the invalid bus itself stayed unconstructed.
Separate throwing `IBus` descriptors closed it. A second controlled mutation
that resolved the invalid bus despite failures also failed both tests with
the bus counter at one. Product source was restored after each counterprobe.

The focused composition class passes 14/14. The complete Core unit project
passes 7,038/7,038 with no failures or skips on exact commit `a34fe1f07`.
Final independent read-only adversarial re-review: **PASS**, no remaining
concrete P1/P2. This is packet five after the T97 global checkpoint (T98,
T100, T101, T102, T103; T99 was review-only). The global profile remains due
on the agreed 20–30-packet cadence or earlier after a cross-cutting change.
