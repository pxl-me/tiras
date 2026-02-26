using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Oscilloscope.Server.Application;
using Oscilloscope.Server.Domain;
using Xunit;

public sealed class SignalAnalyzerTests
{
    private static SignalAnalyzer Create(double sr = 1000, double f = 5)
    {
        var opt = Options.Create(new SignalModelOptions
        {
            SampleRateHz = sr,
            FrequencyHz = f,
            PhaseSearchSteps = 360,
            AnomalyThresholdMae = 0.15
        });
        return new SignalAnalyzer(opt);
    }

    [Fact]
    public void Sine_Perfect_ShouldNotBeAnomaly()
    {
        var sut = Create();
        var samples = BuildLikeEmulator(SignalType.Sine, sampleRateHz: 1000, frequencyHz: 5, count: 100, phaseCycles: 0.33);

        var r = sut.Analyze(SignalType.Sine, samples);
        Assert.False(r.IsAnomaly);
        Assert.True(r.Mae < 0.01);
    }

    [Fact]
    public void CutOff_ShouldBeAnomaly() //??? в умові похибка сигналу +-10%, але порівняння по середній абсолютній 15%
    {
        var sut = Create();
        var samples = new float[100]; // zeros
        var r = sut.Analyze(SignalType.Square, samples);
        Assert.True(r.IsAnomaly);
        Assert.True(r.Mae > 0.15);
    }

    private static float[] BuildLikeEmulator(SignalType type, double sampleRateHz, double frequencyHz, int count, double phaseCycles)
    {
        var arr = new float[count];
        for (int i = 0; i < count; i++)
        {
            double t = i / sampleRateHz;
            double x = frequencyHz * t + phaseCycles;

            arr[i] = type switch
            {
                SignalType.Sine => (float)Math.Sin(2.0 * Math.PI * x),
                SignalType.Square => Math.Sin(2.0 * Math.PI * x) >= 0 ? 1f : -1f,
                //SignalType.Sawtooth => (float)(2.0 * (x - Math.Floor(x + 0.5))),
                SignalType.Sawtooth => (float)(1.0 - 2.0 * Math.Abs(2.0 * (x - Math.Floor(x + 0.5)))),
                _ => 0f
            };
        }
        return arr;
    }
}
