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
