# Используем официальный образ .NET SDK для сборки
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /app

# Копируем файлы проекта и восстанавливаем зависимости
COPY *.csproj ./
RUN dotnet restore

# Копируем остальной код и собираем приложение
COPY . ./
RUN dotnet publish -c Release -o out

# Создаём финальный образ для запуска
FROM mcr.microsoft.com/dotnet/runtime:6.0
WORKDIR /app
COPY --from=build /app/out .
COPY Xeno.dll .

# Указываем порт, который будет использоваться
ENV PORT=10000
EXPOSE 10000

# Команда для запуска приложения
ENTRYPOINT ["dotnet", "GlitcherServer.dll"]
