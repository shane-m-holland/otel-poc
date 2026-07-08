FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /repo

COPY OtelPoc.sln ./
COPY src/OtelPoc.Api/OtelPoc.Api.csproj src/OtelPoc.Api/
COPY tests/OtelPoc.Api.Tests/OtelPoc.Api.Tests.csproj tests/OtelPoc.Api.Tests/

RUN dotnet restore

COPY src/ src/
COPY tests/ tests/

RUN dotnet publish src/OtelPoc.Api/OtelPoc.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

ENTRYPOINT ["dotnet", "OtelPoc.Api.dll"]
