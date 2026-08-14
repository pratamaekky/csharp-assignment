# Build context is csharp-assignment/ (the repo root), not this folder --
# the COPY paths below assume that.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ChatWs/ChatWs.csproj ChatWs/
RUN dotnet restore ChatWs/ChatWs.csproj

COPY ChatWs/ ChatWs/
RUN dotnet publish ChatWs/ChatWs.csproj -c Release -o /app/publish --no-restore

# aspnet:10.0 already runs as a non-root "app" user by default.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "ChatWs.dll"]
