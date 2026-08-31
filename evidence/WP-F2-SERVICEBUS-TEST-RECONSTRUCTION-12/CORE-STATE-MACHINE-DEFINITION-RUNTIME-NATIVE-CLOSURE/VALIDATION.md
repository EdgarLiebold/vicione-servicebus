# Core state-machine definition/runtime native closure

Technical subject: `dce359526505bb386c93d121daaf7a933723f23b`, tree
`c2f60f87ad2e98e59c5a29d3374286ec85942cc7`, direct parent
`f8ce8649d96b9105af52b322efe85be59a7fe514`.

This package closes 120 unique `OBL-R0-CORE-A` obligations with executing xUnit 4/MTP v2
UnitArchitecture owners. The replacement adds 22 executable definition, storage and runtime cases and
strengthens three existing visualizer cases. It retires 27 completely replaced inherited spec files and
their two private serializer helpers. The remaining inherited Automatonymous and Dynamic Modify areas
contain 14 and 8 files respectively; they are intentionally retained because their Group, composite,
fault, retry, dependency and substate responsibilities are not part of this package. No empty legacy test
directory remains.

Positive verification:

- complete UnitArchitecture solution: 2,640 passed, 0 failed, 0 skipped across 21 CTRF files, above the
  active floor of 2,635;
- focused post-restore carriers: definition 4/4, state storage 8/8, runtime 10/10 and visualizer 3/3;
- passive requirement projection versus compiled `RequirementCoverage`: 1/1;
- locked Engineering Release restore and build: exit 0, build with 0 warnings and 0 errors;
- scoped format verification: PASS. The first attempt hit the known macOS sandbox denial for MSBuild
  named-pipe/process access; the identical command and inputs passed outside that sandbox, with its binlog
  retained here;
- verification model: PASS after retiring the three deleted legacy visualizer anchors and assigning native
  xUnit/MTP evidence ownership;
- CI tool self-tests: 257/257 passed;
- identity self-tests: 148/148 passed;
- generated CHANGELIST: PASS with 10,800 entries.

Mutation closure:

- M01-M15 each changes one product behavior and each product mutation builds successfully;
- all 15 mutants are killed by their intended behavioral owner;
- the mutation CTRFs contain 55 executions: 25 independent controls passed and 30 intended cases failed,
  with no skip;
- every mutation is reconstructed from the frozen Technical blob by exact occurrence selection. M07
  deliberately selects occurrence index 1 of 2; all other replacements have one occurrence;
- all eight touched product files were restored byte-for-byte to their Technical SHA-256 before the final
  build and positive 25/25 run.

The product axes include event/reachable-event enumeration, raw/string/integer state storage and
predicates, DuringAny, nested raise dispatch, selected-event filtering, unhandled policy, direct
transition targets, transition hook binding and Graphviz/Mermaid catch-node metadata. The visualizer
assertions compare exact output derived from real declarative and dynamic machines rather than a copied
implementation model.

No provider, broker, fixture or LocalIntegration runtime was started because this package changes no such
path. The Engineering solution build still compiles the complete LocalIntegration graph. Repository
identity and CHANGELIST checks ran only after the complete 127-file Evidence inventory was materialized;
their raw outputs and all 126 nonmanifest SHA-256 values are bound here.
