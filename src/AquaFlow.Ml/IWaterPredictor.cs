using AquaFlow.Core;

namespace AquaFlow.Ml;

/// <summary>
/// Нейросетевой предиктор потока: за миллисекунду предсказывает результат
/// без запуска симуляции (ТЗ, раздел 3.1). Реализация скрыта за интерфейсом,
/// чтобы её можно было заменить (например, на ONNX в M7).
/// </summary>
public interface IWaterPredictor
{
    Prediction Predict(SimConfig config);
}
