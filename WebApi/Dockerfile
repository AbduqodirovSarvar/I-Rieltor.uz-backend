# См. статью по ссылке https://aka.ms/customizecontainer, чтобы узнать как настроить контейнер отладки и как Visual Studio использует этот Dockerfile для создания образов для ускорения отладки.

# Этот этап используется при запуске из VS в быстром режиме (по умолчанию для конфигурации отладки)
# Pinned by digest (see docker-compose.yml for how to update).
FROM mcr.microsoft.com/dotnet/aspnet:10.0@sha256:d5633778c754e89a386882eddf8dbee6942447a8a4b8267c19cf1e1158d0b0ac AS base
# Uploaded files live outside the app folder, owned by the non-root app user, so a new image never touches them.
# Mount a volume here (docker run -v irieltor-files:/data/files ...) so files survive a new container.
RUN mkdir -p /data/files && chown $APP_UID:$APP_UID /data/files
ENV FileStorage__RootPath=/data/files
VOLUME /data/files
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# Этот этап используется для сборки проекта службы
FROM mcr.microsoft.com/dotnet/sdk:10.0@sha256:0d99ebf1ccad15037b5c98c9fe8979d6b8dbac23812feae2277a0b277b5c5ffa AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
# Every project of the solution must be present for restore to resolve the project references.
# The lock files fix every package version (also transitive ones); --locked-mode fails instead of silently changing one.
COPY ["Directory.Build.props", "./"]
COPY ["WebApi/WebApi.csproj", "WebApi/packages.lock.json", "WebApi/"]
COPY ["Application/Application.csproj", "Application/packages.lock.json", "Application/"]
COPY ["Persistence/Persistence.csproj", "Persistence/packages.lock.json", "Persistence/"]
COPY ["Infrastructure/Infrastructure.csproj", "Infrastructure/packages.lock.json", "Infrastructure/"]
COPY ["Domain/Domain.csproj", "Domain/packages.lock.json", "Domain/"]
RUN dotnet restore "./WebApi/WebApi.csproj" --locked-mode
COPY . .
WORKDIR "/src/WebApi"
RUN dotnet build "./WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Этот этап используется для публикации проекта службы, который будет скопирован на последний этап
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Этот этап используется в рабочей среде или при запуске из VS в обычном режиме (по умолчанию, когда конфигурация отладки не используется)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WebApi.dll"]