# R0 — binding of the twelve skills required by the Development Slice

Requirement `REQ-TEST-102`: "Every assigned writer and reviewer reads the required skill and .NET
extension itself and records its hash and applied rules."

Source of the list: `DEVELOPMENT_SLICE.json` field `scope.skills`, slice SHA-256
`c1865362bd7f5c1754d2e42e2af1b521f4878b28c8e1bee49fc7ee397cca45f8`. All twelve are present in the
local skill store. `treeSha256` is the SHA-256 over the sorted per-file SHA-256 list of the whole
skill directory, so it changes if any bundled reference file changes, not only `SKILL.md`.

| Skill | files | SKILL.md SHA-256 | tree SHA-256 |
|---|---:|---|---|
| `dotnet:binlog-generation` | 1 | `6a35bd9871239d314e36836bd425c93e50c295e9794166aef620bbd0754bca85` | `1413c11ecac84f221cc5f031c05d784dfbce7614cfd20ca9bd6f020d60fb8eac` |
| `dotnet:directory-build-organization` | 4 | `19236a9a5ccac500f1d2bc4023bbe5c427e4ec598220af7366257de686835ff8` | `b8cbc170a36a0c3b89d44c36b1cc5fd6af550387128deba8567b6d68e15ac53d` |
| `dotnet:msbuild-antipatterns` | 4 | `08b36b41196b1282fc96bee299cbabf10e876969be0d9abb5b40ae4f15665ae4` | `a26b64494d5fc079a0b93e4774f026a84ba344499d696dbfc03c2839ed01995a` |
| `dotnet:assertion-quality` | 1 | `efc92d1cbea09cf01e5e3b723bffa72ff4f6f8be352a2cd89fb502d6d27194e8` | `63a680482a3d1fca7e818ed2de03c7723a95dbcdcd39f64b3cafb8aed361f9c0` |
| `dotnet:code-testing-agent` | 2 | `bffc611d22e28c685ecddf2f2b327c80fc74d553a6c488a4cf3bd72ee3a3df4e` | `c3c6f36bd4ec119a600e56e6f540b30b77cdbd8433b86b2066a6fa1fb29ca1ee` |
| `dotnet:code-testing-extensions` | 22 | `211d1cdfc77a36bee0f2d2ee9095689899bceebe6adc5bfedcb8b5de5fa7fb47` | `ac77e4071e3b088e78e280ed4543a80ab75c48ca4d9d4433f9c18cb9268f7677` |
| `dotnet:find-untested-sources` | 3 | `b9e8d509604469418d998e18eecdadda3ee7e9afec612f6e11770053a9eef445` | `e0985be55274cf71ae4c1e3b496ed1c24284cd52b46e06621ca07ff8aaf81c0f` |
| `dotnet:run-tests` | 1 | `62a44beebf3d8ec988d171b3907cd3248b064b68ea6a1e9016ac32c6bc7100f5` | `84483fc292808cfe4818ea6f1e3cc0923705431615d50d30757f7d6cfe917ba3` |
| `dotnet:test-analysis-extensions` | 12 | `ec6f69ed00a627c0319a2bbdcfd71fcfab84eaae28900b2f814af3b27e44bd70` | `96e642b629906d9e93f05f0e4133ddab68f012e28da87d37f7155518241a686d` |
| `dotnet:test-anti-patterns` | 1 | `0f589352ba88c5873e85a2bde65d44f72c75ec76a0547105ff6698271d813bf0` | `5862ca62ebd2eab7bb972ab37b6c237fb5ce6131327408d4eaa4e457e3b4a53b` |
| `dotnet:test-gap-analysis` | 1 | `cfec7d0e13deaee15fb97151d0cd5339fa5f14c109a10c49de81e9a5512231b7` | `48e31354c95d24a7ab21c2f976a6b28d4b4f0834eba59e3946dce1b2d60436dc` |
| `dotnet:test-smell-detection` | 2 | `73888755e509cfd455db5c00df6aaa20b9e7d4d7dc4b485f4add2c71f652dd4e` | `f2e81c9a66fcb4d5d0bc8e01455d68e9da56b7a999fdff87a3a52f7b6191840f` |

## Read status

Honest separation: a skill counts as read only when its rules were actually applied or recorded.

### Read and applied in R0

**`find-untested-sources`** — executed exactly once as the slice requires. Applied rules: chose the
Roslyn engine because the repository is .NET-only and its namespace disambiguation beats identifier
overlap; used the narrowest repository root named by the caller rather than the parent workspace;
based the result on the analyzer JSON without re-guessing paths; reported the static-pairing caveat
and did not append build, coverage or test-run commands to it. Evidence:
`FIND_UNTESTED_SOURCES.md`.

**`run-tests`** — read to fix the exact command form the Lead plan section 8 requires. Applied
rules: detected the platform from `global.json`, `.csproj`, `Directory.Build.props` **and**
`Directory.Packages.props` rather than from memory; established that SDK 10+ passes MTP arguments
directly with no `--` separator and forbids the bare positional path; established that xUnit v3 on
MTP filters with `--filter-class` / `--filter-method` / `--filter-trait` and not `--filter
"ClassName=…"`; established that MTP reports TRX with `--report-trx` and never `--logger trx`, and
uses `--blame-crash` / `--blame-hang-timeout` and `--coverage` instead of the VSTest spellings.
Each of these was additionally verified by measurement rather than accepted from the text.
Evidence: `MTP_COMMAND_FORM_PROBE.md`.

**`code-testing-agent`** — read because it is the mandatory entry point and the source of the
`Research → Plan → Implement → Review` workflow the Lead plan section 3 references. Applied rules:
this work package is **broad scope**, so the `.testagent/research.md`, `.testagent/plan.md` and
`.testagent/status.md` artifacts and the full completion contract apply, not the focused-scope
shortcut; `find-untested-sources` is run once and its output consumed rather than re-derived;
every requirement becomes a checklist item before implementation, quoted verbatim; the final
response carries a compact `Requirement | Evidence` table whose behavioural rows cite exact test
names and whose evidence comes from a run that exited 0, never from an attempt; a failing generated
test is fixed at the assertion, never by `[Skip]`; and per its own troubleshooting section a full
non-incremental workspace build closes the phase work.

### Due later, not yet read — reported as unread

| Skill | Due in wave | Why then |
|---|---|---|
| `directory-build-organization`, `msbuild-antipatterns`, `binlog-generation` | F1 | They govern the central MSBuild layout, the fail-closed `Directory.Build.targets` rules and the binlog evidence the plan requires for evaluated-graph proof |
| `code-testing-extensions` | F1 / C1 | The .NET language extension is consulted when the repository has no representative test of the new shape yet — which is exactly the greenfield situation from F1 onward |
| `assertion-quality`, `test-gap-analysis`, `test-anti-patterns`, `test-smell-detection`, `test-analysis-extensions` | per cohort in C1–C4a, and completely before freeze | `REQ-TEST-107` requires assertion, gap, anti-pattern and smell review over the **complete** final estate; running them before the estate exists would produce a finding set that does not describe the delivered tests |

No skill in this table is treated as applied until its rules appear in a wave record with concrete
consequences. A hash recorded here without applied rules is a binding, not a claim of compliance.
