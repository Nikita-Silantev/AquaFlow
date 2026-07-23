-- Начальная схема БД AquaFlow (ТЗ, раздел 7).
-- Схема фиксируется на M3: после M3 изменения — только по согласованию (ТЗ, раздел 12.6).

CREATE TABLE IF NOT EXISTS samples (
    id                BIGSERIAL PRIMARY KEY,
    source            TEXT NOT NULL,
    valves            JSONB NOT NULL,
    reached_receivers TEXT[] NOT NULL
);

CREATE TABLE IF NOT EXISTS runs (
    id                  BIGSERIAL PRIMARY KEY,
    created_at          TIMESTAMPTZ NOT NULL DEFAULT now(),
    mode                TEXT NOT NULL CHECK (mode IN ('real', 'predict')),
    source              TEXT NOT NULL,
    valves              JSONB NOT NULL,
    predicted_receivers TEXT[],
    predicted_probs     JSONB,
    actual_receivers    TEXT[],
    was_correct         BOOLEAN
);

CREATE TABLE IF NOT EXISTS model_meta (
    id               BIGSERIAL PRIMARY KEY,
    trained_at       TIMESTAMPTZ NOT NULL DEFAULT now(),
    epochs           INTEGER NOT NULL,
    subset_accuracy  DOUBLE PRECISION NOT NULL,
    file_path        TEXT NOT NULL
);
