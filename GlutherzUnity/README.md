# Glutherz – Der Schmied von Aschenfurt (Unity, First-Person)

Ein 3D-Schmiede-Action-RPG aus der Ego-Perspektive: Gegner bekämpfen, Erz sammeln,
am Amboss Ausrüstung mit Seltenheitsstufen schmieden und fünf Bosse besiegen.

Das Spiel baut Welt, Gegner und Oberfläche vollständig im Code auf. Es braucht keine
Modelle, Texturen, Sounds oder vorbereitete Szene.

## Einrichten

1. In Unity Hub ein neues Projekt mit **Unity 6** und der Vorlage **Universal 3D** anlegen.
2. Den Ordner `Assets/Glutherz` aus diesem Verzeichnis in den `Assets`-Ordner des Projekts kopieren.
3. Eine leere Szene öffnen (oder die mitgelieferte `SampleScene`) und auf **Play** drücken.

Kamera und Licht der Szene werden beim Start automatisch ersetzt bzw. ergänzt.

## Steuerung

| Taste | Aktion |
|---|---|
| W A S D / Maus | Laufen / Umsehen |
| Umschalt / Leertaste | Sprinten / Springen |
| Linke Maustaste | Zuschlagen (gedrückt halten für Serie) |
| Rechte Maustaste | Blocken (Schaden stark reduziert) |
| Q | Wuchtschlag (Flächenschaden, 6 s Abklingzeit) |
| R | Heiltrank |
| E | Benutzen (Amboss im Dorf) |
| Tab | Inventar und Charakter |

## Spielablauf

- **Aschenfurt** ist das sichere Dorf. Am Amboss unter dem Schmiededach öffnet `E` die Schmiede,
  die Händlerin und das Reise-Menü.
- Nach Norden reihen sich fünf Gebiete aneinander. Gegner und Erzadern (dreimal draufschlagen)
  liefern das Erz des jeweiligen Gebiets.
- Vor dem leuchtenden Siegel am Ende jedes Gebiets wartet der Boss. Ist er besiegt, verschwindet
  das Siegel und das nächste Gebiet ist offen.
- Beim Schmieden entscheidet ein Timing-Minispiel mit vier Hammerschlägen über Qualität und
  Seltenheit (Gewöhnlich bis Legendär). Die Schmiedekunst steigt mit jedem Stück.
- Der Spielstand wird automatisch gespeichert (`PlayerPrefs`).

## Aufbau des Codes (`Assets/Glutherz/Scripts`)

| Datei | Inhalt |
|---|---|
| `GameBootstrap.cs` | Startpunkt: baut Welt, Dorf, Gebiete und Siegel |
| `GameData.cs` | Spieldaten, Gegenstände, Werteberechnung, Speichern |
| `PlayerController.cs` | Ego-Steuerung und Kampf |
| `Enemy.cs`, `ZoneSpawner.cs` | Gegner-KI und Nachschub je Gebiet |
| `OreNode.cs`, `ForgeStation.cs`, `Interactable.cs` | Erzadern und Amboss |
| `GameUI.cs` | HUD, Menüs und Schmiede-Minispiel |
| `In.cs`, `Sfx.cs`, `Prim.cs` | Eingabe, erzeugte Soundeffekte, Bau-Hilfen |
