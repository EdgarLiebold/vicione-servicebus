# Core runtime, scheduling and lifecycle validation

Technical commit `23f5936ddee72d0f1606bd7a2601a88fa6401bd7` closes 34 obligations as one coherent package across delayed redelivery, recurring scheduling, service-instance configuration, built-pipeline resolution, in-memory lifecycle, message telemetry and deterministic test-harness timing.

- Fifteen new xUnit methods materialize 18 executable cases. The complete affected-class selection is 33/33 and the complete UnitArchitecture profile is 2,473/2,473 with no failed, skipped, pending or other cases.
- Nine fully replaced inherited files and 1,584 legacy test lines are removed. The Technical commit is net negative by 224 lines: 1,364 additions and 1,588 deletions.
- Full Release build and post-mutation restore build report zero warnings and zero errors. Scoped formatting, Verification Model, all 257 CI self-tests, all 148 identity self-tests and the generated 10,011-entry CHANGELIST are green.
- Six buildable one-cause mutations independently weaken redelivery count progression, message-id replacement, cron step projection, service-instance endpoint configuration, cancelled-scope renewal and trace baggage propagation. Every intended carrier turns red and the exact Technical source hash is restored afterward.
- The post-restore control is 10/10. The complete-profile evidence is reused from the single Technical validation run rather than rerun for each mutation.
- No empty inherited test directory remains. No external broker, database or cloud fixture was used, and no remote push was performed.
