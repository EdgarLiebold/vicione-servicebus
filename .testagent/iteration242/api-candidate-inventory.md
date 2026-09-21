# ServiceBus-API-Kandidateninventar, Iteration 242

**Ergebnis:** Die versionierte gepackte API-Baseline liefert einen reproduzierbaren *Kandidaten-Nenner* von 30 Laufzeit-Assemblies, 3.287 extern sichtbaren Typen, 15.442 deklarierten öffentlich/geschützt sichtbaren Membern und 18.342 Parameterslots. Das ist **keine** manuelle API-Prüfung, kein Testdeckungsnachweis und keine A+-Abnahme. Der in dieser Inventur manuell geprüfte API-*Zähler* ist **0**; über die Arbeit anderer wird damit nichts ausgesagt.

## Bezugsstand und Geltungsbereich

- Repository: `repositories/vicione-servicebus`, Branch `feature/servicebus-a-plus-api`, `HEAD=194271bbd0062d99ea856435dec9390e69b20d11` (19.09.2026, 12:32:05 +02:00). Statische Erhebung am 19.09.2026 gegen `docs/api/packed-public-api.txt`, SHA-256 `59ea05a49d8d99e64715ac60b79bc68f9b657948f0742fd9c3d3e972babd054b`; diese Datei ist in `HEAD` identisch. Ihr letzter Commit ist `fd11887df54fbf5731a50e4626ef4224b5f43c6e` vom 15.09.2026.
- Die Baseline wurde laut ihrem Kopf aus frisch gepackten, paketweise restaurierten `net10.0`-Assemblies per Reflexion erzeugt. Die Erzeugerlogik steht in `tools/public-api-baseline/PublicApiBaseline.cs`; `tools/ci/verify_developer_journeys.sh` baut und startet sie und vergleicht die Ausgabe bytegleich mit dem versionierten Vertrag. **Dieses Inventar startete weder .NET noch Build, Pack, Restore oder Reflexion erneut.**
- 31 Delivery-Pakete, davon 30 `lib/net10.0`-Laufzeit-Assemblies in der Baseline. Die drei weiteren `src`-Projekte `ViciOne.ServiceBus.Analyzers`, `.Analyzers.CodeFixes` und `.Analyzers.Package` sind hier nicht als Laufzeit-Assemblies erfasst: Die ersten beiden erzeugen Roslyn-Host-Assemblies, das dritte bündelt sie als Paket ohne `lib/`-Assembly. Es gibt insgesamt 33 getrackte `src/**/*.csproj`.
- Pro Typ zählt die TSV-Datei deklarierten Memberbestand, nicht geerbte Member pro abgeleitetem Typ. `METHOD` schließt Property-/Event-Accessor aus; Properties und Events werden je einmal gezählt. `CTOR`, `METHOD`, `PROPERTY`, `EVENT` und `FIELD` bilden die 15.442 Member. Parameterslots stammen aus Konstruktoren, Methoden und den acht Indexer-Properties; implizite Setter-`value`-Parameter und Typ-/Methodengenerik sind keine Parameterslots. Drei geschützte geschachtelte Typen sind enthalten. Die Baseline enthält keine separaten `GENERIC`-Zeilen; die TSV-Spalte `generic_constraints=0` bedeutet deshalb nur **nicht separat serialisiert**, keinesfalls „keine generischen Verträge“ (1.318 `TYPE`-Zeilen enthalten `<`).

| Kandidaten-Nenner | Anzahl |
|---|---:|
| Laufzeit-Assemblies | 30 |
| Typen (1.879 Klassen, 1.215 Interfaces, 93 Delegates, 62 Enums, 38 Structs) | 3.287 |
| Konstruktoren | 1.890 |
| Methoden | 8.495 |
| Properties | 4.482 |
| Events | 25 |
| Felder | 550 |
| Member gesamt | 15.442 |
| Parameterslots | 18.342 |

Die begleitende `api-candidate-inventory.tsv` enthält für **jeden der 3.287 Typen** Assembly, Typart, Sichtbarkeit sowie die Member- und Parameterslotzahlen. Die vollständigen Membernamen und Parametersignaturen stehen in der versionierten Baseline; die TSV ist eine Zählsicht und keine zweite API-Norm. TSV-SHA-256: `b72b3f8c845a8dc9e9e49ab21ec777df10eae77c626ec9739dc058ef7ace2883`.

## Dirty-Tree-Grenze

Zum Erhebungszeitpunkt waren **28 getrackte `src`-Dateien** gegenüber `HEAD` verändert. Ein statischer Diff enthält mindestens textuelle öffentliche/geschützte Signaturkandidaten in `EntityFrameworkSagaRepository.cs` (Konstruktor/Property/Override), `BusFactoryConfigurator.cs` (`HasMessageLimits`) und `MessageLimitsConfigurationExtensions.cs` (`Limits`-Overload). Sichtbarkeit des enthaltenden Typs, kompilierter Zustand und exakter aktueller API-Delta wurden nicht reflektiert. Die in `src/ViciOne.ServiceBus.Sagas/Middleware/SendSagaPipe.cs` geänderten `RequireTask`-Methoden gehören zu `static class SagaRepositoryLifecycle` ohne externe Typsichtbarkeit und werden **nicht** als öffentlicher API-Kandidat gezählt; `MessageDataFormatter.cs` enthält diese Umbenennung nicht. Die Zahlen oben sind daher **der versionierte Baseline-Kandidat, nicht die bewiesene aktuelle Working-Tree-API**. Laufende Arbeit kann diesen Vorbehalt vergrößern. Auch `HEAD` liegt nach dem letzten Baseline-Commit; ohne erneuten Paketgate-Lauf ist bytegleiche Aktualität für `HEAD` nicht bewiesen.

