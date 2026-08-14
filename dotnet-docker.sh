#!/usr/bin/env bash
# Runs `dotnet` inside the official SDK container so no local .NET install
# is needed. Always run this from the csharp-assignment/ directory.
set -euo pipefail

docker run --rm \
  -v "$(pwd):/src" \
  -w /src \
  -v csharp-assignment-nuget-cache:/root/.nuget/packages \
  mcr.microsoft.com/dotnet/sdk:10.0 \
  dotnet "$@"
