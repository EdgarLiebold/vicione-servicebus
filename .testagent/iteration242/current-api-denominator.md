# Aktueller öffentlicher/geschützter API-Nenner · Iteration 242

**Erneute aktuelle Bestätigung:** Die frische paketweise Reflexion vom 19.09.2026 auf `src`-Hash `b70d54d627e30a97e0d31298c0bed51b6884bde48981a83c09e423ffb8deb6aa` ist bytegleich mit der unten beschriebenen früheren Reflexion (identischer SHA-256 `2536d15d…`). Damit gelten die **3.233 Typen / 15.255 Member / 18.126 Parameterslots** auch für diesen aktuellen Produktquellbestand. Der vollständige Beleg und die weiterhin rote alte Contract-Baseline stehen in [current-b70-api-confirmation.md](current-b70-api-confirmation.md). Die nachstehenden `e99…`-Angaben beschreiben den ursprünglichen Messlauf, nicht den neuesten Hash.

**Ergebnis:** Für den gemessenen Working-Tree-Stand vom 19.09.2026 wurden **31 aktuelle Pakete** gebaut und daraus **30 `net10.0`-Laufzeit-Assemblies** paketweise restauriert und mit dem vorhandenen API-Reflektor ausgewertet. Sie enthalten **3.233 extern sichtbare Typen, 15.255 deklarierte öffentliche/geschützte Member und 18.126 Parameterslots**. Das ist ein maschinell erhobener **aktueller Nenner**, keine manuelle Memberdisposition, kein Test-Mapping und keine A+-Abnahme.

## Reproduzierbarer Bezug

- Repository: `repositories/vicione-servicebus`, Branch `feature/servicebus-a-plus-api`. Der Inhaltshash aller getrackten und ungetrackten, nicht ignorierten Dateien unter `src tests` war vor dem isolierten Packlauf, nach ihm und nach der Reflexion jeweils `e99ed5d929466ac2ce95221b91b9b42e4b92c33a7535b64f919345dc9f00484a`. Befehl: `git ls-files -c -o --exclude-standard -z -- src tests | sort -z | xargs -0 shasum -a 256 | shasum -a 256`. `HEAD` bewegte sich währenddessen durch nicht inhaltsändernde Root-Commits; am Ende war er `e0abbdbdd0f7da056a214d7ebdf084c96722f72f`. Der Inhaltshash, nicht dieser bewegliche Commit, bindet die gemessenen Produktquellen.
- Werkzeug: .NET SDK `10.0.302`; `tools/public-api-baseline/PublicApiBaseline.cs`, SHA-256 `f62b2315d132da62ee743052d8d23c4c99f3fe787a22e12280117032de7e26ed`. Der offizielle Aufrufpfad steht in `tools/ci/verify_developer_journeys.sh`, SHA-256 `f7bf8eeeecc9fdc62b5cc62b53da96bc0ac5ad58b833c25c335c10ae50bdaf00`. Dessen `PublicApiBaseline`-Programm wurde hier **wirklich ausgeführt**; die Gesamtsuite des Skripts wurde nicht ausgeführt.
- Isolation: `dotnet restore ViciOne.ServiceBus.slnx --locked-mode` und `dotnet pack ViciOne.ServiceBus.slnx --configuration Release --no-restore` mit eigenem `--artifacts-path /private/tmp/vsb-current-api.98OYVy/sdk` und eigenem Feed; danach ebenso die vier separat geführten Testing-Projekte. Alle fünf Restore-/Packphasen endeten mit Exitcode 0. Ergebnis: genau 31 Dateien `ViciOne.ServiceBus*.nupkg`. Die öffentliche API wurde aus deren in einen **neuen** temporären NuGet-Cache restaurierten `lib/net10.0`-Assemblies reflektiert, nicht aus alten `artifacts/packages` oder der getrackten Textbaseline.
- Der bestehende Lockfile des Package-Consumers enthält erwartungsgemäß ältere Inhalts-Hashes derselben Paketversion `1.0.0` und verweigerte den aktuellen Feed mit `NU1403`. Für diesen Inventarlauf wurde **ausschließlich ein temporärer Lockfile** unter `/private/tmp/vsb-current-api.98OYVy/` erzeugt und der Consumer damit erfolgreich restauriert. Der getrackte Lockfile und `docs/api/packed-public-api.txt` blieben unverändert. Dies ist deshalb **kein grüner vollständiger Package-Consumer-/Developer-Journey-Gate**.
- Reflektoraufruf nach dem isolierten Build: `dotnet run --project tools/public-api-baseline/ViciOne.ServiceBus.Build.PublicApiBaseline.csproj --configuration Release --no-restore --artifacts-path /private/tmp/vsb-current-api.98OYVy/sdk -- /private/tmp/vsb-current-api.98OYVy/global-packages /private/tmp/vsb-current-api.98OYVy/feed .testagent/iteration242/current-api-reflected.txt`. Die temporäre NuGet-Konfiguration band die aktuellen ViciOne-Pakete ausschließlich an den neuen Feed und Drittanbieterpakete an den lokalen Cache; keine getrackte Restore-Konfiguration wurde geändert.
- Vollständige reflektierte Typ-/Member-/Signaturliste: [current-api-reflected.txt](current-api-reflected.txt), 20.067 Zeilen, SHA-256 `2536d15de991345234fdf3f9618bdfa27f6b4a34c38f8c80d391237ffbe8a0a2`. Maschinelle Zählsicht je Typ: [current-api-denominator.tsv](current-api-denominator.tsv), 3.233 Datenzeilen, SHA-256 `6203e2f63b8aacf067a45e4dddd35a055f8f059f7676dbdec9e8d19c1f37baf1`. Die TSV wurde mit dem in `api-candidate-inventory.md` dokumentierten Ruby-Parser auf **diese neue reflektierte Datei** angewendet; Summen und `Member = CTOR + METHOD + PROPERTY + EVENT + FIELD` wurden für jede Zeile geprüft.

