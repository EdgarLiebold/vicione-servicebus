# Mutation validation — type relationships and readable properties

Each mutation changed one production or requirement-projection rule, rebuilt the owning Release
test project, and ran that complete project through Microsoft Testing Platform. Every mutation was
restored before the next run. Counts below are the exact counts at the point in the incremental
cohort when the mutation ran; the final unmutated totals are recorded separately.

| Mutation | Intended rejection | Observed result |
| --- | --- | --- |
| Accept more than one closed generic match as a single match | Ambiguous single-match access must fail | 186/187; exact ambiguity fact failed |
| Treat partially open matches as closed | Only fully constructed matches are usable | 186/187; exact closed-match fact failed |
| Remove the per-property getter requirement | A write-only property must not borrow another member's getter | 185/187; both write-only facts failed |
| Traverse a derived interface before its bases | Base-before-derived and derived-wins ordering is stable | 185/187; diamond/hiding order facts failed |
| Format open generic definitions through constructed generic arguments | Open generic names must format without activation or malformed arguments | 186/187; open-generic name fact failed |
| Replace `GenericTypeMatchCache` weak ownership with a strong dictionary | Collectible assemblies must remain unloadable | 186/187; collectible-type fact failed |
| Replace `TypeNameFormatter` weak ownership with a strong dictionary | Formatter caching must not root collectible types | 186/187; collectible-type fact failed |
| Replace `TypeCache` weak ownership with a strong dictionary | Short-name caching must not root collectible types | 186/187; collectible-type fact failed |
| Use conjunction instead of independent Future-state rejection conditions | Wrong-state futures must fail directly at registration | 617/618; only the wrong-state case failed |
| Remove the cache-lifetime requirement projection row | Compiled coverage metadata and the canonical projection must agree | The owning projection fact failed |
| Make the generic `ImplementsInterface<T>` overload return false | Both public overloads preserve interface semantics | 189/191; the positive overload and its null-boundary fact failed |
| Remove stable match ordering while reversing declaration order | Public enumeration must not depend on reflection order | 190/191; only stable-order enumeration failed |
| Disable generic-definition validation | A closed or non-generic comparison target is invalid | 189/191; both invalid-definition theory cases failed |

An earlier attempted Future mutation did not alter the guarded behavior and was rejected as
evidence. It is intentionally absent from this table. During the effective Future mutation, an
unrelated request-race test initially exposed its own missing send-completion synchronization. That
test defect was fixed at the real `RequestHandle.Message` boundary; the clean repeat then produced
the exact 617/618 result above.
