# T75 saga composite declaration and dual-response request

T74's complete exact-commit profile identified related uncovered lines in
the saga state-machine declaration and two-response request paths. The
Microsoft Roslyn source/test pairing run is
`artifacts/t75-source-test-pairing.json`: 4,290 source files, 1,562 test
files, 2,559 statically paired and 1,731 unpaired. Extension-method callers
are not always attributed to the declaring class, so this is a selection aid,
not a test or coverage result.

Source review found a real composite declaration defect. The property and
named overloads created or replaced an event before validating the
constituent array. Eight red-first cases, covering null, empty, 32-member and
uninitialized arrays on both paths, failed 8/8 against the unchanged product:
invalid property calls replaced the implicit event identity and invalid
named calls left a ghost in the public event catalog. Validation now runs
before either registration. The shared validation also remains on the
existing-event overload. Red Team found the need to prove that preservation;
four further cases check the existing-event path. All 12 invalid-declaration
variants check exact error behavior, unchanged event identity/catalog or
composite status, then a valid redeclaration that raises exactly once.

The connected success request case sends two requests from different sagas through
the saga-ID-based two-response declaration. The service returns accepted and
denied responses whose payload correlation IDs are both a third, unrelated
GUID. Both responses must reach their own original saga using the request
header, produce exactly one matching published outcome, preserve the
destination and two accepted-response URNs, and create neither a decoy saga
nor a fault. A second case fails one request in the real in-memory service
while a healthy neighbor succeeds: the failed saga alone takes the fault
transition. Its request payload carries a decoy correlation ID distinct from
the saga ID; the sent fault preserves that payload ID, the exact saga request
header ID, and the exception type and text. No decoy saga is created, and only
the neighbor publishes a success outcome. This closes a Red Team finding that
the original fault case could not distinguish header-based routing from
payload-based routing. This complements the
existing three-response test that stores a separate request ID in a saga
property. The two new request journeys pass 2/2 with no product change.

The final targeted composite suite passed 12/12. The complete Core suite
passed 6,929/6,929 before the additional fault case was added; the two
request journeys then passed 2/2. The exact-commit full Core run remains.
Manual Microsoft
assertion-quality review found exception, identity, collection, state and
negative oracles in the composite cases, and exact routing, header,
destination, count, state, exact fault identity, neighbor isolation and
non-delivery oracles in the request cases.
Pseudo-mutation review finds the former register-before-validate behavior
causally killed by the red-first cases; dropping existing-event validation
is killed by the Red Team's added four cases; payload-based correlation would
miss the two original sagas because the payload carries an unrelated ID.
No numeric mutation score is claimed. Independent read-only Red Team re-review
of the composite, success-request and corrected fault-request paths is PASS
with no remaining concrete P1/P2 findings.

The exact-commit Core receipt and remote publication are pending. The T74
33-profile Line/Branch/CRAP report remains the latest complete product-wide
measurement; these source/test changes make its percentages stale. The next
full measurement follows the agreed 10–12-packet interval unless a broad
contract change requires it sooner.