| Aktuell gemessen | Anzahl |
|---|---:|
| Pakete / daraus restaurierte Laufzeit-Assemblies | 31 / 30 |
| Typen: Klassen / Interfaces / Delegates / Enums / Structs | 1.822 / 1.217 / 93 / 63 / 38 |
| Typen gesamt: öffentlich / geschützt geschachtelt | 3.230 / 3 |
| Konstruktoren / Methoden / Properties / Events / Felder | 1.833 / 8.410 / 4.433 / 25 / 554 |
| Deklarierte Member gesamt | 15.255 |
| Parameterslots | 18.126 |
| Separat serialisierte Typ-Generikverträge (`GENERIC`) | 1.484 |

Der Paketunterschied ist beabsichtigt: `ViciOne.ServiceBus.Analyzers` ist ein Entwicklerpaket mit Analyzer-/Codefix-Dateien, aber ohne `lib/net10.0`-Laufzeit-Assembly. Die Reflexion zählt pro extern sichtbarem Typ nur dessen **eigene** sichtbare Konstruktoren, Methoden ohne Property-/Event-Accessor, Properties, Events und Felder; geerbte Member werden nicht je Subtyp vervielfacht. Parameterslots zählen explizite Konstruktor-, Methoden- und Indexerparameter, nicht implizite Setterwerte oder Generikparameter. `GENERIC`-Zeilen sind zusätzliche Signaturinformation, keine Member.

## Drei konkrete Gegenbelege zur alten Baseline

Die aktuelle Source-Deklaration und die **neu gepackte/reflektierte** `ViciOne.ServiceBus.Abstractions`-Assembly enthalten jeweils:

| Deklaration | Aktuelle reflektierte Zeile | Sichtbarer Vertrag |
|---|---:|---|
| `IBoundedMessageSerializer` | 1450 | öffentliches Interface; `TransportTextFormat`, `TryLocateSerializedBody`, `WriteSerializedBody<T>`, `WriteTransportEnvelope<T>` |
| `ICopiedEnvelopeBodyLocator` | 1455 | öffentliches Interface; `TryLocateSerializedBody` |
| `SerializedTransportTextFormat` | 1550 | öffentliches Enum; `None=0`, `Utf8=1`, `Base64=2` |

Alle drei Namen fehlen in `docs/api/packed-public-api.txt` (0 Treffer). Die alte Datei hat SHA-256 `59ea05a49d8d99e64715ac60b79bc68f9b657948f0742fd9c3d3e972babd054b` und bleibt **Kandidaten-/Vertragsbaseline**, nicht der aktuelle Nenner. Ihre früheren Zählwerte (3.287 Typen, 15.442 Member, 18.342 Parameterslots) sind nicht als heutiger Istwert verwendbar. Die arithmetischen Differenzen zur aktuellen Liste sagen ohne Member-für-Member-Disposition nichts über einzelne Entfernen-/Hinzufügen-Fälle aus; außerdem serialisiert der heutige Reflektor Typ-Generikverträge separat, die alte Baseline noch nicht.

## Offene Grenze

Weder die 3.233 Typen noch ihre 15.255 Member wurden hier manuell nach Anwendungsschicht, Fachabsicht, Kompatibilität oder passenden positiven/negativen Tests geprüft. Die TSV ist nur eine Zähl- und Navigationssicht; das beidseitige Member-/Parameter-zu-Test-/Requirement-Mapping aus `API_A_PLUS_CONTROL.md` ist weiterhin offen. Ebenso wurden weder Gesamtprodukt-Coverage, CRAP-Risiken, Realprovider noch unabhängige Prüfung oder Remote-Verifikation mit diesem Inventar erledigt. Jede spätere Änderung des `src`-Inhalts verlangt einen neuen Nenner unter neuem Hash.
