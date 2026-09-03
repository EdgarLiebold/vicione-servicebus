# V5 payload, diagnostics, and analyzers mutation validation

Date: 2026-09-03

Each counted mutation changed one production mechanism, compiled the complete owning test project with zero warnings and
errors, and made the named native owner fail for the intended reason. Every mutation was removed before the next
mutation and before the final positive builds and regression run.

| ID | One-cause production mutation | Causal killing observation |
|---|---|---|
| M01 | make the serialized-body hard maximum exclusive | the exact-maximum case threw instead of succeeding |
| M02 | make the transport-envelope hard maximum exclusive | the exact-maximum envelope case threw instead of succeeding |
| M03 | make the MessageData threshold exclusive | the exact threshold selected offload instead of inline |
| M04 | invert missing-owner enforcement | an over-threshold body without evidence no longer failed loudly |
| M05 | reject an exact-capacity writer request | the exact bounded-writer case failed before its final byte |
| M06 | permit a one-byte oversized writer request | hostile growth crossed the configured owner capacity |
| M07 | disable the System.Text.Json body cache | the counting converter observed three application serializations |
| M08 | disable the raw JSON body cache | repeated body access serialized the application more than once |
| M09 | serialize the MessagePack application twice | the MessagePack counting resolver observed two body writes |
| M10 | treat inline MessageData without an address as stored evidence | an over-threshold inline-only send was incorrectly admitted |
| M11 | invert typed-bus runtime registration matching | the secondary bus consumed the default bus's admission policy |
| M12 | remove admission from the common physical send boundary | a rejected send reached the observer/provider path |
| M13 | remove Event Hub single-send admission | source-order ownership no longer found admission before observation and SDK I/O |
| M14 | remove Event Hub batch-item admission | one batch path could bypass admission before observation and SDK I/O |
| M15 | disable payload-level sensitivity redaction | a classified payload emitted its application data |
| M16 | stop inheriting sensitivity from base contracts | a derived sensitive payload was classified safe |
| M17 | invoke application `ToString` during diagnostics | the hostile application object executed and threw |
| M18 | disable control-character sanitization | newline/control injection survived diagnostic rendering |
| M19 | remove Unicode-scalar boundary protection | truncation emitted a split surrogate scalar |
| M20 | force URI rendering through `AbsoluteUri` | a relative URI threw instead of producing bounded text |
| M21 | replace the weak type cache with a strong dictionary | the collectible application type remained rooted |
| M22 | rethrow a hostile metric-listener/exporter exception | observation replaced the accepted result/original rejection |
| M23 | remove rejection's once-per-buffer guard | one rejection produced duplicate metric observations |
| M24 | report envelope rejection in the body histogram | the recorded stage dimension was incorrect |
| M25 | resolve RT-001 `PrefetchCount` by name only | a shadow/lookalike property incorrectly produced VOSB5002 |
| M26 | resolve `ConcurrentMessageLimit` by name only | a noncanonical property incorrectly produced VOSB5001 |
| M27 | resolve framework `Task` by type name only | a foreign `Task` type incorrectly produced VOSB5003 |
| M28 | analyze generated code | generated fixtures produced VOSB5001 through VOSB5005 diagnostics |
| M29 | invert the MessageData exemption | a MessageData member was reported as a large inline payload |
| M30 | suppress outbound message-contract analysis | the outbound-only large payload lost VOSB5005 |
| M31 | stop scanning inherited contract members | the inherited large member lost VOSB5005 |
| M32 | resolve `ExcludeFromTopology` by name only | a lookalike attribute incorrectly produced VOSB5004 |
| M33 | break compilation-end VOSB5005 deduplication | two consumers of one payload produced duplicate diagnostics |
| M34 | remove the VOSB5001 consumer-definition boundary | a nonconsumer definition property incorrectly produced a diagnostic |
| M35 | read `EmptyMessageData.Address` without checking `HasValue` | both missing-data request contracts timed out instead of returning the exact fault |

The audit also caught and corrected a nondeterministic test calibration. Separate live sends used NewId-derived timestamps
whose JSON fractional-second text can differ in length. The final JSON envelope-capacity owner uses fixed representative
metadata through the real production serializer and bounded buffer; three independent 16-case runs are green. This was a
test-quality defect, not a surviving product mutant.

No sampled high-risk mutation survived. M35 originated as a full-regression failure in the candidate implementation and
was retained as explicit causal evidence rather than being hidden by changing the established fault expectation.
