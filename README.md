# AquaFlow

Демо-проект: симулятор трубопроводной сети со SCADA-логикой + нейросетевой предиктор потока.
Полное ТЗ — см. `ТЗ_AquaFlow.md`.

## Структура

```
AquaFlow.sln
├── src/AquaFlow.Core   — доменная модель + детерминированный симулятор (M1)
├── src/AquaFlow.Ml     — датасет, обучение MLP, IWaterPredictor (M3, M4)
├── src/AquaFlow.App    — Avalonia UI: Симуляция / Метрики / История (M2, M5, M6)
├── tests/AquaFlow.Core.Tests — xUnit-тесты симулятора
├── db/migrations       — SQL-миграции Postgres (появятся в M3)
└── models/             — файл весов обученной модели (water_mlp.bin)
```

Статус: **M2 — канвас и реальный прогон готовы** (вкладка «Симуляция»: выбор входа,
переключение клапанов мышью, кнопка «Реальный прогон» с анимацией и окном итога).
Нейросеть, датасет и БД ещё не реализованы — по майлстоунам M3–M7 (см. ТЗ, раздел 9).

## Требования

- .NET 10 SDK (arm64, Apple Silicon)
- Docker Desktop (для Postgres 16)

## Команды

```bash
# Восстановить зависимости
dotnet restore

# Собрать решение
dotnet build

# Поднять Postgres
docker compose up -d

# Прогнать тесты
dotnet test

# Запустить приложение
dotnet run --project src/AquaFlow.App
```

Строка подключения к БД берётся из переменной окружения / appsettings,
не хардкодится (появится начиная с M3).
