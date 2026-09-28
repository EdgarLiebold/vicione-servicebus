# T78 Azure Table saga native-property restoration

The frozen T74 profile identified uncovered branches in native Azure Table
saga property conversion. Existing tests cover a populated round trip,
reserved-name isolation, malformed serialized values and storage limits.
This packet adds three behavioral tests through the real entity converter
and Azure Tables SDK `TableEntity` representation. The T75 Roslyn pairing
artifact and frozen T74 profile were reused for selection. No production
implementation changed.

`EntityConverter_RestoresSdkOffsetValuesAsExactUtcDateTimesAndPreservesOffsetProperties`
uses four distinct non-UTC offsets. It proves required and nullable CLR
`DateTime` properties contain the exact UTC instant and `Utc` kind, while
`DateTimeOffset` properties retain their full offset values.

`EntityConverter_RejectsWrongNativeStorageTypesWithoutCoercingOrDefaulting`
tries twenty-one corrupted required and nullable native fields, including
integer-width, identity, timestamp, duration, binary, URI, version and text
mismatches.
Each must fail with its own stored
field name and target type, even with another valid field present.

`EntityConverter_DistinguishesAbsentNullableValuesFromPersistedFalseZeroAndEmptyValues`
proves an all-null state contributes no table properties, while explicit
false, zero, zero duration, empty binary and empty text remain distinct,
persisted and restored; absent Guid and DateTime values stay null.

The affected xUnit v3/MTP entity-converter class passes 29/29 after the
Red Team counterprobes expanded the malformed-value matrix
to nullable converters and the required duration wrong-type path. Assertion
review found value, type, exception, collection-presence and absence oracles;
no new test is assertion-free or trivial-only. Static pseudo-mutation review
indicates that wrong offset normalization, numeric coercion, dropped explicit
defaults and defaulted absent nullable fields would fail. Independent
read-only Red Team final re-review is PASS with no remaining concrete P1/P2
finding. No numeric mutation score or current
product-wide coverage value is claimed. T74 remains the latest complete
Line/Branch/CRAP measurement; the next complete Core and 33-profile
run follows the agreed multi-packet checkpoint unless a broad behavior
change requires an earlier run.

The complete Azure Table provider unit project passed 92/92 on exact
product/test commit `c289cf2c8` with zero failures and skips. The later
documentation-only commit does not alter that product/test tree. The
product-wide 33-profile aggregate remains scheduled for the grouped
checkpoint.
