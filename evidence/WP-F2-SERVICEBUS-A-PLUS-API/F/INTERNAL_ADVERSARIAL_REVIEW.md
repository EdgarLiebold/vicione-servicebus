# Internal adversarial review

This review was performed by the implementing Developer AI. It is internal adversarial evidence only.
It is not an independent Developer Red Team, external review, or architecture acceptance.

## Challenges applied

- Evaluated the actual MSBuild `Compile` and project-reference graph instead of relying on source
  folders, namespace text, or successful compilation alone.
- Attempted to load every capability-owned type from both foundation assemblies and rejected duplicate
  full names or a reverse assembly reference.
- Built all Engineering projects so local-integration, benchmark, sample, provider, testing, and tool
  consumers had to declare their capability dependencies explicitly.
- Packed every new package and compiled 14 clean consumer scenarios against 15 freshly generated local
  NuGet packages, preventing project-reference leakage from making the sample gate pass.
- Ran SuiteComposition and asserted limits, SQLite reliable messaging, consumer delivery, request/
  response, scheduled delivery, and its loaded product-assembly allow-list.
- Contributed an unknown third-party `IConsumerKind`, challenged valid and invalid harness-observation
  shapes, deliberately overlapped it with the core consumer fallback, and verified primary plus
  companion endpoint behavior through real message delivery.
- Applied three one-cause mutations to harness observation, endpoint materialization, and owner
  precedence, then repeated the complete acceptance profile after restoring the source.

## Findings closed during review

- The first Engineering build exposed 272 compile errors in ten local-integration projects. Their
  former transitive access to optional capabilities was replaced by the minimal explicit project
  references and global usings required by each project.
- Saga-specific retry/outbox extensions were missing in Azure Table, DynamoDB, and EF Core integration
  tests after the split. Each project now imports the saga configuration namespace explicitly.
- Telemetry tests had an `ActivityContext` name collision after the new Advanced contract was
  introduced. Explicit aliases retain the intended `System.Diagnostics.ActivityContext` behavior.
- The testing harness initially ignored a new kind whose name was not one of the built-in strings.
  Structural observation now supports third-party consumers, sagas, state machines, and activities and
  rejects unsupported shapes with one actionable configuration message.
- The first owner-precedence mutation survived because the probe did not overlap the core fallback.
  The test now creates the overlap deliberately and kills the mutation at startup.
- The full formatter found ten import-order differences after capability usings were added. The files
  were formatted, and both complete warn/error format gates now return exit 0.

## Environment diagnosis

NuGet restore stalled inside the filesystem sandbox at project discovery but completed in 7.072
seconds outside it. `dotnet format` failed inside the sandbox because MSBuildWorkspace could not create
its named pipe; both complete gates passed outside it. Package builds were stable with build servers
disabled and serial MSBuild. These are execution-environment constraints, not product exceptions or
test skips.

## Residual boundary

No cloud account, broker, or database service is required by package F. The sample uses the real SQLite
store with the in-memory transport as specified. Provider-specific cloud acceptance remains exactly
where its provider integration profiles own it; no unexecuted cloud behavior is reported as green.
Formal independent review remains a separate action on a frozen commit.
