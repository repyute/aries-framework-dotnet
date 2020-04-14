FROM streetcred/dotnet-indy:1.14.2 AS base
WORKDIR /app

FROM streetcred/dotnet-indy:1.14.2 AS build
WORKDIR /src
COPY [".", "."]

WORKDIR /src/samples/routing/EdgeConsoleClient
RUN dotnet restore "Edge.Console.csproj" \
    -s "https://api.nuget.org/v3/index.json"

COPY ["samples/routing/EdgeConsoleClient/", "."]
# COPY ["docker/docker_pool_genesis.txn", "./pool_genesis.txn"]
RUN curl http://indy.ledger.repyute.com:9000/genesis > pool_genesis.txn
RUN dotnet build "Edge.Console.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "Edge.Console.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .

ENTRYPOINT ["dotnet", "Edge.Console.dll"]