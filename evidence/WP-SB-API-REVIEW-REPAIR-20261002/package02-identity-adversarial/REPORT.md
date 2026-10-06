Der interne adversariale Review besteht für den exakten Identitäts-Freeze zu F-SB-API-009 und F-SB-API-023. Keine neue Produktregression oder blockierende Testlücke wurde im geprüften Delta festgestellt. Das Urteil gilt nur für dieses Paket; es erteilt keine Gesamt-API- oder Releaseabnahme.

Freeze SHA256: `4ed7455cc1b1a7416fb906448fbdf01b0a5f96e3bba5b2b4b289992a12334d9c`. Alle sechs Live-/Snapshotdateien und vorhandenen Vergleichsbaselines sind hashgleich. Jede geänderte Zeile sowie die vollständigen Runner, der Executor und beide Verbraucher wurden gelesen. Der Reviewer hat keine nativen Test-/Buildprozesse gestartet und keine Produktdateien geändert.

Die Schlüsselvalidierung zählt UTF8 mit einer strikt fehlschlagenden Kodierung. Ungültige UTF16-Sequenzen werden als ArgumentException am Parameter keyId mit EncoderFallbackException als Ursache abgewiesen. 65535 Bytes bleiben zulässig; mehrbyte Unicode wird nach Bytes begrenzt. Die ursprünglichen Identitäten, AES-Größen 16/24/32 und defensiven Kopien bleiben erhalten. Der versiegelte, unveränderliche Schlüssel verhindert, dass nach erfolgreicher Konstruktion ein ungültiger Identifier in die bestehende Envelopeschreibgrenze gelangt.

Die Vertragsvalidierung kontrolliert zuerst die Grenze von 256 UTF16-Codeeinheiten und prüft dann gepaarte Surrogate sicher. Ein gültiges Paar verbraucht beide Einheiten; ungepaarte hohe/niedrige Surrogate werden verworfen. Die bestehenden Ausschlüsse für Whitespace, Steuerzeichen und Semikolon sowie Version 1..65535 bleiben bestehen. Parse, TryParse, Attribute und öffentlicher Catalog nutzen dieselbe Konstruktorgrenze. EF speichert die kanonische Darstellung und liest sie mit Parse zurück; die tatsächliche SQLite-Prüfung zeigt identische gültige Namen, idempotente Wiederholung und öffentliche Typauflösung.

Die öffentlichen Regressionen sind aussagekräftig: genaue Ausnahmearten, Parameternamen und Kodierungsursache; acht zulässige Unicode-/Grenzfälle und sechs ungültige Sequenzen pro Grenzprofil; rohe Envelopebytes, ordinaler Schlüsselproviderlookup, echter AES-Roundtrip und Kopierverhalten. Die positiven U+FFFD-, Surrogatpaar- und Grenzwertkontrollen verhindern zu strenge Ablehnung. TryParse kontrolliert auch den Default-Ausgabewert im Fehlerfall. Kein geprüfter Verhaltensmodus ist assertionsfrei oder nur trivial. Die bestehenden key-id- und pre-auth-Modi sind zusätzliche Kontrollen; die genauere Grenzprüfung liefert den kausalen Nachweis.

| Nachweis | Tatsächlich geprüft |
|---|---|
| Frischer NuGetverbraucher | fünf Modi exit 0; sechs Paket-/Runtimebindungen über beide Verbraucher bytegleich mit lib/net10.0 |
| Echte SQLite-Verwendung | ASCII, Unicode, Surrogatpaar gültig; lone-high/lone-low vor Persistierung verworfen, null Zeilen |
| Unveränderter Abstractions-Unitowner | 979/979, Fehler 0, Skip 0; locked restore und strict build ohne Warnungen |
| Unveränderter Core-Unitowner | 7446/7446, Fehler 0, Skip 0; locked restore und strict build ohne Warnungen |
| Ausgewählte native Mutanten | neun kompilierbare Ein-Ursachen, alle kausal exit 134 |
| Baseline-/Rollbackkontrollen | Originalpaket und isolierter Originalsource beide Grenzmodi rot; corrected und rollback beide grün; 6209 isolierte Baseline-Dateien bytegleich |

