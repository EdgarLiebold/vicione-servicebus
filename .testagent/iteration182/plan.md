# Iteration 182 plan

1. Split the bounded packet into non-overlapping context-API, host-context/sanitization, and
   host-pipeline/dispatcher workstreams, each with an independently owned test file.
2. Establish focused parent behavior, implement only concrete correctness/API/comment fixes, and keep
   all central requirement and evidence changes under lead control.
3. Reconcile every new test against the public API and behavior checklist, then review assertion depth
   and empirically test the important single-cause mutation points.
4. Run focused Release tests, fresh focused Cobertura and method-level CRAP, then the complete Courier
   namespace and all unfiltered Core/EF profiles.
5. Run strict warning-free Release builds, requirement projections, format and scoped diff gates;
   update exact hash-bound manifests and evidence.
6. Create a normal commit and annotated tag, atomically push both without force, and independently
   verify the remote branch, tag object, and peeled tag before advancing to the next packet.
