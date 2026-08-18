# Entscheidungsvorlage — RS1038 im Analyzerpaket

## Ursache

`src/ViciOne.ServiceBus.Analyzers` enthält **beides** in **einer** Assembly: drei
`DiagnosticAnalyzer` (`AsyncMethodAnalyzer`, `CancellationTokenOverloadMethodAnalyzer`,
`MessageContractAnalyzer`) und zwei `CodeFixProvider` (`MessageContractCodeFixProvider`,
`CancellationTokenOverloadMethodFixer`). Ein `CodeFixProvider` lebt in
`Microsoft.CodeAnalysis.Workspaces`, deshalb referenziert das Projekt
`Microsoft.CodeAnalysis.CSharp.Workspaces`.

Der Sprung auf Roslyn `5.6.0` meldet dafür dreimal **RS1038**, einmal je Analyzer, im Wortlaut:
„Diese Compilererweiterung sollte nicht in einer Assembly implementiert werden, die einen Verweis
auf `Microsoft.CodeAnalysis.Workspaces` enthält. Die Assembly wird in
Befehlszeilen-Kompilierungsszenarien nicht bereitgestellt."

## Wirkung

Das Paket liefert genau diese eine Assembly unter `analyzers/dotnet/cs` aus. Der Kommandozeilen­
compiler — also `dotnet build`, wie dieses Repository und jeder Consumer baut — lädt sie und stellt
`Microsoft.CodeAnalysis.Workspaces` **nicht** bereit. Heute fällt das nicht auf, weil die
Analyzerpfade selbst keinen Workspaces-Typ anfassen; die Fixer tun es. Der Analyzerlauf ist mit
`111/111` grün. Es ist also kein aktueller Fehlschlag, sondern eine Ladeabhängigkeit, die bei jeder
Änderung an den Analyzern in einen `TypeLoadException`-Fehlschlag beim Consumer kippen kann — und
zwar bei ihm, nicht bei uns.

## Alternativen

**A — Assemblies trennen (Standardlösung von Roslyn).**
`ViciOne.ServiceBus.Analyzers` behält nur `Microsoft.CodeAnalysis.CSharp`; ein neues
`ViciOne.ServiceBus.Analyzers.CodeFixes` nimmt die zwei Fixer und die Workspaces-Referenz. Beide
Dateien liegen im selben NuGet unter `analyzers/dotnet/cs`.
*Pro:* behebt die Ursache; entspricht dem von Roslyn dokumentierten Layout; der Compiler lädt nur
noch, was er auch bereitstellen kann.
*Contra:* neue Assemblyidentität. Berührt `signing.props`, den Identitätszensus, `NOTICE`,
`CHANGELIST.md` und den Paketinhalt. Das ist eine Paketstrukturänderung, keine Paketversion.

**B — RS1038 unterdrücken.**
*Pro:* eine Zeile.
*Contra:* unterdrückt genau die Warnung, die vor einem Ladefehler beim Consumer warnt, und
widerspricht der Anordnung, keine pauschale Warnunterdrückung als bequemen Abschluss zu nehmen.

**C — Fixer entfernen.**
*Pro:* die Workspaces-Referenz entfällt ersatzlos.
*Contra:* entfernt eine erhaltene Fähigkeit ohne Äquivalenz. Nicht zulässig ohne ausdrückliche
Produktentscheidung.

## Empfehlung

**A.** Es ist die einzige Alternative, die die Ursache beseitigt statt ihr Symptom, und sie erhält
beide Fähigkeiten vollständig. Der Preis ist eine zusätzliche Assemblyidentität, und genau deshalb
lege ich sie vor, statt sie selbst zu ziehen: sie ändert den Paketinhalt und die Signaturfläche,
und beides gehört nicht in einen Paketversionsschritt.

**Nicht behauptet:** solange A nicht entschieden ist, bleiben die drei RS1038 offen stehen. Sie sind
weder unterdrückt noch weggefiltert, und der Bau meldet sie bei jedem Lauf.