| Mutant | Diskriminierende Ursache |
|---|---|
| key-lossy-encoding | fehlende Ausnahme bei ungültigem UTF16 |
| key-exclusive-upper-bound | zulässige 65535 Bytes abgewiesen |
| key-character-count-bound | mehrbyte Identifier über 65535 Bytes akzeptiert |
| key-lost-encoding-cause | konkrete Kodierungsursache verloren |
| contract-lost-high-surrogate-guard | ungültiges mittleres hohes Surrogat akzeptiert |
| contract-valid-pair-not-consumed | zulässiges Paar als einzelnes niedriges Surrogat abgewiesen |
| contract-lost-low-surrogate-guard | ungepaartes niedriges Surrogat akzeptiert |
| contract-exclusive-length-bound | zulässige 256 Codeeinheiten abgewiesen |
| contract-replacement-character-overrejection | echter zulässiger U+FFFD verworfen |

Alle neun Mutantentexte wurden aus den gefrorenen Produktdateien durch exakt eine Ersetzung rekonstruiert und gegen die aufgezeichneten Sourcehashes geprüft. Sämtliche 35 Mutationslogs stimmen in ihren Hashes; die Failstacks zeigen jeweils die öffentliche Vertragsprüfung. Drei bereits ausgeführte Mutanten wurden nur mit identischem rekonstruierbarem Sourcehash übernommen, sechs im zweiten Lauf neu ausgeführt. Der erste nicht kompilierbare Lost-cause-Versuch zählt ausdrücklich nicht. Der vollständige Runner verändert keine kanonischen Tests; Oracle-Source und Oracle-DLL sind weiterhin identisch. Historische Mutanten-DLLs sind über Runnerreceipts belegt und nicht als einzeln aufbewahrte Binärdateien nachgehasht.

Die vollständigen Unitowner liefern zusätzliche Kompatibilitätsevidenz, keine neue kanonische Testqualität-Abnahme. Alle 766 Core- und 117 Abstractions-Dateien unter diesen Testownerverzeichnissen sind unverändert gegenüber der Baseline. Der spätere Coreabschluss wurde unabhängig mit SHA256 `d98c6cab49dd65373f149880b2f27bf41f4f224b8cce8435cadda29b50f49354` bestätigt; das alte RUNNING-Feld des eingefrorenen Manifests wurde nicht nachträglich verändert.

Die Beweisgrenzen bleiben konkret: Bereits verlustbehaftet gespeicherte Identifier werden durch diese Konstruktorreparatur nicht rekonstruiert. Historische Envelopes und SQLite-Zeilen brauchen bei Bedarf eine explizite Wiederherstellung oder Aliasentscheidung ihres Owners. Die SQLite-Kontrolle beweist Admission/Readback/Claim in frischen echten Datenbanken, keinen Prozessneustart, Migration oder Backgrounddelivery. Der Identifier ist vor dem Authentifizierungslookup weiterhin untrusted; dieser unveränderte Pfad wurde charakterisiert und nicht pauschal freigegeben. Die neun ausgewählten Mutanten sind keine projektweite Mutationsquote.

Die fest angehefteten Microsoft-Testskills test-gap-analysis, assertion-quality und die .NET-Analyseerweiterung wurden angewandt. Es werden keine nur statisch vermuteten Überlebenden als bestätigte Lücken gemeldet. REVIEW.json bindet Einzeldateiurteile, Aufrufpfade, Grenzen und Runtimebytes; MUTATION_REVIEW.json bindet Ursachen; VERIFIED_RECEIPTS.json hält die unabhängig geprüften nativen Receipts. MANIFEST.json bindet alle Berichtartefakte.
