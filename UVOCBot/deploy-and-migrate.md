# Deployment and Database Migration Notes

Docker image build and deployment:

```sh
cd UVOCBot
dotnet publish -c Release --os linux --arch x64 /t:PublishContainer -p ContainerRegistry=<remote>
```

## Migration

UVOCBot now performs runtime migrations.
The old commands for generating idempotent scripts were:

```sh
cd UVOCBot.Core
dotnet ef --startup-project "..\UVOCBot\UVOCBot.csproj" migrations Add "<MigrationName>"
dotnet ef --startup-project "..\UVOCBot\UVOCBot.csproj" database update
dotnet ef --startup-project "..\UVOCBot\UVOCBot.csproj" migrations script --idempotent -o migrate.sql
```
