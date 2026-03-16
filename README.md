# Hierholzer-Algorithm

Interaktive Windows-Forms-Anwendung zur Modellierung eines ungerichteten Graphen mit "Inseln" (Knoten) und "Bruecken" (Kanten), inklusive Eulerkreis-Pruefung und Hierholzer-Visualisierung.

## Inhaltsverzeichnis

1. [Projektueberblick](#projektueberblick)
2. [Hauptfunktionen](#hauptfunktionen)
3. [Technik-Stack und Voraussetzungen](#technik-stack-und-voraussetzungen)
4. [Projektstruktur](#projektstruktur)
5. [Build und Start](#build-und-start)
6. [Bedienung im Detail](#bedienung-im-detail)
7. [Dateiformate (Import/Export)](#dateiformate-importexport)
8. [Algorithmik im Projekt](#algorithmik-im-projekt)
9. [Code-Navigation fuer Entwickler](#code-navigation-fuer-entwickler)
10. [Bekannte Einschraenkungen](#bekannte-einschraenkungen)
11. [Troubleshooting](#troubleshooting)
12. [Erweiterungsideen](#erweiterungsideen)

## Projektueberblick

`Hierholz_Island` ist ein Lern- und Visualisierungsprojekt fuer Graphentheorie:

- Knoten werden als farbige Inseln dargestellt.
- Kanten werden als Bruecken zwischen Inseln dargestellt.
- Ein Eulerkreis kann geprueft und anschliessend per Hierholzer-Verfahren berechnet werden.
- Die Berechnung wird visuell animiert (aktive Kante + Farbuebergang von Teilkreisen).

Technisch ist es eine klassische .NET-Framework-Windows-Forms-Anwendung mit einer zentralen Form (`Form1`), in der sowohl UI-Logik als auch Graph-Logik umgesetzt sind.

## Hauptfunktionen

- Interaktives Platzieren von Inseln per Maus
- Dynamisches Verbinden von Inseln zu Bruecken
- Umbenennen von Inseln (Ein-Zeichen-Name)
- Verschieben von Inseln per Drag & Drop
- "Bagger"-Modus zum Entfernen von Inseln
- Eulerkreis-Bedingungspruefung:
  - mindestens eine Kante vorhanden
  - zusammenhaengender Graph (bezogen auf Knoten mit Grad > 0)
  - alle Knotengrade sind gerade
- Hierholzer-Berechnung des Eulerkreises mit Teilkreiszerlegung
- Ausgabelog in `textBox1` (Kantenfolge und Knotenfolge)
- Animation des Ergebnispfads
- Speichern/Laden als:
  - Text (`.txt`)
  - XML (`.xml`)
  - "CSV" (`.csv`, gleicher semikolon-separierter Aufbau wie Textformat)
  - Bildexport (`.png`, `.jpg`)

## Technik-Stack und Voraussetzungen

### Verwendete Technologien

- Sprache: C#
- UI: Windows Forms
- Zielframework: `.NET Framework 4.7.2`
- Projektformat: klassisches `csproj` (nicht SDK-style)
- IDE: Visual Studio (Solution-Format fuer VS 2022 vorhanden)

### Wichtige Referenzen im Projekt

- `System.Windows.Forms`
- `System.Drawing`
- `System.Xml.Linq`
- `Microsoft.VisualBasic` (fuer `Interaction.InputBox` beim Umbenennen von Inseln)

## Projektstruktur

```text
.
|- Hierholz_Island.sln
|- Hierholz_Island/
   |- Hierholz_Island.csproj
   |- Program.cs
   |- Form1.cs
   |- Form1.Designer.cs
   |- Form1.resx
   |- App.config
   |- Properties/
   |  |- AssemblyInfo.cs
   |  |- Resources.resx
   |  |- Resources.Designer.cs
   |  |- Settings.settings
   |  |- Settings.Designer.cs
   |- Resources/
   |  |- *.png, *.jpg   (UI-/Cursor-Bilder)
   |- bin/Debug/
      |- Hierholz_Island.exe
      |- Graph.txt      (Beispieldatei)
```

## Build und Start

### Empfohlener Weg (Visual Studio)

1. `Hierholz_Island.sln` in Visual Studio 2022 oeffnen.
2. Build-Konfiguration `Debug | Any CPU` auswaehlen.
3. Startup-Projekt: `Hierholz_Island`.
4. Mit `F5` starten.

### CLI-Hinweise

- `dotnet build` kann bei diesem .NET-Framework-WinForms-Projekt je nach lokaler Toolchain fehlschlagen (TaskHost/GenerateResource-Konflikt).
- Das alte .NET-Framework-`MSBuild.exe` (v4.x) kann wegen moderner C#-Syntax (`$`-Interpolation, Auto-Property-Initialisierer) ebenfalls fehlschlagen.
- Praktisch: Visual Studio 2022 als Build-Host verwenden.

### Direktstart einer vorhandenen Debug-Build-Ausgabe

Falls bereits kompiliert:

- `Hierholz_Island\bin\Debug\Hierholz_Island.exe`

Hinweis: Diese Binary kann veraltet sein, falls Quellcode geaendert wurde.

## Bedienung im Detail

### UI-Bereiche

- Linke Seite:
  - Steuer-Buttons (`Upload`, `Save`, `Clear`, Euler-Buttons, `Bagger`)
  - Hinweise/Erklaerungen
  - Ausgabefeld (`textBox1`) fuer Berechnungsergebnisse
- Mitte:
  - Zeichenflaeche `panel1` (Graph-Canvas)
- Rechte Seite:
  - `listView1` (aktuell ohne aktive Funktionalitaet)

### Maussteuerung (Normalmodus)

- Linksklick auf freie Flaeche:
  - Neue Insel erzeugen (falls Mindestabstand eingehalten)
  - Farbe:
    - manuell per ColorDialog
    - oder zufaellig bei aktivem AutoColorPicker
- Linksklick auf Insel + Ziehen:
  - Insel verschieben (mit Rand- und Abstandspruefung)
- Rechtsklick auf Insel:
  - Brueckenbau in zwei Schritten:
    1. erste Insel markieren
    2. zweite Insel waehlen => Bruecke wird erzeugt
- Mittelklick auf Insel:
  - Inselname aendern (InputBox, nur erstes Zeichen wird verwendet)

### Tastatur

- Taste `3`:
  - AutoColorPicker ein/aus
  - Bei aktivem Modus bekommt jede neue Insel automatisch eine Zufallsfarbe

### Bagger-Modus (`button5`)

Beim Aktivieren:

- Cursor wird durch ein Bagger-Icon ersetzt
- Mehrere Buttons werden deaktiviert (`button1`, `button2`, `button3`, `button4`, `button6`)
- Klick auf Insel (links/rechts/mittel) loescht die Insel
- Beim Loeschen werden:
  - Adjazenzliste angepasst
  - betroffene Kanten entfernt
  - Kantenindizes neu konsolidiert
  - laufende Animationen beendet

Beim Deaktivieren:

- Standardcursor wird wiederhergestellt
- deaktivierte Buttons werden wieder aktiv

### Buttons (logisch)

- `Upload` (`button3`): Import aus `.txt`, `.csv`, `.xml`
- `Save` (`button2`): Export nach `.txt`, `.xml`, `.png`, `.jpg`
- `Clear` (`button6`): kompletter Reset
- `Eulerscher Weg?` (`button1`):
  - prueft nur die Existenzbedingungen
  - Buttonfarbe:
    - Gruen = Bedingungen erfuellt
    - Rot = Bedingungen nicht erfuellt
- Bildbutton oberhalb `Bagger` (`button4`):
  - startet Eulerkreis-Berechnung (Hierholzer), schreibt Ergebnis in `textBox1` und animiert das Resultat

Hinweis: Die statische Hilfsbeschriftung im Fenster ist nicht in allen Punkten synchron zur implementierten Steuerlogik. Die hier dokumentierte Bedienung basiert auf dem aktuellen Code.

## Dateiformate (Import/Export)

### Text / CSV (gleiches Inhaltsschema)

Dateiaufbau:

```text
#INSELN
x;y;R;G;B;Name
...
#BRUECKEN
a;b;R;G;B;Name
...
```

Bedeutung:

- Insel-Zeile:
  - `x;y`: Pixelposition auf dem Panel
  - `R;G;B`: Insel-Farbe (0..255)
  - `Name`: erstes Zeichen wird verwendet
- Bruecken-Zeile:
  - `a;b`: Inselindizes (0-basiert, bezogen auf Reihenfolge der Inseln im Dateiabschnitt)
  - `R;G;B`: Brueckenfarbe
  - `Name`: Kantenname (erstes Zeichen)

Importverhalten:

- Ungueltige Zeilen werden uebersprungen.
- Ungueltige Indizes werden ignoriert.
- Duplikatkanten werden nicht angelegt.
- Farben werden auf `0..255` begrenzt.

### XML

Gespeicherte Grundstruktur:

```xml
<Graph>
  <Inseln>
    <Inseln>
      <X>...</X>
      <Y>...</Y>
      <R>...</R>
      <G>...</G>
      <B>...</B>
      <Name>...</Name>
    </Inseln>
  </Inseln>
  <Brücken>
    <Bridge>
      <A>...</A>
      <B>...</B>
      <R>...</R>
      <G>...</G>
      <Blue>...</Blue>
      <EdgeName>...</EdgeName>
    </Bridge>
  </Brücken>
</Graph>
```

Wichtig:

- Der Loader erwartet:
  - Insel-Liste unter `Graph/Inseln/Inseln`
  - Bruecken-Liste unter `Graph/Brücken/Bridge`
- Als Kantenname wird bevorzugt `EdgeName`, alternativ `Name` gelesen.

### Bildexport

- `.png` und `.jpg` speichern den aktuellen Zustand von `panel1` als Bitmap.
- Das reine Datenmodell wird dadurch nicht gespeichert (nur Visualisierung).

## Algorithmik im Projekt

### Datenmodell

- `Insel`
  - `Position` (`Point`)
  - `Farbe` (`Color`)
  - `Name` (`char`)
- `Bruecke`
  - `InselA`, `InselB` (Knotenindizes)
  - `Name` (`char`)
  - `Farbe`, `StartFarbe`, `ZielFarbe`
  - `Benutzt` (im aktuellen Stand nicht aktiv genutzt)
- Graph intern:
  - `List<List<int>> adjazenzliste` (ungerichtet)

### Eulerkreis-Pruefung (`Eulerkreis()`)

Reihenfolge der Checks:

1. Anzahl Kanten > 0
2. Zusammenhang fuer alle Knoten mit Grad > 0 (BFS in `EinsameInsel()`)
3. Jeder Knotengrad gerade (`Grad(i) % 2 == 0`)

Bei Fehler:

- Meldung per MessageBox
- Text in `textBox1`
- `button1` auf OrangeRot

Bei Erfolg:

- `button1` auf Lime

### Hierholzer-Implementierung (`Hierholzer()`)

Umsetzungsidee:

1. Adjazenzliste wird kopiert (`adjazenzKopie`), damit Originalgraph fuer Darstellung erhalten bleibt.
2. Startknoten: erster Knoten mit Grad > 0.
3. `BaueEinfachenKreis(start, adjazenzKopie)` erzeugt einen ersten geschlossenen Zyklus.
4. Solange es im bisherigen Zyklus einen Knoten mit Restkanten gibt:
   - neuen Teilkreis ab diesem Knoten bauen
   - mit `FuegeTeilkreisEin(...)` in den Hauptkreis einspleissen
5. Ergebnis ist die Euler-Knotenfolge (`List<int>`), parallel werden `teilkreise` fuer Visualisierung gespeichert.

### Visualisierung und Animation

- Jede Bruecke hat eine Start- und Zielfarbe.
- Nach Berechnung werden Ziel-Farben anhand der Teilkreise gesetzt (`StarteBrueckenFarbwechselZuTeilkreisen()`).
- Timer:
  - Intervall: `450 ms`
  - Farbtransition: `12` Schritte
  - pro Tick:
    - Brueckenfarbe linear interpolieren
    - aktive Eulerkante weiterschalten
- Aktive Kante wird in `LimeGreen` hervorgehoben (`VisualisierePfad()`).

### Komplexitaet (grob)

- Euler-Check:
  - BFS: `O(V + E)`
  - Gradpruefung: `O(V)`
- Hierholzer:
  - typischerweise `O(E)` fuer reine Kantenverarbeitung
  - im aktuellen Listen-/Splice-Ansatz je nach Graphstruktur mit Mehrkosten durch Listenoperationen

## Code-Navigation fuer Entwickler

Wichtige Dateien:

- `Hierholz_Island/Program.cs`: App-Entry
- `Hierholz_Island/Form1.Designer.cs`: UI-Layout, Controls, Event-Wiring
- `Hierholz_Island/Form1.cs`: Graphlogik, Interaktion, I/O, Euler-Algorithmik

Wichtige Methoden in `Form1.cs`:

- Interaktion:
  - `HandleMouseInput`
  - `Panel1_MouseDown`, `Panel1_MouseMove`, `Panel1_MouseUp`
  - `button5_Click` (Bagger)
- Persistenz:
  - `SaveGraph`, `LoadGraphFromText`
  - `SavePanelAsXml`, `LoadGraphFromXml`
  - `button2_Click` (Save), `button3_Click` (Upload)
- Euler:
  - `Eulerkreis`, `EinsameInsel`, `Grad`
  - `Hierholzer`, `BaueEinfachenKreis`, `FuegeTeilkreisEin`
  - `FormatiereKantenfolge`, `FormatiereKnotenfolge`
- Animation:
  - `StarteBrueckenFarbwechselZuTeilkreisen`
  - `AnimationTimer_Tick`
  - `VisualisierePfad`
- Konsistenz:
  - `BrueckeBauen`, `BrueckeEinreißen`, `RebuildBridgeListFromAdjacency`

## Bekannte Einschraenkungen

- Nur einfacher ungerichteter Graph:
  - keine Mehrfachkanten
  - keine Schleifenkante (Knoten auf sich selbst)
- Namen:
  - Auto-Vergabe fuer Inseln und Bruecken laeuft modulo 26 (`A..Z`, `a..z`), danach Wiederholung
  - Name ist jeweils nur ein Zeichen
- Einige Felder/Controls sind vorbereitet, aber derzeit ungenutzt oder nicht voll integriert:
  - `listView1`
  - `btnClearCircles_Click` (separate Clear-Methode, im Designer nicht als Button verdrahtet)
  - `MindestAbstand`-Konstante wird deklariert, aber lokal wird ebenfalls fixer Wert `60` verwendet
- `dotnet build` ist fuer dieses klassische WinForms-.NET-Framework-Projekt nicht immer der stabilste Weg; Visual Studio ist empfohlen.

## Troubleshooting

### "Dateiformat wird nicht unterstuetzt"

- Sicherstellen, dass Dateiendung `.txt`, `.csv` oder `.xml` ist.

### "Ein Eulerkreis ist nicht moeglich"

Pruefen:

1. Graph hat mindestens eine Bruecke.
2. Alle Knoten mit Grad > 0 liegen in einer Zusammenhangskomponente.
3. Jeder Knoten hat geraden Grad.

### Insel kann nicht verschoben werden

- Neue Position ist evtl. ausserhalb des Panels oder verletzt den Mindestabstand zu einer anderen Insel.

### Build-Fehler ueber CLI

- Projekt in Visual Studio 2022 bauen und starten.
- Bei CLI-Builds sicherstellen, dass passende Build-Tools fuer .NET Framework vorhanden sind.

## Erweiterungsideen

- Vollstaendige Trennung von UI und Graphkern (z. B. Services + DTOs)
- Unit-Tests fuer:
  - Eulerkreis-Pruefung
  - Hierholzer-Algorithmus
  - Parser fuer Text/XML
- Undo/Redo fuer Interaktionen
- Aktiv nutzbare Seitenleiste (`listView1`) fuer:
  - Knotengrade
  - Kantenliste
  - Schritt-fuer-Schritt-Trace
- Mehrsprachigkeit (de/en)
- Export/Import als JSON
- Konfigurierbare Animation (Geschwindigkeit, Darstellung)

---
