# SignalR-API-Anforderungszuordnung · Iteration 242

## Ergebnis und Grenze

Für die zwei exportierten SignalR-Typen und ihre drei deklarierten sichtbaren Member schließt `REQ-VSB-SIGNALR-COMPOSITION` die fehlende **testprojektlokale** Zuordnung: neun bestehende öffentliche API-/Kompositionstests tragen jetzt `RequirementCoverage`, und zehn passende Einträge stehen in `Requirements/SignalRRequirements.json`. Der zehnte Fall prüft neu, dass eine spätere Änderung des an den Callback übergebenen `SignalRBackplaneOptions`-Objekts die registrierten Hub-Einstellungen nicht verändert. Die genaue Member→Varianten→Test-Zuordnung steht in `signalr-api-mapping-members.tsv`.

Dies ist keine neue Featurekatalog-Garantie und keine API-A+-Freigabe. `RequirementCoverageProjectionVerifier` prüft die Gleichheit von JSON und kompilierten Testmetadaten in beide Richtungen, besitzt aber selbst weder eine Public-Member-Dimension noch einen Ersatz für die inhaltliche Prüfung. Die Memberbrücke liegt daher in der vorgenannten kleinen TSV.

## Norm- und Produktherleitung

- `PO-2026-08-13-03` Nr. 3 erhält SignalR als optionalen ServiceBus-Adapter; Erhalt bedeutet keine automatische Suite-Komposition.
- `PO-2026-09-04-01` Nr. 6 verlangt für statische Optionsinvarianten positive und kausale negative Startprüfungen, Nr. 7–8 fordert eine klare öffentliche Provider-API und ihren belegten Verbraucher.
- `PO-2026-09-08-01` Nr. 7 verlangt verhaltensunterscheidende Tests je öffentlichem API-Member und Parameter. A13 §5.1/§5.2 trennt Testzuordnung von tatsächlicher Ausführung und Freigabe.
- Der aktuelle Code in `SignalRBackplaneExtensions` erzeugt `SignalRBackplaneOptions`, ruft den optionalen Callback vor der Prüfung auf, validiert einen strikt positiven Timeout, erfasst ihn anschließend als `RequestTimeout` in `SignalRBackplaneSettings<THub>`, registriert den Hubmanager und weist eine Doppelregistrierung vor Serviceeinträgen zurück. Der Optionen-Initialwert ist 20 Sekunden. `docs/static-configuration-validation.json` verweist bereits auf die bestehenden positiven/negativen Konfigurationstests, enthält aber keine Requirement-Projektion.

Der neue Snapshot-Test ist gerechtfertigt, weil der Callback eine veränderliche, öffentlich setzbare Optionsinstanz an Anwendungscode übergibt. Eine spätere Mutation darf die bereits registrierte Einstellung nicht still ändern; die Assertion vergleicht nach einer tatsächlich abweichenden Mutation den gespeicherten `RequestTimeout` mit dem ursprünglichen Wert. Die anderen acht Tests in `SignalRBackplaneConfigurationTests` zu internen Laufzeit-/Endpoint-/Scope-Kollaboratoren wurden nicht als Beweis für diese drei exportierten Member umetikettiert.

## Lesegate und statische Prüfung

- Prüf-Freeze vor diesem Diff: ServiceBus-Commit `2eb9ffe63823a8a9bdd17fa06539bcad7f405fdb`, Tree `b6f04ed1b5d152d54e4d9db0c7383965a4fc3bf8`.
- `git ls-files` liefert 25/25 getrackte Dateien des besitzenden SignalR-Testprojekts; alle 25 samt Projektdatei, Lockdatei, Fixtures, Testdaten und Projektionsdateien wurden direkt gelesen. `signalr-api-mapping-read-manifest.tsv` hält die sortierte Pfad-/SHA-256-Menge der Working Copy nach dem Mapping-Edit fest.
- Wirksame Eingaben gelesen: Root- und Test-`Directory.Build.props`/`.targets`, `Directory.Packages.props`, `global.json`, `signing.props`, `.editorconfig`, `tests/testconfig.json`, `tests/testsettings.json`, `ViciOne.ServiceBus.Tests.Unit.slnx`, `.github/workflows/native-tests.yml` sowie `RequirementCoverageAttribute` und `RequirementCoverageProjectionVerifier`.
- Statisch: `jq` meldete 74 eindeutige Requirement-Varianten, davon zehn neue Kompositionszeilen; `git diff --check` war ohne Befund.
- Nach Freigabe des .NET-Fensters: SDK `10.0.302`; `dotnet test --project tests/Transports/ViciOne.ServiceBus.SignalR.Tests/ViciOne.ServiceBus.SignalR.Tests.csproj -c Release --no-build --no-restore --minimum-expected-tests 1` bestand mit **97/97**, 0 Fehlern und 0 übersprungenen Tests. Der Lauf schließt die kompilierten Requirement-Metadaten und ihre bidirektionale Projektion ein.

## Offen, bewusst nicht als Vertrag erfunden

Die obere Grenze positiver `TimeSpan`-Werte ist ungeklärt. `SignalRBackplaneOptions.Validate()` akzeptiert derzeit jeden positiven Wert, während der Request-Pfad einen `TimeProvider`-Timer daraus anlegt. Ein hypothetischer Maximalwert und sein Fehlersignal dürfen nicht aus diesem Mapping-Task als Produktnorm festgeschrieben werden. Die vollständige Pfad-/Providerprüfung, eine Entscheidung über zulässige Obergrenze bzw. Durchsetzbarkeit und der dazugehörige Boundary-/Mutationsnachweis bleiben ein eigener Produktbefund. Das hier ergänzte Testorakel sagt nur den belegten Default, eine normale positive Einstellung, die eingefrorene Registrierung und die vorhandene Nichtpositiv-Ablehnung zu.
