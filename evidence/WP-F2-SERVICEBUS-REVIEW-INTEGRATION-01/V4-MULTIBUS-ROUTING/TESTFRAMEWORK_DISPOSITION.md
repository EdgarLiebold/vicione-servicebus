# Legacy TestFramework terminal disposition

Date: 2026-09-03

The authoritative row-level ledger is `.testagent/testframework-capability-disposition.tsv`.

| Disposition | Count | Meaning |
|---|---:|---|
| `REPLACED_EXECUTING` | 9 | Reusable harness capability remains executable in the engineering-only native Testing project. |
| `RETIRED_SUPPORT_ONLY` | 135 | The legacy support/helper file is unnecessary because named native tests own its meaningful behavior. |
| `RETIRED_BUILD_INFRASTRUCTURE` | 3 | The obsolete project, lock file, and logging artifact are absent from the current graph. |
| **Total** | **147** | Every legacy path has exactly one terminal disposition. |

Ledger SHA-256:
`55974b5ff3d17507b9529ab3ec311178b4dfb13748f58d6a3d5699f834cb1390`.

The native architecture owner verifies all of the following during normal test execution:

- exactly 147 distinct legacy paths and only the three allowed terminal disposition values;
- every named native owner exists;
- every referenced closure ledger exists;
- the obsolete `ViciOne.ServiceBus.TestFramework` project and its source tree are absent;
- NUnit is absent from the project and dependency graph;
- Testing projects are present only in Engineering composition and absent from shipping composition.

This closes the review's regression-capability requirement without reviving an old project, old framework,
or parallel verdict path. It also preserves the agreed migration rule: native tests grow while an old test
or helper disappears only after its meaningful behavior is fully and more strongly owned.
