# Beleg zur Scope-Frage `WRITE_SCOPE_CHANGE`

> **RESOLVED HISTORICAL EVIDENCE.** This document records the state when question record `0005`
> was raised. Lead record `0006-reslice.json` resolved both gaps through revision 2 at
> `revisions/0002/DEVELOPMENT_SLICE.json` (SHA-256
> `4d3e3ebe55a2a6cfa07e647754723ecfc3436db2f91baf3b3a6d059328ea7d75`). The effective slice has
> 92 write scopes and grants both `tests2/xunit.runner.json` and `tests2/Core/**`. Everything below
> is retained only as the evidence that justified that resolved change; it is not current guidance.
> A later Lead-owned technical correction removed `xunit.runner.json` and replaced it with the
> MTP-only `tests2/testconfig.json`. No current implementation or future AI may derive the runner
> design from the historical proposal below.

Arbeitspaket `WP-F2-SERVICEBUS-TEST-RECONSTRUCTION-12`, Team `team-1-claude`.
Erstellt als Pflichtbindung der Frage-Sequenz; es wurde keine vorhandene Evidencedatei geändert.

## 1. Unveränderte Prüfbasis

| Gegenstand | Wert |
|---|---|
| Produktcommit | `a6b205c9fc2a29d81968069936148f1ee0a6e1d1` |
| Produkttree | `b567593278b1dfa6620c792cc9df7b7e5ea72c88` |
| Branch | `test/servicebus-xunit4-mtp2-a-plus-v2` |
| Development Slice | `vicione-architecture/work/delivery/WP-F2-SERVICEBUS-TEST-RECONSTRUCTION-12/DEVELOPMENT_SLICE.json` |
| Slice-SHA-256 | `c0c4c3dae27fff038b58d393eb7dffe3f1bb6722f3f89b0f83a78498832bd1b2` |
| Gemeinsamer Vertrag | `vicione-architecture/work/delivery/WP-F2-SERVICEBUS-TEST-RECONSTRUCTION-12/A_PLUS_NATIVE_TEST_IMPLEMENTATION_CONTRACT.md` |
| Vertrags-SHA-256 | `bac1cd5c62d0dd7bc76c8c0601914b3f867c5dbc4673b7c124a51a0a200128e1` |
| Schreibscopes im Slice | 90 |

Slice und Vertrag sind unverändert. Die Frage verlangt keine Änderung ihrer Semantik, sondern
ausschließlich die Ergänzung zweier fehlender Zielpfade in der Schreibmenge.

## 2. Erste Lücke — `tests2/xunit.runner.json`

Der gemeinsame Vertrag verlangt die Datei an zwei Stellen:

- Abschnitt 2, verbindliche native Kontrollen: `xunit.runner.json` setzt übersprungene Tests und
  Warnungen auf Fehler.
- Abschnitt 4, Zielstruktur: die Datei liegt unmittelbar unter `tests2/`.

Die Schreibmenge des Manifests enthält `tests2/Directory.Build.props`,
`tests2/Directory.Build.targets`, `tests2/testconfig.json`, `tests2/testsettings.json` sowie die
bereichsweisen Globs, aber **nicht** `tests2/xunit.runner.json`.

Wirkung: `REQ-TEST-203` verlangt, dass übersprungene Tests und Warnungen fehlschlagen. Ohne diese
Datei ist die Zusage innerhalb der freigegebenen Schreibmenge nicht erfüllbar, außer durch
projektweise Kopien — die der Lead in Direktive `DIR-A0071-SCOPE-QUESTION-02` Nummer 4 ausdrücklich
verbietet.

Vorgesehener Übernahmeweg nach derselben Direktive: Die eine zentrale Datei wird durch die
verschachtelte MSBuild-Konfiguration in `tests2/Directory.Build.props` als Inhaltselement in jedes
ausführbare Testartefakt kopiert. Es entsteht genau eine Konfigurationswahrheit ohne Projektkopie.

## 3. Zweite Lücke — `tests2/Core/**`

Abschnitt 4 des Vertrags zeichnet unter `tests2/` die quelleigentümerorientierten Ordner `Testing/`,
`Architecture/`, `Core/`, `Persistence/`, `Scheduling/`, `Transports/` und `Tools/`.

Die Schreibmenge enthält `tests2/Architecture/**`, `tests2/Testing/**`, `tests2/Persistence/**`,
`tests2/Scheduling/**`, `tests2/Transports/**` und `tests2/Tools/**` sowie flache Projektpfade wie
`tests2/ViciOne.ServiceBus.Tests/**`, jedoch **kein** `tests2/Core/**`.

Wirkung: `REQ-TEST-205` verlangt einen quelleigentümerorientierten Testbaum. Der Kernbereich hat
damit keinen strukturell richtigen Zielordner, obwohl der Vertrag ihn zeichnet.

F1a ist davon nicht blockiert: Der kleine native Smoke-Satz liegt nach Direktive Nummer 5 im
Architekturprojekt, solange jede Prüfung ausschließlich eine reale kompilierte Assembly oder einen
tatsächlich ausgewerteten Buildgraphen untersucht. Spätere Produktverhaltenstests des Kernbereichs
gehören nach `tests2/Core/**`.

## 4. Betroffene Requirements

| Requirement | Betroffen durch | Wirkung |
|---|---|---|
| `REQ-TEST-203` | fehlende `tests2/xunit.runner.json` | Skips und Warnungen können nicht fail-closed gesetzt werden |
| `REQ-TEST-205` | fehlender `tests2/Core/**` | Testbaum ist nicht vollständig quelleigentümerorientiert |
| `REQ-TEST-207` | beide | Der F1a-Fundamentcheckpoint ist nicht einfrierbar, solange eine Pflichtkontrolle keinen zulässigen Ablageort besitzt |

## 5. Abgrenzung

Diese Frage ändert weder Produktverhalten noch Vertragssemantik, Proof-Stärke, Baseline,
Zielrepository, Modellbindung oder gemeinsame Contracthashes. Sie betrifft ausschließlich die
Schreibmenge und ist deshalb nach `DEVELOPMENT_ORCHESTRATION` Abschnitt 3.2 nur über einen
Lead-erzeugten `RESLICE` auflösbar, niemals über eine `ANSWER`.
