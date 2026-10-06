Der eingefrorene S3-Patch ist intern adversarial geprüft; es gibt keine bestätigte Regression.

- Der öffentliche Optionskonstruktor weist Namen mit dem AWS-reservierten Anfang `amzn-s3-demo-` früh und mit dem richtigen Parameternamen ab. Die kürzere Form und derselbe Text innerhalb eines gültigen Namens bleiben erlaubt. Die [AWS-Primärregel](https://docs.aws.amazon.com/AmazonS3/latest/userguide/bucketnamingrules.html) wurde aktuell geprüft.
- Alle geänderten Zeilen der fünf Manifestdateien und alle 81 Zeilen des neuen Mutationsrunners sind geprüft; Live-Dateien, Snapshots und Vergleichsbaselines stimmen mit Freeze `4bd24d58a2d13c7141d249d8b4b687e31537fd1f9753edc4918ad08e61436ab1` überein.
- Das vollständige S3-Ownerprofil besteht 26/26 Fälle ohne Skip. Alle drei exakt rekonstruierten Gegenmutanten scheitern am erwarteten einzelnen Namensfall; die korrigierte Rückkehr besteht. 6.209 getrackte Dateien der isolierten Kopie sind wieder bytegleich zur ursprünglichen Baseline.
- Der frische öffentliche S3-Paketverbraucher besteht. Die drei verwendeten Produktassemblies sind bytegleich zu den geprüften NuGet-Paketen; Consumerquellen und Lockfile stimmen mit dem Manifest überein.
- Der getrennte EventHub-Verbraucher prüft jetzt die geparste URI-Identität einschließlich Standardport, Probe-Scope, Name und Geheimnisfreiheit. Das behebt den nachgewiesenen Substring-Orakelfehler bei explizitem `:443`; Paket-1-Produktquellen bleiben unverändert.
- Native Runlogs und Counts wurden eigenständig geprüft. Der Runner verwendet für Counts einfache Substrings; der Bericht verifiziert zusätzlich exakt geparste Zahlen. Keine unbewiesenen Survivor- oder allgemeinen Mutationsscores.

Kein realer AWS-/Azure-Cloudnachweis, keine vollständige API- oder Releasefreigabe. Dieses Urteil gilt ausschließlich für den gebundenen Deltaumfang. Einzeldatei- und Mutantendeltas sowie konkrete Quellen-, Log- und Paketbindungen stehen in REVIEW.json und *.patch.
