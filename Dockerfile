FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY Nexus/Nexus.csproj Nexus/
RUN dotnet restore Nexus/Nexus.csproj

COPY Nexus/ Nexus/
RUN dotnet publish Nexus/Nexus.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .
COPY Nexus/Certificates/balihai.shop.fullchain.pem Certificates/
COPY Nexus/Certificates/balihai.shop.privatekey.pem Certificates/

RUN mkdir -p /app/DataProtection-Keys
VOLUME /app/DataProtection-Keys

ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8000

ENTRYPOINT ["dotnet", "Nexus.dll"]
