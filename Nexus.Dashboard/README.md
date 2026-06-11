# Nexus Dashboard (React)

SPA em Vite + React + TypeScript. Consome a API Nexus via proxy em desenvolvimento.

O dashboard Blazor original está arquivado em [`../Nexus.Dashboard.Legacy/`](../Nexus.Dashboard.Legacy/).

## Executar

1. Suba a API Nexus (`Nexus/Nexus.csproj`) — padrão `https://websete.localhost:7254/`.
2. Instale dependências e inicie o dev server:

```bash
cd Nexus.Dashboard
npm install
npm run dev
```

3. Abra `http://localhost:5173/dashboard`.

## Configuração

Ajuste o alvo do proxy em `.env.development`:

```
VITE_API_PROXY_TARGET=https://websete.localhost:7254
```

Em produção, sirva o build (`npm run build`) atrás de um reverse proxy que encaminhe `/api` para a API Nexus.

## Build

```bash
npm run build
npm run preview
```
