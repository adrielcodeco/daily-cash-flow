#!/usr/bin/env bash
# Runs all tests in the .NET 10 SDK container. Docker is the only requirement.
set -euo pipefail

docker run --rm \
  -v "$PWD":/repo \
  -v dotnet-nuget:/root/.nuget/packages \
  -v /var/run/docker.sock:/var/run/docker.sock \
  --add-host=host.docker.internal:host-gateway \
  -e TESTCONTAINERS_HOST_OVERRIDE=host.docker.internal \
  -w /repo \
  mcr.microsoft.com/dotnet/sdk:10.0 \
  dotnet test DailyCashFlow.slnx "$@"
