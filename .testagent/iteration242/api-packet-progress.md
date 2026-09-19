# Manuell geprüfte API-Pakete · Iteration 242

Dies ist eine **disjunkte Paket-Zwischensumme**, keine Gesamt-API-Abnahme und kein historischer Gesamtzähler. Die Assembly-Namen sind der Schlüssel; Namespace-Gleichheit über Assemblies hinweg darf nicht zu Doppelzählung führen. Der paketweise reflektierte Nenner in [current-api-denominator.md](current-api-denominator.md) war beim Freeze 3.233 Typen, 15.255 deklarierte Member und 18.126 explizite Parameterslots. Spätere `src`-/`tests`-Änderungen haben dessen globalen Inhaltshash invalidiert; vor einer Gesamtquote ist ein neuer Pack-/Reflexions-Freeze erforderlich.

| Disjunkte Assembly | Statisch manuell inventarisierte Typen / Member / Slots | Beleg | Noch kein A+-Beweis für |
|---|---:|---|---|
| `ViciOne.ServiceBus.SignalR` | 2 / 3 / 2 | [Memberprüfung](signalr-api-manual-review.tsv), [Requirement-Mapping](signalr-api-mapping-review.md) | positive `TimeSpan`-Obergrenze, Gesamt-Providerpfad |
| `ViciOne.ServiceBus.MessagePack` | 2 / 8 / 8 | [Memberprüfung](messagepack-api-member-review.tsv), [Review](messagepack-api-manual-review.md) | fortgeschrittene SPI-/Consumer-Journey, Content-Type-/Default-Präzedenz, Peak-Memory |
| `ViciOne.ServiceBus.Initializers` | 8 / 33 / 107 | [Memberprüfung](initializers-api-member-review.tsv), [Review](initializers-api-review.md) | negative Erstparameter-Matrix einzelner Overloads; Consumer-/Mutationsgate |
| `ViciOne.ServiceBus.StateMachineVisualizer` | 2 / 4 / 2 | [Memberprüfung](visualizer-api-member-mapping.tsv), [Review](visualizer-api-manual-review.md) | gepackter Consumer, echte DOT-/Mermaid-Parser, Mutationen |
| `ViciOne.ServiceBus.AmazonS3` | 4 / 16 / 25 | [Memberprüfung](amazon-s3-api-member-mapping.tsv), [Review](amazon-s3-api-manual-review.md) | diskriminierende Orakel für alle Slots, öffentlicher Positiv-Startup, realer AWS-Lifecycle |
| **Einmalige Paketsumme** | **18 / 64 / 144** | fünf getrennte Assembly-Scopes | keine Freigabequote |

Die Zahlen zählen gelesene und zugeordnete Signaturen, **nicht** bestandene A+-Member. Insbesondere sind die 25 S3-Slots benannt, aber nicht alle individuell verhaltensunterscheidend getestet; die TSV-Reports beschreiben die jeweilige Belegqualität. Frühere API-Arbeit außerhalb dieser fünf Pakete ist hier nicht nullgesetzt, sondern mangels konsolidiertem, aktuellen Member-/Parameterledger **nicht quantifiziert**. Diese Zwischensumme darf deshalb weder als `64/15.255` für die gesamte historische Arbeit noch als Produktfortschritt in Prozent ausgegeben werden.

Nächste Kontrollschritte: neue und geänderte Pakete jeweils mit Scope-Hash und disjunkter Assembly-ID aufnehmen; nach einem stabilen Quellfreeze den öffentlichen API-Nenner erneut paketweise reflektieren; danach jeden Member-/Slot-Schlüssel mit Test/Requirement oder begründeter Nichtverhaltens-Disposition und die Gegenrichtung abgleichen. Erst dann existiert ein belastbarer Gesamt-API-Zähler.
