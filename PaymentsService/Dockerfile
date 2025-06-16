# ---- build stage ----
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src

# Копируем csproj и Shared
COPY Shared/Shared.csproj Shared/
COPY PaymentsService/PaymentsService.csproj PaymentsService/
RUN dotnet restore PaymentsService/PaymentsService.csproj

# Устанавливаем dotnet-ef как глобальный инструмент внутри билд-контейнера
RUN dotnet tool install --global dotnet-ef --version 7.*

# Копируем остальное и публикуем
COPY . .
WORKDIR /src/PaymentsService
RUN dotnet publish -c Release -o /app

# ---- runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS runtime
WORKDIR /app

# Переносим глобальные инструменты из билд-контейнера и добавляем в PATH
COPY --from=build /root/.dotnet/tools /root/.dotnet/tools
ENV PATH="${PATH}:/root/.dotnet/tools"

# Копируем опубликованный код
COPY --from=build /app ./

ENTRYPOINT ["dotnet", "PaymentsService.dll"]
