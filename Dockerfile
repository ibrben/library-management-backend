FROM mcr.microsoft.com/dotnet/sdk:8.0 AS restore
WORKDIR /src
COPY Directory.Build.props global.json LibraryManagement.sln ./
COPY src/LibraryManagement.Api/LibraryManagement.Api.csproj src/LibraryManagement.Api/
COPY src/LibraryManagement.Business/LibraryManagement.Business.csproj src/LibraryManagement.Business/
COPY src/LibraryManagement.DataAccess/LibraryManagement.DataAccess.csproj src/LibraryManagement.DataAccess/
COPY tests/LibraryManagement.Business.UnitTests/LibraryManagement.Business.UnitTests.csproj tests/LibraryManagement.Business.UnitTests/
RUN dotnet restore LibraryManagement.sln

FROM restore AS build
COPY . .
RUN dotnet publish src/LibraryManagement.Api/LibraryManagement.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
RUN apt-get update \
    && apt-get install --yes --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build --chown=app:app /app/publish .
USER app
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080
ENTRYPOINT ["dotnet", "LibraryManagement.Api.dll"]