Die getrackte Baseline ist für den aktuellen API-Nenner veraltet: `IBoundedMessageSerializer`, `ICopiedEnvelopeBodyLocator` und `SerializedTransportTextFormat` sind aktuelle Source-Deklarationen, die dort fehlen. Das ist ein konkreter Gegenbeleg gegen die Gleichsetzung dieses Inventars mit dem aktuellen API-Nenner.

Für einen belastbaren aktuellen Nenner: den Produktstand einfrieren, dann den regulären gepinnten Paketweg `tools/ci/verify_developer_journeys.sh` gegen diesen Freeze ausführen und dessen frisch erzeugtes `artifacts/verification`-Inventar mit der versionierten Baseline vergleichen. Bei beabsichtigter API-Änderung erst nach fachlicher Disposition `--update-public-api-contract` verwenden und den Diff prüfen. Dieser .NET-Lauf war für diesen parallelen Read-only-Auftrag nicht freigegeben und wurde nicht gestartet.

## Reproduktion der statischen Zählsicht

Tool: macOS-System-Ruby `ruby 2.6.10p210`. Vom ServiceBus-Repositoryroot erzeugt der folgende Befehl die TSV mechanisch aus der angegebenen Baseline; er führt keinen Build aus. Die Klammerzählung berücksichtigt verschachtelte CLR-Generik, Nullability-/Modifierblöcke, Default-Ausdrücke und zitierte Texte. Die Befehlsausgabe enthält **nur Kandidatenzählungen**.

```sh
ruby -e '
def arity(line, opener, closer)
  start = line.index(opener)
  return 0 unless start
  depths = Hash.new(0)
  quote = nil
  escaped = false
  count = 0
  nonblank = false
  line[(start + 1)..].each_char do |ch|
    if quote
      if escaped
        escaped = false
      elsif ch == "\\"
        escaped = true
      elsif ch == quote
        quote = nil
      end
      next
    end
    if ch == "\"" || ch == "\x27"
      quote = ch
      nonblank = true
      next
    end
    if ch == closer && depths.values.all?(&:zero?)
      return nonblank ? count + 1 : 0
    end
    case ch
    when "<", "[", "{", "("
      depths[ch] += 1
    when ">"
      depths["<"] -= 1
    when "]"
      depths["["] -= 1
    when "}"
      depths["{"] -= 1
    when ")"
      depths["("] -= 1
    when ","
      count += 1 if depths.values.all?(&:zero?)
    end
    nonblank = true unless ch.match?(/\s/)
  end
  raise "unclosed parameter list: #{line}"
end
puts "assembly\ttype_kind\ttype_visibility\ttype\tctors\tmethods\tproperties\tevents\tfields\tgeneric_constraints\tmembers\tparameters"
assembly = nil
row = nil
emit = -> {
  if row
    members = row.values_at(:ctors, :methods, :properties, :events, :fields).sum
    puts [row[:assembly], row[:kind], row[:visibility], row[:type], row[:ctors], row[:methods], row[:properties], row[:events], row[:fields], row[:generic], members, row[:parameters]].join("\t")
  end
}
File.foreach(ARGV.fetch(0), chomp: true) do |line|
  if line.start_with?("ASSEMBLY ")
    emit.call
    row = nil
    assembly = line.split(" ", 3).fetch(1)
  elsif line.start_with?("TYPE ")
    emit.call
    kind, visibility, rest = line.split(" ", 4)[1..]
    row = {assembly: assembly, kind: kind, visibility: visibility, type: rest.split(" [", 2).first,
      ctors: 0, methods: 0, properties: 0, events: 0, fields: 0, generic: 0, parameters: 0}
  elsif line.start_with?("  ")
    raise "member without type: #{line}" unless row
    kind = line.strip.split(" ", 2).first
    case kind
    when "CTOR", "METHOD"
      row[kind == "CTOR" ? :ctors : :methods] += 1
      row[:parameters] += arity(line, "(", ")")
    when "PROPERTY"
      row[:properties] += 1
      row[:parameters] += arity(line, "[", "]") if line.include?(" Item[")
    when "EVENT"
      row[:events] += 1
    when "FIELD"
      row[:fields] += 1
    when "GENERIC"
      row[:generic] += 1
    else
      raise "unrecognized member: #{line}"
    end
  end
end
emit.call
' docs/api/packed-public-api.txt > .testagent/iteration242/api-candidate-inventory.tsv
```

Plausibilitätsprüfungen: `rg -c '^TYPE ' docs/api/packed-public-api.txt` ergibt 3.287; `rg -c '^  (CTOR|METHOD|PROPERTY|EVENT|FIELD) ' docs/api/packed-public-api.txt` ergibt 15.442; TSV hat 3.288 Zeilen einschließlich Kopf. Die Summe der fünf Memberarten ist in jeder Zeile gleich `members`; 30 verschiedene Assemblynamen. Diese Strukturprüfungen ersetzen weder einen aktuellen Paketlauf noch manuelle Vertrags- und Testbeurteilung.
