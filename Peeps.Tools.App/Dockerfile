FROM mcr.microsoft.com/dotnet/runtime:5.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["Peeps.Tools.App/Peeps.Tools.App.fsproj", "Peeps.Tools.App/"]
RUN dotnet restore "Peeps.Tools.App/Peeps.Tools.App.fsproj"
COPY . .
WORKDIR "/src/Peeps.Tools.App"
RUN dotnet build "Peeps.Tools.App.fsproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Peeps.Tools.App.fsproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Peeps.Tools.App.dll"]
