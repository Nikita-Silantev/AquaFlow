using static TorchSharp.torch;

namespace AquaFlow.Ml;

/// <summary>
/// MLP на TorchSharp (ТЗ, раздел 6.2): Linear(10→32)→ReLU→Linear(32→16)→ReLU→Linear(16→3)→Sigmoid.
/// Выход — три независимые вероятности (multi-label), а не softmax-распределение.
/// </summary>
public sealed class WaterMlpModel : nn.Module<Tensor, Tensor>
{
    private readonly nn.Module<Tensor, Tensor> _fc1;
    private readonly nn.Module<Tensor, Tensor> _fc2;
    private readonly nn.Module<Tensor, Tensor> _fc3;

    public WaterMlpModel() : base(nameof(WaterMlpModel))
    {
        _fc1 = nn.Linear(FeatureEncoder.FeatureCount, 32);
        _fc2 = nn.Linear(32, 16);
        _fc3 = nn.Linear(16, FeatureEncoder.LabelCount);

        RegisterComponents();
    }

    public override Tensor forward(Tensor input)
    {
        var x = nn.functional.relu(_fc1.forward(input));
        x = nn.functional.relu(_fc2.forward(x));
        x = sigmoid(_fc3.forward(x));
        return x;
    }
}
