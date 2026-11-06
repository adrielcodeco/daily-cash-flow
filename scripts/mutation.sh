#!/usr/bin/env bash
# Runs mutation tests on the domain projects, in the .NET 10 SDK container.
# Integration tests are excluded: each mutant would otherwise start a database.
set -euo pipefail

for service in ledger:Ledger consolidation:Consolidation; do
  dir=${service%:*}
  name=${service#*:}
  docker run --rm \
    -v "$PWD":/repo \
    -v dotnet-nuget:/root/.nuget/packages \
    -w "/repo/services/$dir/tests/$name.Tests" \
    mcr.microsoft.com/dotnet/sdk:10.0 \
    sh -c 'dotnet tool restore --tool-manifest /repo/.config/dotnet-tools.json && dotnet tool run dotnet-stryker --output /repo/reports/mutation/'"$dir"
done
