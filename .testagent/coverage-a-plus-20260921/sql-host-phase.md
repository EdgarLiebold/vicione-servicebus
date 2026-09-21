# SQL host configuration A+ slice

## Product defects fixed

- SQL address credentials truncated passwords at the second colon. URI-decoded passwords now retain
  their complete suffix.
- Mutable SQL host settings accepted whitespace instance names, invalid virtual-host and area
  symbols, an area below the root virtual host, out-of-range TCP ports, and PostgreSQL Unix-socket
  paths that cannot be represented by the transport URI.
- PostgreSQL host parsing silently accepted malformed later hosts, empty list elements, invalid
  ports, broken brackets, and invalid IPv6 values. Every segment is now validated before any setting
  is changed, so a failed connection-string replacement is atomic.
- An inline PostgreSQL port could disagree with the actual `NpgsqlDataSource` target. This included
  the explicit default port `5432`, which a competing global port could overwrite.
- `GetDataSource()` retained stale builder values and could ignore later configurator changes to
  host, port, database, schema, username, or password. It now rebuilds from the current settings
  while preserving unrelated provider and security options.
- Assigning the first host of an existing multi-host list through the public configurator left the
  old list active. An explicit host assignment now collapses that list even when the assigned value
  equals its former first host.

## Hard behavior evidence

- The final SQL Unit/Contract run passed 190/190, with 0 failures and 0 skips.
- The complete Release Unit/Architecture profile passed 9,840/9,840, with 0 failures and 0 skips.
- The red run for the last review findings passed 187 existing cases and failed exactly the two
  explicit-default-port cases plus the equal-first-host override case.
- Tests assert the effective `NpgsqlDataSource` connection string, public bus address, complete
  validation keys, exact exception parameters, state after a failed replacement, and preservation
  of SSL mode and application name.

## Coverage and CRAP

Fresh focused report:
`artifacts/coverage-a-plus-20260921-sql-host-final2`.

- Generic SQL focused lines: 1,008/2,120; branches: 422/817.
- PostgreSQL focused lines: 206/1,011; branches: 149/340.
- Every selected host-parsing and validation hotspot is below CRAP 30.

| Baseline hotspot | Baseline CRAP | Reviewed result |
| --- | ---: | --- |
| `ConfigurationSqlHostSettings.Validate` | 49.85 | split into CRAP 18 and 16 helpers; 100% lines and branches |
| `ConfigurationSqlHostSettings(Uri)` | 42 | CRAP 8; 13/13 lines, 7/8 branches |
| `PostgreSqlHostSettings.ParseHost` | 203.47 | CRAP 14; 18/18 lines, 14/14 branches |
| PostgreSQL segment parser | part of `ParseHost` | largest helper CRAP 12; all parser helpers have 100% lines and branches |
| `SqlHostAddress(string, ...)` | 20 | CRAP 26 after stricter contracts; 21/21 lines, 26/26 branches |

The focused report observes five assemblies. It does not replace the next fresh 32-assembly
product-wide aggregate. Receiver and provider-runtime hotspots remain open in later SQL slices.

## Microsoft grade-tests assessment

All 16 added test methods were graded against the xUnit guidance from the Microsoft `dotnet/skills`
test-analysis extension. Distribution: **16 A, 0 B, 0 C, 0 D, 0 F**. The accompanying
test-anti-pattern review found 0 Critical, 0 High, 0 Medium, and 0 Low findings.

| Test | Grade | Note |
| --- | --- | --- |
| `AddressConstructor_DecodesCredentialsWithoutLosingPasswordSuffix` | A | Encoded and literal colon suffixes have exact independent password oracles. |
| `Defaults_ExposeTheOperationalContract` | A | Every operational default is asserted with an exact value. |
| `Validate_ReportsEveryInvalidOperationalBoundary` | A | Exact validation keys prove all simultaneous failure boundaries. |
| `Validate_RejectsPortsOutsideTheTcpRange` | A | Both sides of the TCP range fail with the exact key. |
| `Validate_AcceptsEveryValidOperationalBoundary` | A | Minimum and maximum accepted values form positive controls for the failures. |
| `Validate_ReportsInvalidAddressComponentsBeforeHostAddressFormatting` | A | Mutable invalid host, instance, virtual-host, and area values fail validation first. |
| `Validate_RejectsAnAreaWithoutANamedVirtualHost` | A | The unrepresentable root/area combination fails at validation and formatting boundaries. |
| `Validate_RejectsUnixSocketHostsSetThroughThePublicConfigurator` | A | Both PostgreSQL socket forms fail validation and address projection. |
| `HostSettings_ProjectEverySupportedHostShape` | A | Hostname, inline port, IPv6, bracketed IPv6, and multi-host forms have exact projections. |
| `HostSettings_RejectMalformedHostSegmentsBeforeRuntime` | A | First and later malformed segments, ranges, brackets, IPv6, empties, and sockets are rejected. |
| `ConnectionStringInlinePort_UsesTheSameBusAndDataSourceTargetAsync` | A | Nondefault and explicit default ports defeat a conflicting global port in both targets. |
| `OptionsInlinePort_UsesTheSameBusAndDataSourceTargetAsync` | A | The options path proves the same effective target through the created data source. |
| `HostSettings_ReportMissingHostThroughConfigurationValidation` | A | Null, empty, and whitespace hosts fail validation and address materialization. |
| `FailedConnectionStringReplacement_PreservesEveryPriorSettingAsync` | A | A bad later segment preserves all prior routing, credentials, database, and provider options. |
| `HostSettings_RebuildDataSourceFromCurrentValuesAndPreserveSecurityOptionsAsync` | A | Current configurator values replace stale builder state without losing SSL or application options. |
| `HostSettings_ExplicitFirstHostOverride_CollapsesPriorMultipleHostListAsync` | A | The equality edge case is observed through state, bus address, and effective data-source host. |

## Pseudo-mutation discriminators

- Splitting user information at every colon fails both credential-suffix cases.
- Dropping any validation branch changes the exact failure-key set or admits an address that must
  fail materialization.
- Skipping validation of later host segments makes the malformed-later-host cases succeed and
  breaks the atomic-replacement oracle.
- Applying the global port after an explicit inline port fails the bus/data-source agreement cases,
  including the explicit default port.
- Reusing the retained builder without overwriting current settings fails the complete rebuilt
  connection-string assertions; rebuilding from scratch loses SSL mode or application name.
- Selecting a multi-host list by comparing the current host value with its former first value fails
  the equal-first-host override regression.

## Adversarial review

The first read-only review found target divergence between host settings and the effective Npgsql
builder, weak malformed-list and atomicity coverage, accepted Unix sockets without a representable
bus URI, and the invalid root-virtual-host/area combination. A second review found the explicit
default-port collision and the equal-first-host override defect. Product code and discriminating
tests were corrected after both rounds. The final review, followed by a separate review of the CRAP
refactoring, returned **PASS with no further concrete findings**.
