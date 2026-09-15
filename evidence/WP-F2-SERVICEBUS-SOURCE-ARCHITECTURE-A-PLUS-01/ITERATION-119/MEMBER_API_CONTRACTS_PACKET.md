# Member API contracts and remaining nested source reconciliation

## Authority and bounded scope

The original unbounded whole-product A+ goal remains active. Input checkpoint:
9990fe12490d2330964c4e53945097c0688fbff6, already committed, annotated,
pushed without force and independently verified remotely. Normative hash
bindings remain unchanged; the ServiceBus direct-Lead slice applies, not the
separate Licensing exception. Protected review/result trees are not traversed.

The main personally reads nine complete state-machine partial files, 1,164
starting/current lines, and manually updates their comments immediately after
understanding the corresponding complete files. They contain the remaining
13 newly qualified nested type declarations: all 35 collision-corrected nested
identities now have complete personal source reads across the connected packets.
This is not complete personal reading of the 2,266-line main state-machine file,
the entire family, all source, all APIs or the original whole goal.

Comments explain actual context/message/exception proxying, observer forwarding,
selected-event no-op notifications, absent previous state, deliberate missing
correlation validation, explicit request-ID storage versus saga-ID fallback,
late request/schedule event assignment and state hierarchy/behavior precedence.
No executable statement, signature, visibility, project, dependency or directive
is changed in these source files; exact XML-line-stripped input comparison is
required before capture. Functional defects are not closed by truthful comments.

The main also personally reads the complete 391-line inventory input, the three
neighboring typed inventory test files, all manually authored tooling deltas and
the complete new 282-line test file. Complete Architecture-owner admission
reuses the verified complete c0ea82bf915b1f0c744cced8f80215bc6911f81b tree and
all subsequent personal reads/authored deltas. Existing Core execution is not
complete personal Core-owner reading and does not authorize new Core tests.

## Inventory correction

The actual formatter now preserves:

- Typed defaults for structs and generic parameters, distinct from concrete
  reference/nullable-value null; numeric raw enum constants and invariant literals.
- Required versus optional flags independently of default constants; params arrays
  and parameter collections distinct from ordinary arrays.
- C# byref keywords independently of ordinary direction flags: by-value [Out]
  arrays never become byref, and [In] ref never becomes readonly in.
- RequiresLocation/IsReadOnly parameter annotations; ordered required and optional
  custom modifier lists on parameters, returns, fields, properties and accessors.
- Init versus set while retaining IsExternalInit, readonly versus writable ref
  returns, volatile fields, override/final method shape and each accessor's modifiers.
- Character apostrophe escaping without applying that escaping to string literals.

