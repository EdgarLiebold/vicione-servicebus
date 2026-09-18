# Iteration 215 status

- Scope: seven state-machine graph snapshot, node, edge, extension and visitor sources, now admitted.
- Progress: 772/4,118 personally read C# sources (18.747%).
- Product outcome: graph node defects now win before edge enumeration; visitor construction and all
  visit collaborators have exact null boundaries; state, event and exception contexts restore on
  failure and nested traversal without breaking the established top-level event protocol.
- Reentrancy: typed and untyped nested events, nested exception scopes, continuation failures and
  scope-depth balancing preserve the exact outer transition/composite source.
- Compatibility: ordered state-local event occurrences, disconnected declarations, inheritance,
  transition, exception and composite edges remain stable and de-duplicated across repeated graph
  projections.
- Tests: 19/19 new focused contracts; 27/27 new plus existing graph/projection contracts;
  1,130/1,130 saga-wide; 5,911/5,911 full Core; and 249/249 EF unit.
- Requirement projections: Core, EF unit and EF local integration are each 1/1 green.
- Builds: final Core, EF unit and EF local Release builds are warning-clean and error-free.
- Formatting: scoped test and product `dotnet format --verify-no-changes` gates are clean.
- Mutation proof: 15/15 compiled isolated material mutants killed and restored. One initial typed
  event restoration survivor caused a causal test strengthening and was killed on rerun.
- Coverage: 266/267 owner class-line entries and 114/116 owner branches. Graph, extension, node and
  visitor classes are 100% line/branch covered; the sole uncovered edge line is the default arm
  unreachable behind the constructor's prior defined-enum guard. The two residual edge branches are
  that same unreachable default and the `Kind` inequality that cannot occur for identical endpoint
  references because the validated endpoint-kind matrix admits exactly one kind per pair.
- Maximum owner-method CRAP: 18.040 for `StateMachineGraphEdge.HasValidEndpoints`; its only gap is
  the explicitly disposed unreachable default arm above.
- Coverage artifact: `/private/tmp/vsb-iteration215-core-final2.cobertura.xml`, SHA-256
  `fe025aa841a14e4a0794926a3ae798be0ce6d76d5228db3090a5f72d9f928092`.
- Three independent Sol-xhigh final cross-audits of the disjoint and integrated packets report no
  findings after three remediation cycles.

## Final source manifest

- `717b3b4008f476a8607113974a2d315928f132fba9f393e1871f7defbad189cb` — `StateMachineGraph.cs`
- `07c86f094ccd9b238bd26f295ddd47847d266d7cd149d0d44b9c140ec9d62c0b` — `StateMachineGraphEdge.cs`
- `9c84ae1af771811e44a2269fbcfdd02131f50ae175050a24b8654b74e995caaf` — `StateMachineGraphEdgeKind.cs`
- `da24945b3edec6d7c82d093df2636ec7c40eb3c1c441316aba141e852d18f343` — `StateMachineGraphExtensions.cs`
- `f53f4c7ddc75633b715815cb915e7c4046982dd47302fa16716bf622def13bd3` — `StateMachineGraphNode.cs`
- `89136700d5572b321f2687eee35891b625592ff03eacc1e25b749b60b07acf24` — `StateMachineGraphNodeKind.cs`
- `77aef1bff5c45ac9e266ce0ea652ab9e5aa1d0946083b7d46cb0af404b8d22d2` — `StateMachineGraphVisitor.cs`

No unresolved correctness, reentrancy, ordering, ownership, public-contract, nullability,
formatting or material test-risk finding remains in this packet.
