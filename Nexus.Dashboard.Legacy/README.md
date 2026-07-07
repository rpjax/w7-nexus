# Nexus Dashboard

Blazor Server UI restaurada do histórico do repositório (`bcbf286`), isolada do projeto `Nexus` (API).

## Executar

1. Suba a API Nexus (`Nexus.Api/Nexus.Api.csproj`) na URL configurada em `NexusApi:BaseUrl` (padrão `https://websete.localhost:7254/`).
2. Execute este projeto:

```bash
dotnet run --project Nexus.Dashboard/Nexus.Dashboard.csproj
```

3. Abra `https://localhost:5299/dashboard`.

## Configuração

Ajuste `NexusApi:BaseUrl` em `appsettings.Development.json` se a API usar outra porta ou host.