Virtual ref readonly parameters use the required InAttribute modifier and a
RequiresLocation attribute. Optional location modifiers apply to function-pointer
signatures; they are not claimed here. This distinction is checked against the
[official C# design](https://github.com/dotnet/csharplang/blob/main/proposals/csharp-12.0/ref-readonly-parameters.md)
and independent compiled reflection assertions. The initial Learn URL returned
404; the actual authoritative GitHub source was then inspected.

No generator/script writes source, tests or comments; every change is handwritten
with apply_patch. Standard whitespace formatting is restricted to the new test
file and produces no byte change. No automatic API-baseline update/copy occurs.

## Research, plan and assertion quality

The testing skill's Research -> Plan -> Implement workflow is performed inline,
under the user's no-generator requirement. Research/plan/status retain bounded
scope and the original whole-product obligations. The single tested source target
is PublicApiBaseline.cs; the previous once-only safely scoped tool pairing result
is not relabeled as missing external Architecture tests or runtime coverage.

PublicApiMemberModifierTests contains 20 methods / 43 native cases, with 20 exact
REQ-VSB-PACKED-PUBLIC-API tuples; the catalogue grows from 155 to 175. All fixtures
are typed compiler inputs for reflection, not production dummy implementations.
There are 54 physical Assert calls, including two shared selection-helper calls:
34 Equal, six Single, five True, three NotEqual, two False and one each Contains,
Empty, Null and StartsWith. All 20 methods contain meaningful exact or semantic
prefix assertions; no assertion-free, trivial-only or self-referential test.
Three explicit non-collision pairs supply negative equality checks (15% of
methods). Ordered collection assertions preserve field/modifier shape. Metadata
guards are paired with actual formatter-output assertions. Exception mocks are
not appropriate for this metadata formatter and are not added for diversity.

Test names and variants are exact in the hand-maintained compiled requirement
projection. Constructor/indexer defaults, character versus string delimiters,
ordinary/static/virtual event accessors and override property accessors exercise
shared paths rather than inferring their behavior solely from method coverage.

## Actual causal observations

Raw outputs are retained only in the owned directory
/private/tmp/vsb-iteration119-member-modifiers.EXN6Ek.

| Observation | Actual terminal outcome |
| --- | --- |
| Initial strict owner compile | 0; zero warnings/errors; 61.44s |
| Original formatter, initial tests | Native 2; 22 total, 15 functional failures, seven passes, no skips; 28.602s |
| Initial corrected strict compile | 0; zero warnings/errors; 13.99s |
| Initial corrected inventory/projection tests | Native 0; 62/62 passed, no skips; 23.539s |
| Expanded final red strict compile | 0; zero warnings/errors; 7.54s |
| Expanded special-case red | Native 2; 43 total, seven functional failures, 36 passes, no skips; 23.999s |
| Expanded corrected strict compile | 0; zero warnings/errors; 11.30s |
| Expanded inventory/projection green | Native 0; 83/83 passed, no skips; 24.589s |

The seven expanded failures are two readonly-location cases, four direction-flag
cases and the unconstrained generic default. Struct/unmanaged constrained generic
defaults already pass: the advisor's blanket IsValueType assertion for those
constraints is qualified by actual runtime evidence, not repeated as fact.

The initial unexecuted virtual-location oracle guessed a modopt payload; the
official encoding and independent assertions correct it before native execution.
Intermediate compilation outcomes are not counted as separate causal test passes.
A sandbox process-list diagnostic is unavailable (sysmond absent); it is not
build/test evidence and is not retried as an implementation strategy.

## Internal counterreview and connected findings

A separate internal gpt-5.6-sol advisor completely reads the 440-line corrected
inventory and 164-line initial test input (604 lines), without edits, tests,
builds, mutations, delegation or broader traversal. This is internal read-only
Lead advice, not independent external/product acceptance. Exact entry=exit hashes:

- Tool: 387b392dd17b351f5853e2906b797b246aa240d6e85df33594c63d842e53efc6.
- Initial test: aa30d59a9c4526ed7e0b8fd503d72fc1772b51227a6b95c549d631950e892417.

Final bounded internal counterreview completely reads the actual 449-line tool
and complete 282-line expanded test (731 lines), with unchanged entry/exit
checksums a4178ebc... and 65d5804c... bound in the manifest. It finds no unresolved
high/medium correctness defect in this default/byref-direction/modifier/accessor/
literal repair. It explicitly corrects the earlier constrained-generic claim
and retains all larger inventory and fixture gaps. It performs no execution:
the supplied native/mutation outcomes are main execution evidence, not an
independent replay or external acceptance. The main also personally reopens
the complete corrected 449-line tool after all mutations are restored.

The direction, readonly-location and unconstrained-generic findings have causal
red and green evidence above. Constant-only-required, string-apostrophe,
constructor/indexer, event and override-accessor missing oracles are added.
Nonempty optional and multiple modifier lists, undefined/signed/unsigned/alias
enum boundary fixtures and independently differing accessor contracts remain
connected follow-up; absence of a fixture is not an observed surviving mutation.

The larger inventory still needs recursive member read/write nullability,
conditional flow attributes, function-pointer conventions/modifiers, tuple names,
dynamic position annotations and annotated generic-constraint binding. Existing
generic raw nullable vectors are not falsely described as completely invisible.
No whole public-API acceptance follows from this bounded correction.

Source findings remain behavioral obligations: required proxy/observer inputs;
late request/schedule initialization and honest nullable storage; request-ID null
guards and accepted-response snapshot ownership; observer failures preserving the
primary error; empty-name equality versus null and state hierarchy cycle/ownership
consistency; lifecycle versus declared-event semantics. NST-01 through NST-10 and
all earlier saga/provider/cancellation/losslessness obligations remain open.

## Terminal validation before capture

### Compiled one-cause test-gap checks

Each candidate is applied manually to the otherwise green tool, strictly
compiled, executed natively against the complete 43-case new class, then
manually byte-restored. Every compile is successful with zero warnings/errors;
every native run terminates 2 with the following actual assertion failures.
All ten restorations independently match the exact a4178ebc... green checksum.

| Candidate | Single change | Failed cases |
| --- | --- | --- |
| M01 | Remove generic typed-default recognition | 1 |
| M02 | Remove optional-without-constant marker | 2 |
| M03 | Remove params marker | 1 |
| M04 | Replace the init marker's required type | 1 |
| M05 | Drop required custom-modifier payloads | 4 |
| M06 | Omit sealed on concrete overrides | 2 |
| M07 | Drop explicit parameter direction metadata | 4 |
| M08 | Replace the RequiresLocation annotation match | 2 |
| M09 | Remove char apostrophe escaping | 2 |
| M10 | Replace invariant conversion with CurrentCulture | 1 |

Ten selected empirical kills / 20 failed cases are not exhaustive mutation
coverage, whole-source coverage, independence certification or universal correctness.
All underlying strict build/native logs are checksum-bound in the manifest.

### Final restoration regression

Serial final strict builds terminate 0 with zero warnings/errors: Architecture
7.30s, Core 38.33s, Abstractions 6.04s. Fresh full Core terminates 0 with
4,007/4,007 passed, zero failures/skips, 32.353s. Fresh full Abstractions terminates
0 with 749/749 passed, zero failures/skips, 1.774s. Three explicitly scoped
whitespace checks terminate 0, no output/warnings/writes, for the nine source
files, actual tool and new test. Fresh unfiltered Architecture terminates 0 with
401/401 passed, zero failures/skips, 3m 47.726s. The actual
EveryMethodName_MatchesItsAsynchronousContractBidirectionally test passes
(193,678ms). These three complete selected owners total 5,157 passed cases;
this is not execution of every test owner in the product.

The fresh package gate terminates 1 only at comparison against the unchanged
committed API baseline. Before that comparison, the fail-fast gate completes
31 package builds, 18 developer journeys, three isolated package consumers and
30 runtime assembly inventories. The fresh API output contains 20,045 lines,
SHA256 ed29376e3e214ede55083913ddd8d12d0dcea080075d7472d0f51ca8b4d7ddde.
The committed baseline remains 18,824 lines / SHA256
59ea05a49d8d99e64715ac60b79bc68f9b657948f0742fd9c3d3e972babd054b.
Improved metadata fidelity intentionally changes the fresh inventory; the old
baseline mismatch remains open, with no automatic update or green gate claim.
No cloud/provider acceptance or whole-API losslessness certification follows.

All owned validation handles are terminal before capture. The manifest binds
nine input/current source rows, three current contract inputs and 46 terminal
raw artifacts, including the final native CTRF reports. Git capture and remote
security are reported only after their own actual terminal outcomes.
Current fresh global line/branch coverage is not measured here; historical Core
coverage is not relabeled as current or whole-product coverage. The original
final multidimensional A+ gates and full personal source reading remain open.
