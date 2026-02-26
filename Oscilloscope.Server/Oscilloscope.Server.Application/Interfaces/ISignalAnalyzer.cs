using Oscilloscope.Server.Domain;

namespace Oscilloscope.Server.Application;

public interface ISignalAnalyzer
{
    AnalysisResult Analyze(SignalType type, ReadOnlySpan<float> samples);
}
