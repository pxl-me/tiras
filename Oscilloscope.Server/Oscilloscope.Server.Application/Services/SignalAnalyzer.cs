using Microsoft.Extensions.Options;
using Oscilloscope.Server.Domain;

namespace Oscilloscope.Server.Application;

public sealed class SignalAnalyzer : ISignalAnalyzer
{
    private readonly SignalModelOptions _opt;

    public SignalAnalyzer(IOptions<SignalModelOptions> opt) => _opt = opt.Value;

    public AnalysisResult Analyze(SignalType type, ReadOnlySpan<float> samples)
    {
        if (samples.Length == 0)
            return new AnalysisResult(0, false);

        // Фаза у "циклах" 0..1, щоб мінімізувати MAE.
        // Це робить аналіз стабільним для стріму емулатора, бо дані з пакету можуть бути з будь-якої фази.
        double bestMae = double.MaxValue;

        int n = samples.Length;
        double sr = _opt.SampleRateHz;
        double f = _opt.FrequencyHz;

        int steps = Math.Max(8, _opt.PhaseSearchSteps);

        for (int s = 0; s < steps; s++)
        {
            double phaseCycles = (double)s / steps; // 0..1
            double sumAbs = 0;

            for (int i = 0; i < n; i++)
            {
                double t = i / sr; // секунди від початку пакета
                double x = f * t + phaseCycles; // к-ість "циклів"

                double expected = type switch
                {
                    SignalType.Sine => Math.Sin(2.0 * Math.PI * x),
                    SignalType.Square => Math.Sin(2.0 * Math.PI * x) >= 0 ? 1f : -1f,

                    // як в емулаторі: v = /*2**/(x - floor(x + 0.5))
                    //SignalType.Sawtooth => 2.0 * (x - Math.Floor(x + 0.5)),
                    SignalType.Sawtooth => 1.0 - 2.0 * Math.Abs(2.0 * (x - Math.Floor(x + 0.5))),

                    _ => 0.0
                };

                sumAbs += Math.Abs(samples[i] - expected);
            }

            double mae = sumAbs / n;
            if (mae < bestMae) bestMae = mae;
        }

        bool anomaly = bestMae > _opt.AnomalyThresholdMae;
        return new AnalysisResult(bestMae, anomaly);
    }
}
