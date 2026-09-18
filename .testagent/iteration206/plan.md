# Iteration 206 plan

1. Harden only demonstrated owner-boundary defects in the 12-source packet while preserving valid
   pipeline composition and public compatibility.
2. Add three disjoint deep-contract suites with unique requirement IDs/variants for observer,
   missing-redelivery, and public/partition behavior.
3. Integrate requirement projection centrally; run scoped format, serial warning-clean builds,
   focused tests, saga-wide and full-Core regressions, EF persistence gates and projections.
4. Execute isolated compiled mutation probes, calculate exact owner coverage/CRAP, hash manifests
   and evidence chains, then commit and annotate the iteration without pausing the parent goal.
