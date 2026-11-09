# syntax=docker/dockerfile:1

# ---------- build stage: full SDK, compiles and publishes the app ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy only the project file first so the (slow) restore layer is cached until dependencies change.
COPY RaceDay.Web.csproj ./
RUN dotnet restore RaceDay.Web.csproj

COPY . ./
RUN dotnet publish RaceDay.Web.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ---------- runtime stage: small ASP.NET image, no SDK ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Folder for data-protection keys (mount a volume here to keep sessions across restarts).
RUN mkdir -p /app/keys && chown -R $APP_UID:$APP_UID /app/keys

COPY --from=build /app/publish ./

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DataProtection__KeysPath=/app/keys \
    Api__BaseUrl=http://host.docker.internal:5100/

EXPOSE 8080

# Never run as root.
USER $APP_UID

ENTRYPOINT ["dotnet", "RaceDay.Web.dll"]
