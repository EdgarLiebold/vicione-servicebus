# Iteration 153 — EF relational-infrastructure admission

1. Personally read the complete execution-strategy, transaction-context, identifier and locking
   owner together with its direct unit/local tests and requirement support.
2. Reproduce and correct cache-identity, mapped-identifier and provider-configuration boundary
   defects without widening the public API.
3. Add causal owner tests, validate their assertion depth and kill isolated compiled mutations for
   every productive correction.
4. Run fresh MTP coverage/CRAP, static source-to-test pairing, strict Release builds, format gates,
   local-provider truth checks and the complete Core regression gate.
5. Freeze exact manifests, publish bounded evidence, commit, annotate, push normally and verify the
   remote branch plus tag objects.
