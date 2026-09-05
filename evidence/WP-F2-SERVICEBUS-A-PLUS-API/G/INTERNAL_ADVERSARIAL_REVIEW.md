# Internal adversarial review

This is a self-review, not an independent product acceptance.

| Attack | Observation | Disposition |
|---|---|---|
| Add a file whose name starts with `Journey` but omit it from the expected sequence | Exact ordered inventory equality fails | Protected |
| Change the shell success message while leaving no count guard | The gate now validates the count before any pack operation | Protected |
| Compile examples against source projects | The sample project contains no `ProjectReference`; the gate restores only freshly packed packages | Protected |
| Keep an example that starts without mandatory message limits | Every bus-registration journey now declares limits; the test harness supplies its own conservative policy | Protected |
| Describe inbox retry without its terminal operator state | Reliability architecture assertions require every state and acknowledgement name | Protected |
| Reintroduce internal implementation-history language into user documentation | All current product documents are scanned, with only the legally required provenance paragraph masked | Protected |
| Publish a partial API change guide | Tests require all three sections, principal async forms, unified registrations, and all seven capability packages | Protected |
| Claim provider acceptance more broadly than implemented | Documentation points to the machine-readable capability matrix and names only RabbitMQ and in-memory acceptance | Protected |
| Treat this self-review as independent approval | Explicitly prohibited here and in the program deviation record | Protected |
| Register an Entity Framework delivery source without a loop owner | PostgreSQL transactional-outbox tests require committed rows to dispatch through the common hosted service | Protected |
| Return UTC SQL Server timestamps as offset-less provider values | A real-provider test inspects both fetch procedures' field types, exact values, and zero offsets | Protected |
| Admit headerless raw JSON implicitly | Core type-admission tests reject the secure default; the named Functions test opts in explicitly | Protected |
| Remove the scheduler token's correlation effect | Quartz provider and unit tests require token correlation while preserving an explicit business correlation | Protected |

Repository-wide acceptance found related runtime defects in work delivered by the preceding packages.
Those defects were corrected rather than excluded from package G. Product risk is covered by focused
regressions plus the complete Unit, SQL Server, PostgreSQL, RabbitMQ, and Azure Service Bus profiles.
