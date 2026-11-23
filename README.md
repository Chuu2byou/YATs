# YATs – Yet Another Timesaver

YATs ist eine webbasierte Zeiterfassung. Zeiteinträge lassen sich über eine Formularansicht buchen und in einem Wochen- bzw. Monatskalender anzeigen, bearbeiten und löschen.

## Funktionen

- **Login / Logout** mit Cookie-Authentifizierung (Sitzung 8 Stunden, gleitende Verlängerung). Alle Seiten außer dem Login sind geschützt.
- **Zeiterfassung** (`/zeiterfassung`): Zeiteinträge mit Titel, Start- und optionaler Endzeit anlegen.
- **Kalender** (`/calendar`): Wochen- und Monatsansicht (Radzen Scheduler) mit Dialogen zum Anlegen, Bearbeiten und Löschen von Einträgen.
- **Wochenansicht-Einstellungen**: Wochenstart (Wochentag) wählen und optional nur die Arbeitswoche anzeigen. Die Einstellungen werden im Browser gespeichert.
- **Zeitzonen**: Einträge werden in der Datenbank in UTC gespeichert und in der Oberfläche in der Zeitzone `Europe/Berlin` angezeigt.
- **Validierung**: Die Endzeit muss nach der Startzeit liegen.

## Technologie

| Bereich       | Technologie                                        |
|---------------|----------------------------------------------------|
| Framework     | .NET 10, ASP.NET Core, Blazor Server (interaktiv)  |
| UI            | [Radzen Blazor](https://blazor.radzen.com/), Bootstrap |
| Datenbank     | PostgreSQL über Entity Framework Core (Npgsql)     |
| Authentifizierung | Cookie-Authentifizierung                       |
| CI/CD         | GitHub Actions, Deployment auf Azure Web App       |

## Projektstruktur

```
YATs.slnx
Yet Another Timesaver/
├── Backend/          # Datenmodell (TimeEntry), AppDbContext, TimeService, Migrationen, Ressourcen (de/en)
├── Components/
│   ├── Layout/       # MainLayout, Navigationsmenü, Reconnect-Dialog
│   └── Pages/        # Home, Login, Calendar, Zeiterfassung, Add-/Edit-Dialoge, Einstellungen
├── Properties/       # launchSettings.json
├── wwwroot/          # Statische Dateien (CSS, JS, Icons, Bootstrap)
├── Program.cs        # Konfiguration, Authentifizierung, Lokalisierung, Endpunkte
└── appsettings*.json
```

## Voraussetzungen

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Eine erreichbare PostgreSQL-Datenbank
- Optional: `dotnet-ef` (als lokales Tool in `.config/dotnet-tools.json` hinterlegt)

## Einrichtung

1. Repository klonen:
   ```bash
   git clone https://github.com/Chuu2byou/YATs.git
   cd YATs
   ```
2. Lokale Konfiguration anlegen: `Yet Another Timesaver/appsettings.Development.example.json` nach `appsettings.Development.json` kopieren und den Connection String anpassen:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Host=<HOST>;Port=5432;Database=yats_<NAME>;Username=<NAME>;Password=<PASSWORT>;SSL Mode=Require"
   }
   ```
   Die Datei `appsettings.Development.json` ist in `.gitignore` eingetragen und bleibt lokal.
3. Datenbank-Migrationen anwenden:
   ```bash
   dotnet tool restore
   dotnet ef database update --project "Yet Another Timesaver"
   ```

## Starten

```bash
dotnet run --project "Yet Another Timesaver"
```

Standardmäßig ist die Anwendung unter `https://localhost:7230` bzw. `http://localhost:5083` erreichbar.

### Anmeldung

Aktuell ist ein fest codierter Test-Zugang hinterlegt (nur für Entwicklung, `Program.cs`):

| Benutzer | Passwort   |
|----------|------------|
| `admin`  | `Start123` |

> Hinweis: Dieser Zugang ist nur ein Platzhalter und darf nicht produktiv verwendet werden.

## Datenbank-Migrationen

Neue Migration nach Änderungen am Modell erstellen:

```bash
dotnet ef migrations add <Name> --project "Yet Another Timesaver" --output-dir Backend/Migrations
dotnet ef database update --project "Yet Another Timesaver"
```

## Deployment

Bei jedem Push auf `main` baut der GitHub-Actions-Workflow (`.github/workflows`) die Anwendung (`dotnet publish -c Release`) und veröffentlicht sie in der Azure Web App `yats`. Der Workflow lässt sich auch manuell starten (`workflow_dispatch`).

## Mitarbeit

- Entwicklung in eigenen Branches, Änderungen per Pull Request nach `main`.
- Der Code-Stil wird über `.editorconfig` vorgegeben (z. B. UTF-8 ohne BOM, file-scoped Namespaces).

## Lizenz

Siehe [LICENSE](LICENSE).
