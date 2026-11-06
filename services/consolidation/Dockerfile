# Builds one host project of this service. PROJECT is the path relative to the repository root.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG PROJECT
WORKDIR /repo
COPY Directory.Build.props Directory.Packages.props ./
COPY services ./services
RUN dotnet publish "$PROJECT" -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
ARG ENTRYPOINT_DLL
ENV ENTRYPOINT_DLL=$ENTRYPOINT_DLL
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["sh", "-c", "exec dotnet \"$ENTRYPOINT_DLL\" \"$@\"", "--"]
