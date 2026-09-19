# EF Reliable Store: statischer API-Delta-Kandidatenabgleich · 19.09.2026

**Ergebnis:** Der Inhaltsunterschied dieser einen Produktdatei erzeugt **keinen erkennbaren PUBLIC/PROTECTED-Deklarations- oder Signaturkandidaten**. Das ist eine statische Quellenaussage, kein Beweis über die kompilierte API, NuGet-Pakete oder Consumer-Kompatibilität.

## Vergleichsgegenstand

- Live: `src/Persistence/ViciOne.ServiceBus.EntityFrameworkCore/ReliableMessaging/EntityFrameworkReliableStore.cs`, 1.063 Zeilen, SHA-256 `cdb343bd4fb52d5e75e55a5ceaeb3e09d15abe8ee804e82996fb38bd105822c2`.
- Unveränderlicher a91-Snapshot: `/private/tmp/vicione-servicebus-green-freeze.H2NAR1/repositories/vicione-servicebus/src/Persistence/ViciOne.ServiceBus.EntityFrameworkCore/ReliableMessaging/EntityFrameworkReliableStore.cs`, 1.053 Zeilen, SHA-256 `abf07eb8b12201b8446c3b1a1c962af8e2e3808017d158e8cee6170555a8dace`. Snapshot-Commit laut zugehöriger Drift-Triage: `a2373f95efaa801b7f2c6905d7d2f06dc8e696fb`.
- Der vollständige dateibezogene `diff -u` enthält genau einen Hunk bei Snapshot-Zeile 987 / Live-Zeile 987. Der Aufruf wurde nicht auf andere Dateien oder den gesamten Paketgraph erweitert.

## Statischer Befund

`EntityFrameworkReliableStore<TBus, TDbContext>` bleibt `internal sealed` und implementiert unverändert `IOutboxStore<TBus>`, `IInboxStore<TBus>` und `IScheduleStore<TBus>` mit unveränderten Generic Constraints. Der einzige geänderte Bereich liegt im Rumpf von `static void EnsureSameIntent(DurableSendRecord existing, SerializedDurableSend message)`; ohne Access Modifier ist diese Methode privat. Die sechs bisherigen skalaren Vergleiche wurden durch zwei lokale Tupel und deren `Equals`-Vergleich ersetzt. Die nachfolgenden Body-/Metadata-Vergleiche sowie der `DurableSendIdentityConflictException`-Pfad sind im Diff unverändert.

Es wurde in dieser Datei weder eine Typ-, Konstruktor-, Methoden-, Property-, Event- oder Felddeklaration hinzugefügt, entfernt oder signaturseitig verändert, noch Namespace, Basistypen, Interfaces, Attribute oder Sichtbarkeit. Die `public` deklarierten Mitglieder des umschließenden `internal`-Typs sind zudem nicht dadurch öffentliche Assembly-API; auch ihre Deklarationen sind unverändert. Die neuen Tupel sind lokale Implementierungswerte, keine exponierten Parameter- oder Rückgabetypen.

**Grenze:** Kein .NET-/MSBuild-/Testlauf, kein Assembly-Metadatenvergleich, kein Paketbau und kein Consumer-Compile wurden für diesen Bericht ausgeführt. Daher lautet das Urteil ausdrücklich **„0 statische PUBLIC/PROTECTED-API-Deltakandidaten aus dieser Datei“**, nicht „kompilierte Public API insgesamt identisch“ und nicht „A+-Freigabe“. Die offene paket- und consumerbezogene API-Prüfung sowie die Provider-/Race-Nachweise bleiben beim Lead.
