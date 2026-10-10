# Taleshaven

Systemoberoende play-by-post-plattform för rollspel, byggd med .NET 10 och Blazor Server.

- Projektbeskrivning och användarkrav: [docs/projektbeskrivning.md](docs/projektbeskrivning.md)

## Kom igång

Kräver .NET 10 SDK och Docker.

```bash
docker compose up -d        # startar PostgreSQL (localhost:5432) och Mailpit (localhost:8025)
dotnet build
dotnet run --project src/Taleshaven.Web
dotnet test
```

I VS Code (med C# Dev Kit) räcker det att trycka **F5**: konfigurationen
*Taleshaven.Web* startar databasen i Docker och sedan appen med debugger.

I utvecklingsläge körs databasmigreringar automatiskt när appen startar, och efter
registrering visas bekräftelselänken också direkt på sidan.

### E-post

I utvecklingsmiljön skickas alla mejl (bekräftelse, nytt lösenord, byte av e-post) till Mailpit och läses på
http://localhost:8025. I produktion anges SMTP-servern under `Email` i konfigurationen (`Host`, `Port`, `UserName`,
`Password`, `EnableSsl`, `From`); utan `Email:Host` skickas inga mejl.

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
