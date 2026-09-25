# Transport host and entity URI decoding

Test commit: `f4df45dd5`. Product source is unchanged.

`ParseHostPath_PreservesRootAndDecodesTheVirtualHost` verifies the root
virtual host, an escaped slash within a virtual-host name, and the same
virtual host before an entity segment. `ParseHostPathAndEntityName_DecodesBothSegmentsWithoutMovingTheBoundary`
verifies root and scoped endpoint addresses with an escaped slash in the
entity name. These are shared address rules used by multiple transports.
The Abstractions requirement projection includes both variants.

Read-only adversarial review found no P1/P2 issue. A deliberate mutation
that removed host-path unescaping made the new host-path theory fail on
`team%2Fblue` versus `team/blue`. The source was restored to SHA-256
`e285d4f843552a2e20553cd48fdd971e6afda0b9039e53d564ea5c85b7c679dc`,
rebuilt, and all 12 query-string tests passed.

On the exact test commit, Microsoft Testing Platform passed the full
Abstractions suite, 922/922 with no skips, using cached SDK dependencies.
Report: `artifacts/coverage-host-path-f4df45dd5/coverage.cobertura.xml`,
SHA-256 `5159fae0075791cc30329a1896d2abf5bae80dbf9235e23511f701ff566c5169`.
The Abstractions assembly measures 70.4581% line and 72.8987% branch in
this isolated suite. `ParseHostPath` moves from 0/9 lines and CRAP 72 to
8/9 lines, 87.5% branches and CRAP 8.09. `ParseHostPathAndEntityName`
has 10/10 lines, all measured branches and CRAP 4. The remaining host-path
line is the blank absolute-path fallback, which transport host URIs do not
produce.

The global A+ goal remains open. This isolated Abstractions result is not a
complete provider coverage profile or a fresh dependency restore.
