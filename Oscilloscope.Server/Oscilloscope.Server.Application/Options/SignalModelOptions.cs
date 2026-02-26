namespace Oscilloscope.Server.Application;

public sealed class SignalModelOptions
{
    public double SampleRateHz { get; set; } = 1000;
    public double FrequencyHz { get; set; } = 5;

    public int PhaseSearchSteps { get; set; } = 360;
    //public double AnomalyThresholdMae { get; set; } = 0.023;
    public double AnomalyThresholdMae { get; set; } = 0.15;
}
