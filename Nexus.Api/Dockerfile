ARG ENV=prod

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY Nexus.Api.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
ARG ENV=prod
WORKDIR /app
COPY --from=build /app/publish .
RUN mkdir -p /app/DataProtection-Keys
VOLUME /app/DataProtection-Keys

# Map CLI env (dev|prod) → ASP.NET environment name and bake into entrypoint
RUN case "$ENV" in \
      dev)  ASPNET_ENV=Development ;; \
      prod) ASPNET_ENV=Production ;; \
      *)    echo "Unsupported ENV=$ENV" >&2; exit 1 ;; \
    esac \
    && printf '#!/bin/sh\nexport ASPNETCORE_ENVIRONMENT=%s\nexport ASPNETCORE_URLS=http://+:8080\nexec dotnet Nexus.Api.dll "$@"\n' "$ASPNET_ENV" > /entrypoint.sh \
    && chmod +x /entrypoint.sh

EXPOSE 8080
ENTRYPOINT ["/entrypoint.sh"]
