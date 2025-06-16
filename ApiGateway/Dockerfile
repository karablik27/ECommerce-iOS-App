# ---- build stage ----
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src

# 1) Восстанавливаем зависимости
COPY ["ApiGateway/ApiGateway.csproj", "ApiGateway/"]
RUN dotnet restore "ApiGateway/ApiGateway.csproj"

# 2) Копируем исходники
COPY ApiGateway/. ApiGateway/

# 3) Копируем ocelot.json из корня репозитория
COPY ["ocelot.json", "ApiGateway/ocelot.json"]

# 4) Публикуем
WORKDIR "/src/ApiGateway"
RUN dotnet publish -c Release -o /app/publish

# ---- runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "ApiGateway.dll"]
