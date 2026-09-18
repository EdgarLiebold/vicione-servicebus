# Iteration 216 plan

1. Make builder mutation and materialization one synchronized freeze protocol; reject null
   activities atomically and use the public catch terminal implementation.
2. Give every behavior node deterministic owner-level null validation without changing successful
   continuation, visitor, probe, task, exception, or cache identity.
3. Forward the active context cancellation token into runtime fault dispatch and replace the strong
   exception-type cache with an ephemeron cache.
4. Add three independent deep-contract fixtures for builder/terminal, cached/adapter, and
   execution/fault-dispatch behavior.
5. Bind every new fact to the existing `REQ-VSB-STATE-MACHINE-ACTIVITY` or
   `REQ-VSB-STATE-MACHINE-FAULT` owner, run focused, Saga-wide, full Core,
   requirement-projection, provider and formatting gates, then perform isolated mutation checks
   and independent cross-audits before commit/tag.
