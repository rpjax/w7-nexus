FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY Nexus.Api/Nexus.Api.csproj Nexus.Api/
RUN dotnet restore Nexus.Api/Nexus.Api.csproj

COPY Nexus.Api/ Nexus.Api/
RUN dotnet publish Nexus.Api/Nexus.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .
COPY Nexus.Api/Certificates/balihai.shop.fullchain.pem Certificates/
COPY Nexus.Api/Certificates/balihai.shop.privatekey.pem Certificates/

RUN mkdir -p /app/DataProtection-Keys
VOLUME /app/DataProtection-Keys

ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8000

ENTRYPOINT ["dotnet", "Nexus.Api.dll"]
