FROM mcr.microsoft.com/dotnet/sdk:10.0.403 AS build
WORKDIR /src
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1
COPY global.json Directory.Build.props ./
COPY docker/NuGet.Config ./NuGet.Config
COPY shared/Core/ shared/Core/
COPY game-client/Assets/Scripts/Core/ game-client/Assets/Scripts/Core/
COPY game-client/Assets/Scripts/Network/ game-client/Assets/Scripts/Network/
COPY mock-server/ mock-server/
COPY tests/ClientProbe/ tests/ClientProbe/
COPY docker/HealthProbe/ docker/HealthProbe/
# No network in RUN steps: current projects use only framework packs from the verified SDK image.
RUN --network=none dotnet publish mock-server/MockServer.csproj -c Release --self-contained false -p:UseAppHost=false -o /out/server \
 && dotnet publish tests/ClientProbe/ClientProbe.csproj -c Release --self-contained false -p:UseAppHost=false -o /out/probe \
 && dotnet publish docker/HealthProbe/HealthProbe.csproj -c Release --self-contained false -p:UseAppHost=false -o /out/health

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS server
WORKDIR /app
COPY --from=build /out/server/ ./
COPY --from=build /out/health/ ./health/
ENV DOTNET_EnableDiagnostics=0
USER app
EXPOSE 5077
ENTRYPOINT ["dotnet", "MockServer.dll"]

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS probe
WORKDIR /app
COPY --from=build /out/probe/ ./
ENV DOTNET_EnableDiagnostics=0
USER app
ENTRYPOINT ["dotnet", "ClientProbe.dll"]
