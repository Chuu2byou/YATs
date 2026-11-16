# Datenbank-Migrationen mit EF Core

Diese Doku erklärt, wie Migrationen in **YATs** funktionieren und wie man sie benutzt.
Grundlage sind die offiziellen EF-Core-Dokumente (Links am Ende).

## 1. Was macht eine Migration?

Das C#-Modell (`TimeEntry`, `AppDbContext`) und das PostgreSQL-Schema müssen
zueinander passen. Ändert sich das Modell, erzeugt EF Core aus dem Unterschied
zum letzten Stand eine **Migration**: eine C#-Klasse mit `Up()` und `Down()`.

EF Core führt Buch in der Tabelle `"__EFMigrationsHistory"`. Dort steht, welche
Migrationen bereits angewendet wurden. Dadurch werden beim Update nur die
fehlenden Migrationen ausgeführt.

Pro Migration entstehen drei Dateien (alle gehören ins Git):

| Datei | Zweck |
| --- | --- |
| `<Zeitstempel>_<Name>.cs` | Die eigentliche Migration mit `Up()` / `Down()` |
| `<Zeitstempel>_<Name>.Designer.cs` | Snapshot des Modells **zum Zeitpunkt dieser Migration** (autogeneriert) |
| `AppDbContextModelSnapshot.cs` | Snapshot des **aktuellen** Modells; Basis für den nächsten Diff (autogeneriert) |

### Wie entsteht der Snapshot?

Den Snapshot schreibt man **nicht selbst** und es gibt dafür auch keinen eigenen
Befehl. `dotnet ef migrations add <Name>` macht alles in einem Schritt:

1. EF Core liest das aktuelle Modell aus `AppDbContext` / den Entities.
2. Es vergleicht dieses Modell mit `AppDbContextModelSnapshot.cs`.
3. Die Differenz landet als `Up()` / `Down()` in der neuen Migration.
4. `AppDbContextModelSnapshot.cs` wird automatisch auf den neuen Modellstand
   gebracht und die `.Designer.cs` für die neue Migration erzeugt.

Beispiel: In `TimeEntry` kommt `public string? Notes { get; set; }` dazu.
Dann erzeugt

```bash
dotnet ef migrations add AddTimeEntryNotes \
  --project "Yet Another Timesaver" \
  --output-dir Backend/Migrations
```

eine Migration mit `AddColumn<string>("Notes", ...)` **und** aktualisiert den
Snapshot. Beim nächsten `migrations add` ist dieser neue Stand die Basis.

Der Snapshot ist **kein Daten-Backup**, sondern nur eine C#-Beschreibung des
Modells (Tabellen, Spalten, Typen, Keys, Indizes). Die Datenbank selbst kennt
ihn nicht.

Wenn man die letzte Migration wieder loswerden will, setzt
`dotnet ef migrations remove` auch den Snapshot auf den vorherigen Stand
zurück. Wurde die Migration schon angewendet, vorher
`dotnet ef database update <VorherigeMigration>` ausführen oder `--force`
verwenden.

## 2. Stand in diesem Projekt

- .NET 10, EF Core `10.0.4`, Npgsql-Provider `10.0.3`
- DbContext: `Backend/AppDbContext.cs` mit `DbSet<TimeEntry>`
- Migration: `Backend/Migrations/20261107161257_InitialCreate.cs`
- Lokales Tool: `.config/dotnet-tools.json` → `dotnet-ef` 10.0.4
- Connection String: `ConnectionStrings:DefaultConnection`
  (Vorlage: `appsettings.Development.example.json`)

Die Migration `InitialCreate` erzeugt die Tabelle `TimeEntries`:

| Spalte | Typ | Nullable |
| --- | --- | --- |
| `Id` | `integer`, Identity | nein (PK) |
| `Description` | `text` | nein |
| `StartTime` | `timestamp with time zone` | nein |
| `EndTime` | `timestamp with time zone` | ja |

`Down()` löscht die Tabelle wieder – **dabei gehen alle Daten verloren.**

## 3. Wie die Migration angewendet wird

In `Program.cs` steht beim Start:

```csharp
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}
```

Das ist die Strategie **„Migration beim App-Start“** (Runtime Migration).
Beim Start prüft EF Core `"__EFMigrationsHistory"` und führt alle noch nicht
angewendeten Migrationen aus. Für Entwicklung und dieses Projekt ist das
bequem; für Produktion siehe Abschnitt 7.

> **Wichtig ab EF Core 9:** `Migrate()` wirft eine Exception
> (`PendingModelChangesWarning`), wenn das Modell seit der letzten Migration
> geändert wurde. Wer also `TimeEntry` ändert und keine Migration erzeugt,
> bekommt beim nächsten Start einen Fehler. Das ist Absicht und verhindert
> ein stilles Auseinanderlaufen von Modell und Datenbank.

## 4. Typischer Workflow

Immer im Repo-Root ausführen (`/home/weiting/RiderProjects/YATs`).
Wegen des Leerzeichens im Projektnamen muss der Pfad in Anführungszeichen.

```bash
# Einmalig nach dem Klonen: lokales dotnet-ef wiederherstellen
dotnet tool restore

# 1. Modell ändern (z. B. neue Property in Backend/TimeEntry.cs)

# 2. Migration erzeugen
dotnet ef migrations add AddTimeEntryNotes \
  --project "Yet Another Timesaver" \
  --output-dir Backend/Migrations

# 3. Erzeugte Dateien ansehen und prüfen, ob Up()/Down() das Richtige tun

# 4. Anwenden – entweder App starten (macht Migrate() automatisch)
#    oder explizit:
dotnet ef database update --project "Yet Another Timesaver"
```

