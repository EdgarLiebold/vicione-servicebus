# T100 — EF transactional outbox session and capacity integrity

Implementation commit: `ea3ce51ee`. Latest product-wide checkpoint remains
T97 `3cb94a285`: 92.20544% physical lines, 84.79432% conservative branches,
and zero methods above CRAP 30. This packet does not claim a new global A+ grade.

## Product defects reproduced and repaired

- Clearing or detaching staged EF entries made the previous `All` check succeed
  on an empty set. Commit could report success without a durable message.
  The session now binds exact tracked entities, store identity, ID, generation
  token and storage size, and rejects lost or altered intent before saving.
- Duplicate message IDs could fail after capacity was reserved. Admission now
  rejects already tracked IDs before reservation and compensates a failed add.
- Detached or accepted entries could leave capacity reserved after abort.
  Abort restores the original capacity count, bytes, EF state and modified
  flags while preserving unrelated business changes.
- A staged message could be saved without its capacity update, or with a
  changed storage size. Preflight now requires the exact capacity entry,
  pending state, and count/byte projection before SaveChanges.
- `SaveChanges(false)` previously left persisted entries staged, causing a
  later reinsert or capacity rollback. A successful EF save accepts only this
  session's own entries; unrelated business entries keep caller ownership.

Twelve requirement variants cover tracker clearing, detachment, duplicates,
`AcceptAllChanges`, foreign-store IDs, suppressed saves reporting zero or one,
previously committed IDs, `SaveChanges(false)` follow-on work and business
state, missing capacity, changed storage size, and caller-owned transaction
rollback. Real SQLite fresh-context checks distinguish EF tracking from
persisted data. New tests first failed against the old behavior. A controlled
counterprobe removing the post-save state check made the reported-one
suppression case fail; the guard was restored.

The focused class passed 28/28. The complete EF project passed 359/359 on
the exact implementation commit `ea3ce51ee`, with no failures or skips.
Independent read-only adversarial review: **PASS**, no concrete P1/P2 remains.

`CommitAsync` saves within the caller's DbContext; an outer transaction still
decides durability. The caller-owned EF SaveChanges pipeline must report actual
writes. Interceptors that fake an affected-row count while suppressing writes
are outside this trust boundary. No global 33-profile measurement was run for
this single coherent packet, per the larger-packet cadence.
