# Taleshaven

Systemoberoende play-by-post-plattform för rollspel, byggd med .NET 10 och Blazor Server.

- Projektbeskrivning och användarkrav: [docs/projektbeskrivning.md](docs/projektbeskrivning.md)

## Kom igång

Kräver .NET 10 SDK och Docker.

```bash
docker compose up -d        # startar PostgreSQL på localhost:5432
dotnet build
dotnet run --project src/Taleshaven.Web
dotnet test
```

I VS Code (med C# Dev Kit) räcker det att trycka **F5**: konfigurationen
*Taleshaven.Web* startar databasen i Docker och sedan appen med debugger.

I utvecklingsläge körs databasmigreringar automatiskt när appen startar. Ingen
riktig e-posttjänst är kopplad än; efter registrering visas bekräftelselänken
direkt på sidan.

### Ny migrering

```bash
dotnet ef migrations add <Namn> --project src/Taleshaven.Infrastructure --startup-project src/Taleshaven.Web --output-dir Data/Migrations
```

### Bildbiblioteket

Porträtt och ikoner kan läggas in från källark, bilder med många runda motiv. Arken ligger i
`assets/sources/portraits/` eller `assets/sources/icons/` och versionshanteras inte; taggfilerna bredvid
(`<ark>.tags.txt`) gör det. Se B64 i projektbeskrivningen.

```bash
# Klipp ut ett nytt ark, skapa översiktsark och en tom taggfil att fylla i
dotnet run --project src/Taleshaven.Web -- images preview ../../assets/sources/portraits/<ark>.png
# Lägg in nya bilder och uppdatera taggar (kan köras om; --dry-run visar bara vad som skulle hända)
dotnet run --project src/Taleshaven.Web -- images import --dry-run
dotnet run --project src/Taleshaven.Web -- images import
```
