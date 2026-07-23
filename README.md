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

Статус: **M3 — датасет и Postgres готовы** (миграции схемы, генерация полного датасета
из 384 конфигураций, воспроизводимое train/test-разбиение, запись в таблицу `samples`).
Нейросеть ещё не реализована — по майлстоунам M4–M7 (см. ТЗ, раздел 9).

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

# Запустить приложение (GUI)
dotnet run --project src/AquaFlow.App

# Сгенерировать датасет: применяет миграции, генерирует 384 конфигурации,
# перезаписывает таблицу samples. GUI не открывается.
dotnet run --project src/AquaFlow.App -- --generate-dataset
```

## База данных

Строка подключения берётся из переменной окружения `AQUAFLOW_CONNECTION_STRING`,
не хардкодится. Если переменная не задана — используется значение по умолчанию,
совпадающее с учётными данными из `docker-compose.yml` (удобно для локальной разработки):

```
Host=localhost;Port=5433;Database=aquaflow;Username=aquaflow;Password=aquaflow
```

Порт 5433 снаружи выбран специально, чтобы не конфликтовать с локальным Postgres,
который может уже слушать стандартный 5432 (Postgres.app, Homebrew и т.п.).

Схема (`db/migrations/0001_init_schema.sql`) применяется автоматически командой
`--generate-dataset`. Схема фиксируется на M3 — дальнейшие изменения только по согласованию.
