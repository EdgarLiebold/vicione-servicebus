# C33 product-path analysis — fault diagnostics and host metadata

## Scope

This cohort owns the complete product paths needed by the two inherited exception-data cases and
the inherited host-metadata round trip:

- `FaultExceptionInfo` creation from ordinary, application-wrapper and remote exceptions;
- the System.Text.Json string/object dictionary converter used by fault envelopes;
- `BusHostInfo` current-process capture and `HostMetadataCache` lifetime ownership;
- real in-memory fault publication and host-envelope serialization.

Message timestamps are not part of this cohort. Their common `TimeProvider` normalization remains a
separate path-complete item in `TODO.md`.

## R0 disposition

| R0 identity | Replacement | Disposition |
|---|---|---|
| `OBL-R0-CORE-D-0154` | `FaultExceptionInfoTests.ApplicationDiagnosticData_IsCarriedByExactlyOnePublishedFault` | replaced, executing |
| `OBL-R0-CORE-D-0155` | `FaultExceptionInfoTests.ExceptionDiagnosticData_IsCarriedByExactlyOnePublishedFault` | replaced, executing |
| `OBL-R0-CORE-D-0173` | `HostMetadataRoundTripTests.PublishedMessage_CarriesEveryHostFieldAcrossTheSerializationBoundary` | replaced, executing |

The first two replacements observe exactly one publication and then assert the received fault after
an actual serializer/receive boundary. A publish-hook object is not accepted as round-trip evidence.

## Product findings and dispositions

1. `FaultExceptionInfo` previously aliased generic exception-data dictionaries but copied other
   `IDictionary` implementations. The same API therefore had two lifetime and comparer contracts.
   It now always creates one detached ordinal-ignore-case snapshot of string-keyed non-null values.
2. Application-wrapper values retain precedence, but the wrapped exception remains the reported
   fault identity. Remote `ExceptionInfoException` identities and recursive inner chains remain.
3. `CaseInsensitiveDictionaryStringObjectJsonConverter.Write` selected its own overload again.
   The implementation is now unambiguously named `WriteEntries` and is exercised across the wire.
4. `BusHostInfo(bool)` ignored its argument. It is removed. The public parameterless constructor is
   retained solely for wire materialization; one internal `CaptureCurrent` factory owns production
   capture through current .NET runtime APIs.
5. `HostMetadataCache` continues to own exactly one current and one empty snapshot. All eight wire
   members and their cache lifetime are covered.

No useful feature is removed and no backward-compatible API shape is retained merely for history.
