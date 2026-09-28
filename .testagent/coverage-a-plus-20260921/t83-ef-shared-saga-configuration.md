# T83 EF shared saga configuration admission

The shared EF Core saga overload added the saga map before invoking and
validating its repository configuration callback. A callback exception or
validation failure therefore left a map in the shared model and blocked a
corrected attempt with a different map.

A red-first test reproduced the retained entity after a callback exception.
The overload now completes configuration and validation before adding the
map. The final test verifies exact callback failure, pessimistic-mode
validation failure, absence of the mapped entity after each failure, and a
successful retry with a named table and resolved optimistic lock strategy.
The requirement manifest links the test to
`REQ-VSB-EF-SAGA-CONFIGURATION`.

The affected EF Core xUnit v3/MTP project passes 344/344, with no failures or
skips. Read-only adversarial review found no P1/P2 issue within the
configuration-admission change. A broader pre-existing limitation remains:
an arbitrary saga registration or custom service collection can fail after
partially mutating itself. Atomic registration across those public interfaces
needs a separate contract design; this packet does not claim it.

T74 remains the latest complete Line/Branch/CRAP profile. The next complete
profile is due at the agreed multi-packet checkpoint or sooner after a
cross-project contract change.
