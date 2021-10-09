FROM mcr.microsoft.com/dotnet/runtime:5.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["Peeps.Monitoring.Server/Peeps.Monitoring.Server.fsproj", "Peeps.Monitoring.Server/"]
RUN dotnet restore "Peeps.Monitoring.Server/Peeps.Monitoring.Server.fsproj"
COPY . .
WORKDIR "/src/Peeps.Monitoring.Server"
RUN dotnet build "Peeps.Monitoring.Server.fsproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Peeps.Monitoring.Server.fsproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Peeps.Monitoring.Server.dll"]