`--output-dir Backend/Migrations` ist hier wichtig, weil die bestehenden
Migrationen dort liegen. Ohne die Option legt EF Core ein neues
`Migrations/`-Verzeichnis im Projekt-Root an.

## 5. Befehle (Cheat Sheet)

```bash
# Alle Migrationen und ihren Status anzeigen
dotnet ef migrations list --project "Yet Another Timesaver"

# Prüfen, ob das Modell Änderungen ohne Migration hat (für CI)
dotnet ef migrations has-pending-model-changes --project "Yet Another Timesaver"

# SQL ansehen, ohne es auszuführen
dotnet ef migrations script --project "Yet Another Timesaver"

# Idempotentes SQL-Skript für Deployments erzeugen
dotnet ef migrations script --idempotent \
  --project "Yet Another Timesaver" -o migrations.sql

# Auf eine bestimmte Migration vor- oder zurücksetzen
dotnet ef database update <MigrationsName> --project "Yet Another Timesaver"

# Letzte (noch nicht angewendete) Migration wieder entfernen
dotnet ef migrations remove --project "Yet Another Timesaver"

# Deployment-Bundle erzeugen (braucht zur Laufzeit kein SDK)
dotnet ef migrations bundle --project "Yet Another Timesaver" -o efbundle
```

In Rider gehen die wichtigsten Befehle auch über das Kontextmenü des Projekts
unter **Entity Framework Core**.

## 6. Was man nicht tun sollte

- **Angewendete Migrationen nachträglich ändern.** Die Datenbank hat sie schon
  ausgeführt; die Änderung würde nie ankommen. Stattdessen eine neue Migration
  erzeugen.
- **Designer- oder Snapshot-Dateien von Hand editieren.** Sie werden generiert.
- **Migrationen aus dem Git lassen.** Ohne sie kann kein Teammitglied und keine
  CI-Umgebung die Datenbank aufbauen.
- **Umbenennungen erwarten.** EF erkennt keine Renames: Aus `Description` →
  `Notes` wird „Spalte löschen + neue Spalte anlegen“. In der erzeugten
  Migration prüfen und ggf. `RenameColumn` verwenden, sonst sind die Daten weg.
- **`EnsureCreated()` mit Migrationen mischen.** `EnsureCreated()` umgeht die
  Migrationshistorie und legt die Datenbank inkompatibel an.
- **Zwei Branches mit derselben Basis-Migration.** Beim Merge entstehen zwei
  Migrationen mit gleichem Vorgänger. Dann eine der beiden löschen und neu
  erzeugen, damit die Kette wieder linear ist.

## 7. Produktion (Kurzfassung)

`db.Database.Migrate()` beim Start ist für Entwicklung okay, aber in Produktion
nur mit Bedacht:

- Bei mehreren Instanzen kann es zu parallelen Migrationen kommen. Ab EF Core 9
  schützt ein DB-weiter Lock davor, trotzdem ist ein separater Deployment-Schritt
  sauberer.
- Die App braucht Schema-Rechte, die sie im Normalbetrieb nicht bräuchte.
- Fehler passieren beim Start, ohne Review-Möglichkeit.

Bessere Varianten laut offizieller Doku:

1. **SQL-Skript** (`dotnet ef migrations script --idempotent`), wenn ein DBA
   oder ein Review-Prozess die Änderungen sehen muss.
2. **Migration Bundle** (`dotnet ef migrations bundle`), wenn automatisch
   deployt wird: eine ausführbare Datei, die ohne SDK und Quellcode läuft.

## 8. Offizielle Online-Dokus

- Überblick: <https://learn.microsoft.com/ef/core/managing-schemas/migrations/>
- Migrationen verwalten (add/remove/list, Pending Changes):
  <https://learn.microsoft.com/ef/core/managing-schemas/migrations/managing>
- Migrationen anwenden (Skripte, Bundles, Runtime, Locking):
  <https://learn.microsoft.com/ef/core/managing-schemas/migrations/applying>
- CLI-Referenz `dotnet ef`:
  <https://learn.microsoft.com/ef/core/cli/dotnet>
- Npgsql / PostgreSQL-Provider:
  <https://www.npgsql.org/efcore/>
- EF Core 10 Neuerungen:
  <https://learn.microsoft.com/ef/core/what-is-new/ef-core-10.0/whatsnew>

## 9. Verbindung zur ausgewählten Datei

Die ausgewählte Klasse erbt von `Microsoft.EntityFrameworkCore.Migrations.Migration`.
`Up(MigrationBuilder)` beschreibt den Vorwärts-Schritt, `Down(...)` den
Rückwärts-Schritt. `migrationBuilder.CreateTable(...)` ist eine
provider-unabhängige Anweisung; Npgsql übersetzt sie in `CREATE TABLE`.
Das Annotation-Attribut legt fest, dass `Id` eine PostgreSQL-Identity-Spalte
wird. `timestamp with time zone` entsteht aus `DateTime` (bzw. `DateTime?`),
weil Npgsql `DateTime` standardmäßig auf `timestamptz` abbildet.
