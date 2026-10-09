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

Bilderna ligger i `App_Data/media/images/` tillsammans med `manifest.csv`, som skrivs om efter varje ändring.
När appen startar läggs bilder som står i manifestet men saknas i databasen till, så en ny databas fylls i genom att
kopiera bildmappen.

Nya bilder i utvecklingsmiljön: lägg dem i `assets/new_images/` med en `manifest.csv` och starta appen (se B65 i
projektbeskrivningen).

```
file,kind,tags,source
goblin_chief.webp,portrait,goblin monster,Egen bild
quest.webp,icon,quest scroll,
```

Ark med många runda bilder läggs i `assets/new_images/sheets/`; vid start klipps de ut till inkorgen, med ett
numrerat översiktsark att tagga efter, och arket flyttas till `assets/sources/`. Manifestet i `media/images/` skrivs
bara i utvecklingsmiljön.
