# NewId-GUID-Batch: Sequenz- und Tickwort-Grenze

## Produktvertrag und Teständerung

Commit `fb2f92b3a1767c6d23a157dd37f93f324fe65694` ergänzt zwei
`NewIdGeneratorTests`-Theories mit jeweils zwei Fällen. Ein Batch von
65.537 GUIDs verwendet zunächst die Sequenzen 0 bis 65.534; vor der
65.536. ID erhöht der Generator den Tick und beginnt wieder bei Sequenz 0.
Der zweite Fall beginnt bei einem Tick mit niedrigem 32-Bit-Wort
`0xffffffff`, sodass der nächste Tick auch das hohe Wort ändert.
Die Tests prüfen exakte Zeitstempel vor und nach der Grenze,
Eindeutigkeit, GUID-/SQL-Server-Reihenfolge, Übereinstimmung mit
Skalaraufrufen, Arraygrenzen und das zurückgegebene Segment. Die
Requirement-Projektion enthält beide neuen Varianten. Produktcode wurde
nicht verändert.

## Isolierte Ausführung und Messung

- `src`-Tree: `6d7eefa0ca97f4521fee383690ae5ec2816de5bb`.
- `tests`-Tree: `71163720aa77639bd0d1be63fd402a1922b55440`.
- Normales Receipt:
  `artifacts/coverage-receipt-abstractions-fb2f92b3a/receipt.json`.
- Deklarierter No-AVX2-Receipt:
  `artifacts/coverage-receipt-abstractions-noavx2-fb2f92b3a/receipt.json`.
- Beide Läufe: **828/828** Tests, null Fehler/Skips, null
  Buildwarnungen/-fehler, frischer locked Restore und Release-Build,
  unveränderte gehashte DLL-/PDB-Bytes und Cobertura-Bericht.

Bei unverändertem `src`-Tree stieg der normale Abstractions-Bericht
gegenüber dem 824er Receipt auf `0fcee3a1c` von **5.612/8.225** auf
**5.633/8.225** Zeilen und von **2.080/3.004** auf **2.089/3.004**
Branches. `NextGuid(Guid[], int, int)` stieg von 28/44 auf 42/44
Zeilen, `NextSequentialGuid(Guid[], int, int)` von 26/38 auf 32/38.
Die beiden jeweiligen Rollover-Branchpositionen stehen nun bei 2/2
statt 1/2. Der No-AVX2-Bericht zeigt dort ebenfalls 2/2. Diese Werte
gelten für die Abstractions-Assembly, nicht für das Gesamtprodukt.

## Adversariale Gegenprobe und Grenze

Das Read-only-Red-Team fand zunächst eine fehlende 32-Bit-Tickwort-Grenze;
die zweite Theory-Variante schloss sie und erhielt PASS. Eine danach
isoliert gebaute Mutante unterdrückte die Aktualisierung des hohen
Tickworts in `NextGuid(Guid[], int, int)`. Die neue Grenzvariante
scheiterte am exakt erwarteten Zeitstempel; die Klassenprobe endete
mit **16 bestanden, 1 fehlgeschlagen**, und die unveränderte Variante
bestand. Der Ein-Zeilen-Diff ist unter
`artifacts/newid-guid-batch-rollover-20260925-mutant/mutant.patch`
(SHA-256 `75bd9ec81d5b1af3018f53b62d828bcb48726a21ab3bc51fcdaffe47124ea6c2`),
der rote MTP-Lauf unter demselben Pfad als `tests.log`
(SHA-256 `046d88b612ebf3286deb707748bdd93b9c6cfe4312f48e01b372c60f906fdb90`)
archiviert. Für den Mutanten gibt es keinen vollständigen Build-Receipt;
dies ist eine Entwicklungsgegenprobe. Die Mutation wurde aus dem
Produkt-Quellbaum entfernt; dessen Git-Diff ist leer. Der saubere
Abstractions-Receipt bestand danach 828/828.

`DOTNET_EnableAVX2=0` ist im zweiten Receipt als Runner-Eingabe deklariert.
Ein im Prozess gemessener ISA-Status oder Hardwaretest ohne AVX2 liegt
weiterhin nicht vor. Die GUID-Batch-Fallbackzweige ohne SSSE3 bleiben in
beiden Berichten ungedeckt; das Abschalten von AVX2 schaltet SSSE3 hier
nicht ab. Die aktuelle Teilaggregation erkennt zwei Receipts
auf demselben Commit, aber erst 1/32 Produktassemblies; sie ist kein
produktweites A+-Profil. Der Core-Build hängt in `Grpc.Tools` `ProtoCompile`;
Colima/Docker blockiert die lokalen Providerläufe.

Für die Testarbeit wurden die Microsoft-Skills `code-testing-agent`,
`run-tests`, `test-gap-analysis` und `assertion-quality` mit der .NET-
Analyseerweiterung verwendet. Die Assertion-Prüfung fand in beiden
Theories Gleichheit, Identität, Eindeutigkeit, Reihenfolge und
Grenzwert-Orakel; keine assertionfreie oder nur triviale Variante.
