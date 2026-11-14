# Build: publishing Api also builds the Angular app (through AngularApp.esproj), so the SDK image needs Node.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
COPY --from=node:24-bookworm-slim /usr/local/bin /usr/local/bin
COPY --from=node:24-bookworm-slim /usr/local/lib/node_modules /usr/local/lib/node_modules
WORKDIR /src
COPY . .
RUN dotnet publish Api/Api.csproj -c Release -o /app

# Run: only the ASP.NET runtime and the published output (Angular is in wwwroot). Listens on 8080.
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "Api.dll"]
