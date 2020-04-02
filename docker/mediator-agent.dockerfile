FROM streetcred/dotnet-indy:1.14.2 AS base
WORKDIR /app

FROM streetcred/dotnet-indy:1.14.2 AS build
WORKDIR /src
COPY [".", "."]

WORKDIR /src/samples/routing/MediatorAgentService
RUN dotnet restore "Mediator.Web.csproj" \
    -s "https://api.nuget.org/v3/index.json"

COPY ["samples/routing/MediatorAgentService/", "."]
COPY ["docker/docker_pool_genesis.txn", "./pool_genesis.txn"]
RUN dotnet build "Mediator.Web.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "Mediator.Web.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .

ENTRYPOINT ["dotnet", "Mediator.Web.dll"]