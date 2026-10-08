# ServiceBus-Zwischenstand – 8. Oktober 2026

Dieser unveröffentlichte Zwischenstand sichert die paketweise angenommenen Produktkorrekturen,
Tests, Changelog und die bis zum Sicherungszeitpunkt vorhandenen Reviewunterlagen.
Er ist keine Produktionsfreigabe. Gerätehardware und reale Cloudzugänge fehlen weiterhin.

Offen bleiben unter anderem der aktuelle vollständige Test-/Coverage-Nachweis, CI,
weitere Provider-/Ressourcenprüfungen und die acht neuen Nachrichtenabläufe.
Von diesen liefen drei erfolgreich; der Retry-Prüffall verwendet eine ungeeignete
Beobachtungsabfrage. Die vier späteren Fälle wurden nicht erreicht.
Die neue Consumer-Lock-Prüferkorrektur ist OPEN/INCOMPLETE: 34 Testmethoden sind vorbereitet,
aber nicht ausgeführt; kopierte R1-Dokumentation, Manifest und finales Review fehlen noch.
Die SQS-Visibility-Prüfung ist ein privater, nicht ausgeführter Quellkandidat.
Alle diese Kandidaten liegen im Archiv und ersetzen keinen aktiven Produktpfad.

Produktcode, Tests und Dokumentation liegen als normale Git-Dateien vor.
Die vollständige neue lokale Evidence und vorhandene unabhängige Berichte liegen im
authentifiziert verschlüsselten Archiv; zusätzlich sind die wichtigsten JSON-Einstiegsnachweise direkt getrackt.
Die Git-Ignore-Einträge verhindern, dass ungepackte Binärdateien und Raw-Arbeitsablagen
noch einmal neben der vollständigen Sicherung eingecheckt werden.

`ENCRYPTED_MANIFEST.json` bindet die öffentlich gespeicherten AES-256-GCM-Archivteile.
`MANIFEST.json` bindet die ursprünglichen geordneten Klartextteile, `ARCHIVE_INPUTS.json` alle archivierten
Pfade, Größen, SHA-256-Werte und Dateimodi. Identische Bytes mit identischem Dateimodus
werden im Tar als sichere Rückwärts-Hardlinks gespeichert. Die ursprünglichen Dateien
werden dabei nicht verändert. Der vollständige neue Restore wird separat protokolliert.

Wiederherstellung in ein bisher nicht vorhandenes Verzeichnis:

```sh
python3 encrypted_archive.py restore --key /sicherer/pfad/checkpoint.aes256.key --destination /private/tmp/servicebus-checkpoint-restored
```

Die Wiederherstellung benötigt PowerShell 7.4+ (`pwsh` oder `--pwsh /pfad/pwsh`).
Der getrennt gehaltene 32-Byte-Schlüssel gehört nicht in Git und muss vom Owner separat
gesichert werden. Eine externe Sicherung dieses Schlüssels wird hier nicht behauptet.
Ohne diesen Schlüssel lassen sich die neuen Roharchive nicht entschlüsseln; der normale
Produktcode, Tests und Changelog sind unmittelbar aus Git wiederherstellbar.
Der Restore authentifiziert und entschlüsselt jeden Teil und prüft das zusammengesetzte
Archiv und sämtliche restaurierten
Dateigrößen, Inhaltsprüfsummen und Dateimodi. Er extrahiert keine beliebigen Symlinks.

6.819 byteidentische Dateien der alten Auditablage befinden sich bereits im Archiv
unter `evidence/WP-SB-API-REVIEW-REPAIR-20261002/remote-backup-20261006/` im Commit
`6b451cff07f46c6bf3eee325ff81ead1089e8c89`. `PRIOR_ARCHIVE_REFERENCES.json` bindet diese
Dateien an ihre ursprünglichen Archivmitglieder. Die vier neuen Bestätigungsdateien
werden mit dem neuen Archiv gesichert. Für die vollständige ältere Auditablage muss
zusätzlich das bestehende Archiv gemäß dessen README restauriert werden.
Diese Wiederverwendung behauptet die Gleichheit der Bytes, nicht alter Metadaten.
Das Repository ist zum aktuellen Sicherungszeitpunkt öffentlich. Das alte Klartextarchiv
wird durch die neue Verschlüsselung weder geändert noch rückwirkend geschützt.
Neue Klartextteile bleiben lokal und werden nicht eingecheckt.

Temporäre private SDK-Checkouts außerhalb der dauerhaft abgelegten Evidence gehören
nicht zu diesem Archiv. Die Sicherung selbst führt keine SDK-, Test-, Cloud- oder
Geräteprüfung aus. Nicht grüne Versuche bleiben unverändert als solche nachvollziehbar.
