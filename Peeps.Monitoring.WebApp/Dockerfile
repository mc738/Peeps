﻿FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["Peeps.Monitoring.WebApp/Peeps.Monitoring.WebApp.csproj", "Peeps.Monitoring.WebApp/"]
RUN dotnet restore "Peeps.Monitoring.WebApp/Peeps.Monitoring.WebApp.csproj"
COPY . .
WORKDIR "/src/Peeps.Monitoring.WebApp"
RUN dotnet build "Peeps.Monitoring.WebApp.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Peeps.Monitoring.WebApp.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Peeps.Monitoring.WebApp.dll"]
