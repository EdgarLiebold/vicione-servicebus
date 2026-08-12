# MessagePack — gemessene Fakten für den Pflichtfolgeauftrag REQ-CI-006

Stand 2026-08-12. Nichts am Produkt geändert; alle Angaben sind gemessen oder aus der
GitHub-Advisory-Datenbank abgefragt, nicht geschätzt.

## Fundstellen

| Pfad | Version | ausgeliefert? |
| --- | --- | --- |
| `Directory.Packages.props:33` → `src/ViciOne.ServiceBus.MessagePack` | **3.1.6** | **ja** |
| `Microsoft.AspNetCore.SignalR.Protocols.MessagePack` 9.0.0 → MessagePack | **2.5.187** | nein, nur `tests/ViciOne.ServiceBus.SignalR.Tests` |

Quelle: `interim/record-0060-gates/VULNERABILITY_INVENTORY.json`, Feld `dependencyPath`.
`shippedProjectsWithHighFinding` nennt genau ein Projekt: `src/ViciOne.ServiceBus.MessagePack`.

## Die Befunde

**Zwölf verschiedene Advisories**, nicht einer, wie es bisher in den Records steht:
drei High (GHSA-382j-8mxh-c7x2, GHSA-hv8m-jj95-wg3x, GHSA-vh6j-jc39-fggf) und neun Moderate.
Sie machen 41 der 44 High/Critical-Zeilen des Inventars aus.

Abgefragte betroffene Bereiche und erste behobene Version (GitHub Advisory API):

- `>= 3.0, < 3.1.7` → **behoben in 3.1.7**
- `< 2.5.301` → **behoben in 2.5.301**

Beide MessagePack-Vorkommen liegen also unterhalb ihrer jeweiligen Patchversion, und zwar knapp:
3.1.6 gegen 3.1.7, und 2.5.187 gegen 2.5.301.

## Verfügbare Versionen

- MessagePack: neueste stabile **3.1.8** (3.1.7 wäre das Minimum).
- `SignalR.Protocols.MessagePack`: die **gesamte 9.0-Linie bis 9.0.11 zieht 2.5.187**; erst
  **10.0.11 zieht 2.5.302**. Ein Sprung innerhalb der 9er-Linie hilft nicht.
- `SignalR.Protocols.MessagePack` 10.0.11 unterstützt `net10.0`, `netstandard2.0`, `net462`.
  Das Testprojekt steht auf **net9.0**, würde also die netstandard2.0-Fassung ziehen.

## Was daraus folgt

Der ausgelieferte Anteil ist vollständig durch **eine Zeile** behoben: den zentralen Pin auf 3.1.8.
Der verbleibende Pfad ist reine Testinfrastruktur und erreicht kein Paket, das das Haus verlässt —
er hält aber die Inventarzahl oben und ist damit sichtbar offen.

## Kollisionsprüfung

Ob 3.1.8 und 2.5.302 in einem Projekt zusammentreffen würden, ist **geprüft und verneint**.
`tests/ViciOne.ServiceBus.SignalR.Tests` referenziert neben SignalR nur `ViciOne.ServiceBus.SignalR`,
`ViciOne.ServiceBus.TestFramework` und `ViciOne.ServiceBus`; keines davon referenziert MessagePack, und
`src/ViciOne.ServiceBus.SignalR` deklariert MessagePack überhaupt nicht. Ein gezielter
`VersionOverride` im Testprojekt ist damit kollisionsfrei möglich.
