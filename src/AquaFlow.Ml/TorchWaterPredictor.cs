using AquaFlow.Core;
using static TorchSharp.torch;

namespace AquaFlow.Ml;

/// <summary>
/// Реализация <see cref="IWaterPredictor"/> поверх обученного <see cref="WaterMlpModel"/>.
/// Модель загружается один раз в конструкторе (in-process, без сети/gRPC — ТЗ, раздел 3.1),
/// каждый вызов <see cref="Predict"/> — синхронный и мгновенный.
/// </summary>
public sealed class TorchWaterPredictor : IWaterPredictor
{
    private readonly WaterMlpModel _model;

    public TorchWaterPredictor(string modelFilePath)
    {
        if (!File.Exists(modelFilePath))
        {
            throw new FileNotFoundException(
                "Файл весов модели не найден. Сначала выполните обучение (M4).", modelFilePath);
        }

        _model = new WaterMlpModel();
        _model.load(modelFilePath);
        _model.eval();
    }

    public Prediction Predict(SimConfig config)
    {
        using var _ = no_grad();

        var features = FeatureEncoder.EncodeFeatures(config.Source, config.Valves);
        var input = tensor(features).reshape(1, FeatureEncoder.FeatureCount);
        var output = _model.forward(input).reshape(FeatureEncoder.LabelCount);
        var probs = output.data<float>().ToArray();

        var receivers = FeatureEncoder.ReceiversInOrder;
        var probabilities = new Dictionary<Receiver, float>();
        var predicted = new HashSet<Receiver>();

        for (var i = 0; i < receivers.Count; i++)
        {
            probabilities[receivers[i]] = probs[i];
            if (probs[i] >= 0.5f)
            {
                predicted.Add(receivers[i]);
            }
        }

        return new Prediction(probabilities, predicted);
    }
}
