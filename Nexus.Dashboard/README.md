# Nexus Dashboard (React)

SPA em Vite + React + TypeScript. Consome a API Nexus via proxy em desenvolvimento.

O dashboard Blazor original está arquivado em [`../Nexus.Dashboard.Legacy/`](../Nexus.Dashboard.Legacy/).

## Executar

1. Suba a API Nexus (`Nexus.Api/Nexus.Api.csproj`) — em Development escuta em `https://localhost:444` (ver `appsettings.Development.json`).
2. Instale dependências e inicie o dev server:

```bash
cd Nexus.Dashboard
npm install
npm run dev
```

3. Abra `http://localhost:5173/dashboard`.

## Configuração

`.env.development` aponta o proxy do Vite para a API local:

```
VITE_API_PROXY_TARGET=https://localhost:444
VITE_API_BASE_URL=
```

Com `VITE_API_BASE_URL` vazio, o dashboard chama `/api/...` no dev server (`localhost:5173`) e o Vite repassa para o Kestrel na porta 444.

Em produção, sirva o build (`npm run build`) atrás de um reverse proxy que encaminhe `/api` para a API Nexus.

## Build

```bash
npm run build
npm run preview
```
