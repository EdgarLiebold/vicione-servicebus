# Product-wide ServiceBus profile at 7c8a93e05 and focused follow-up

## Complete exact-commit profile

- `artifacts/t148-aggregate.json` (SHA-256 `dd042a5b28b76f550b85070ab13e637eb4eda7d0c2637228eefbdd4e2d9215af`) merges 33 verified receipts from the same source and test trees at commit `7c8a93e057153d5baa8b3eab0206c1316b1e27d4`.
- All 32 product assemblies, every Unit and LocalIntegration project, and the no-AVX2 and scalar fallback profiles are present. All 14,122 recorded tests passed, without failures or skips in the selected receipts.
- Lines: 87,549/94,777 = 92.37368%. Conservative branches: 31,820/37,382 = 85.12118%. The conservative branch merge does not infer common branch identities between independent Cobertura reports.
- One of 26,192 measured methods exceeded CRAP 30: `MessagePackSerializerContext.TryGetCancellation`, 33.1048, with 35/39 covered lines and complexity 32. No other method exceeded 30.

The first ActiveMQ fixture run had one failed Artemis assertion: after the handler and `PostReceive` completed, Artemis JMX still showed the scheduled message in delivery (`added=1, acknowledged=0, queue=1, scheduled=0, delivering=1`). The isolated full retry passed 106/106 and supplied the profile receipt. Red Team inspected the provider acknowledgement path and found that `PostReceive`, `StopAsync`, and `flushExecutor()` do not create a causal fence for Artemis acknowledgement accounting. There was no broker fault or redelivery evidence. The test at `29cb9982d` now checks stable broker accounting for Artemis while preserving the exact completed statistics for ActiveMQ Classic. Its scheduling class passed 11/11 against both isolated broker fixtures; no wall-clock wait was introduced.

## Focused correction after the profile

- `29cb9982d` adds two product-behavior tests for malformed exception graphs: an empty aggregate branch and a `GetBaseException()` override that throws must not turn a mixed failure into pure cancellation. Full MessagePack passed 124/124 and Architecture passed 450/450.
- `6bad502ae` extracts normal Inner/Base exception edge discovery without changing graph order or classification. Red Team reviewed the exact diff and found no concrete P1/P2. Full MessagePack again passed 124/124.
- `artifacts/t150-messagepack/receipt.json` (SHA-256 `1f93a22382ab8e3ca2fbe858e16b664b9120ed5d73f8a719618e788bbc69168a`) is bound to `6bad502ae` and verifies 124/124 tests. Its Cobertura report (SHA-256 `78d8e55efcea70ff517b5c41b7a7b57709cfa8aaf747ff88cbe5c60e705b28c2`) shows `TryGetCancellation` at 100% Line/Branch and complexity/CRAP 22, and `TryPushExceptionCauses` at 100% Line/Branch and complexity/CRAP 8.

The 33-profile figures above describe `7c8a93e05`, not the later commits. A new product-wide aggregate for `6bad502ae` would require fresh receipts for all 33 profiles; no current global figure is inferred from the focused report. The current evidence closes the one measured CRAP hotspot locally and identifies no product defect in the reviewed Artemis and MessagePack paths. It cannot prove the absence of all source-code defects.
