FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /

COPY "Directory.Build.props" "Directory.Packages.props" "./"
COPY "src/Notification.Api/Notification.Api.csproj" "src/Notification.Api/"

RUN dotnet restore "src/Notification.Api/Notification.Api.csproj"

COPY "src/" "src/"

RUN dotnet publish "src/Notification.Api/Notification.Api.csproj" -c Release -o /app --no-restore

FROM base AS final
WORKDIR /app
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true
COPY --from=build /app .
ENTRYPOINT ["dotnet", "Notification.Api.dll"]
