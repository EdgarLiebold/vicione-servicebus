# Source change overview

This overview is generated from the original MassTransit 8.5.10 import and the current
source tree. It summarizes scale by source area; the [changelog](CHANGELOG.md) explains
behavior, replacements, removed capabilities and fixes. Git retains the exact file history.

Original import: `9be1da2046218be2503c77c529b68d96fe113008`.
Source projects: 33 in the import; 33 in the current tree.
Regenerate with `python3 tools/identity/change_list.py --write`; check with
`python3 tools/identity/change_list.py`.
The underlying comparison is `git diff --no-renames 9be1da2046218be2503c77c529b68d96fe113008 -- src`.

| Source area | Added paths | Changed paths | Removed paths |
|---|---:|---:|---:|
| Core and contracts | 1989 | 0 | 2815 |
| Workflow and application packages | 791 | 0 | 0 |
| Persistence | 179 | 0 | 332 |
| Scheduling | 32 | 0 | 55 |
| Transports | 1077 | 0 | 983 |
| Serialization, diagnostics and tooling | 149 | 0 | 306 |
| Other source | 0 | 1 | 2 |
| **Total** | **4217** | **1** | **4493** |

The repository-wide identity and path migration counts as additions and removals
because rename detection is disabled. These numbers describe Git paths, not independent
features or the amount of original code retained. Read the area-based changelog for
the corresponding product changes.
