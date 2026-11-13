# Build context is the repository root so shared projects are available.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY . .
RUN dotnet restore src/ECommerce.Gateway/ECommerce.Gateway.csproj
RUN dotnet publish src/ECommerce.Gateway/ECommerce.Gateway.csproj \
    -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true

COPY --from=build /app/publish .
USER $APP_UID

EXPOSE 8080
ENTRYPOINT ["dotnet", "ECommerce.Gateway.dll"]
