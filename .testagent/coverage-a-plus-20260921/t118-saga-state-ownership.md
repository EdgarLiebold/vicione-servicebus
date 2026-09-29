# T118 — Saga state declaration ownership and hierarchy

## Frozen product and test bytes

Implementation/test commit: `dc0b895415ec357a72278ae2af27f027c4236d0a`.
Source tree: `79d45d6c1d9961069a5109920c0bb7483f03c767`.
Test tree: `67b4b3b70aa05f3ada65a90c2e73afa9b1ddf825`.

All four property declaration forms used name-only short-circuits. A property
assigned a same-name state from another machine could remain foreign while
the owning machine's state and transition event caches retained the original.
The four new cases failed red-first. Declaration now restores the registered
state instance and leaves its transition events intact.

Adversarial review found related hierarchy defects: the named `SubState`
overload compared parent names, replacing a child left a stale entry under
the former parent, and replacing a parent orphaned its existing children.
The source now compares registered parent references, detaches replaced
children, and moves existing children to a replacement parent without
replacing the children or their transition events. A further counterexample
showed that moving a state beneath its own descendant could create a cycle;
all three declaration forms now reject that before constructing any state.

| Requirement | Product behavior proven by tests |
| --- | --- |
| `REQ-VSB-STATE-MACHINE-DEFINITION` foreign ownership | Four direct/nested State/SubState cases restore the owning state, cache, parent and four event identities. |
| `REQ-VSB-STATE-MACHINE-DEFINITION` property reparent | Direct and nested children move to the requested parent; old parent membership is removed, new membership and event cache are exact. |
| `REQ-VSB-STATE-MACHINE-DEFINITION` named child | Replacing a parent retains existing direct, nested and named children under the new registered instance. |
| `REQ-VSB-STATE-MACHINE-DEFINITION` cycle rejection | Direct, nested and named descendant parents throw `ArgumentException` for `superState` with unchanged property, cache, event and both directed hierarchy memberships. |

## Verification and limits

- Release build on final source/test bytes: zero warnings and errors.
- Focused state-declaration class: 10/10 passed, no skips.
- Complete Core project: 7,126/7,126 passed, no skips, on the frozen
  implementation/test commit.
- Independent read-only Red Team review: PASS after the stale-child,
  orphaned-child and cycle counterexamples were closed. `git diff --check`
  and requirement JSON parsing passed.

Microsoft `code-testing-agent` Research → Plan → Implement was applied inline.
`test-gap-analysis` and `assertion-quality` informed the identity, hierarchy
and failure-atomicity oracles; `run-tests` supplied the SDK 10/MTP commands.
The T98 Roslyn pairing was used only to find related tests. T114 remains the
latest strict 33-profile Line/Branch/CRAP checkpoint; no global A+ claim is
made from this packet. The next grouped checkpoint is due after T119.
