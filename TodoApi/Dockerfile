# Build context is csharp-assignment/ (the repo root), not this folder --
# the COPY paths below assume that.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY TodoApi/TodoApi.csproj TodoApi/
RUN dotnet restore TodoApi/TodoApi.csproj

COPY TodoApi/ TodoApi/
RUN dotnet publish TodoApi/TodoApi.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# aspnet:10.0 ships a non-root "app" user (uid 1654) but does not activate it
# by default — USER $APP_UID opts in explicitly.
USER $APP_UID
ENTRYPOINT ["dotnet", "TodoApi.dll"]
