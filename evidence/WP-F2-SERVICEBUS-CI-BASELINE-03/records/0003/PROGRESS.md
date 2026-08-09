# PROGRESS — Recovery-Import abgeschlossen, Ausgangsstand wiederhergestellt

## Ziel

Direktive `0002`: Übernahme von genau acht Produkt-/Testdateien aus dem Haupt-Stash, mit record-eigenem Beleg vor dem ersten weiteren Edit.

## Ausgeführte Arbeit

Slice `2729d13fbf1b7123bea5373d1c9177996d29c6c1c215fdc7503f09f3b6e568c4` gegen Teammanifest verifiziert, Baseline unverändert `e38203cbf112eeb3354cc394735f28287cdcb748`, `team_orchestrator.py preflight` grün mit zwei Records.

Vorzustand vor jedem Schreibvorgang erfasst: Arbeitsbaum leer, `evidence/WP-F2-SERVICEBUS-CI-BASELINE-03/` und `evidence/WP-F2-SERVICEBUS-CI-BASELINE-02/` **beide nicht vorhanden**.

Import gezielt pfadweise, ohne pauschalen `stash apply` oder `pop`:

- sieben Pfade aus dem getrackten Baum `20a22ab39ed9f7a97fc680bc404cf07665941d35`
- `tests/ViciOne.ServiceBus.Analyzers.Tests/HarnessIntegrity_Specs.cs` aus dem Untracked-Baum `20a22ab3…^3`

Vorab geprüft, dass die Datei tatsächlich nur im Untracked-Baum liegt: `git ls-tree` auf den getrackten Baum liefert für diesen Pfad kein Ergebnis, auf `^3` genau einen Treffer.

## Beobachtete Evidence

- Produktdiff enthält **exakt acht Pfade** — identisch mit der Direktivenliste.
- Alle acht sind **bytegleich** zur jeweiligen Stash-Quelle; verglichen wurde je Datei der SHA-256 des Stash-Blobs gegen den des Arbeitsbaums.
- **Kein** Pfad unter `evidence/WP-F2-SERVICEBUS-CI-BASELINE-02/` wurde restauriert.
- Zusatz-Stash `18e5c59f…` und Vorgänger-Stash `f312854f…` wurden weder angewendet noch gelöscht.
- Analyzer-Suite auf dem wiederhergestellten Stand: **111 von 111 grün**, 0 rot.
- `HEAD` unverändert `e38203cb…`.

## Technische Entscheidung

`git checkout <stash-commit> -- <pfad>` statt `stash apply`, weil nur so die Pfadmenge exakt kontrollierbar ist und die drei Stashes unangetastet bleiben. Der Untracked-Baum wurde über den dritten Elternteil `^3` adressiert, da Git ungetrackte Dateien eines Stashes dort und nicht im Hauptbaum ablegt.

## Nächster Schritt

`REQ-CI-001`: Brokerfixtures für RabbitMQ und ActiveMQ auf gepinnten offiziellen Basen mit laufbezogenem Nicht-`guest`-Account. Digests und Plugin-Prüfsumme sind bereits ermittelt und werden in record-eigener Evidence neu gebunden.

## Blocker

Keiner.
