using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Oscilloscope.Emulator.signal
{
    using Oscilloscope.Emulator.models;

    public sealed class SignalGenerator
    {
        private readonly double _sampleRateHz;
        private readonly double _frequencyHz;

        private long _sampleIndex = 0;

        public SignalGenerator(double sampleRateHz, double frequencyHz)
        {
            _sampleRateHz = sampleRateHz;
            _frequencyHz = frequencyHz;
        }

        public void Generate(SignalType type, Span<float> buffer)
        {
            // без шуму/"обрізання"
            for (int i = 0; i < buffer.Length; i++)
            {
                var t = (_sampleIndex++) / _sampleRateHz;
                buffer[i] = type switch
                {
                    SignalType.Sine => (float)Math.Sin(2 * Math.PI * _frequencyHz * t),

                    SignalType.Square => Math.Sin(2 * Math.PI * _frequencyHz * t) >= 0 ? 1f : -1f,

                    SignalType.Sawtooth => Sawtooth2(t, _frequencyHz),

                    _ => 0f
                };
            }
        }

        private static float Sawtooth(double t, double f)
        {
            // нормалізований sawtooth в діапазоні [-0.5; 0.5]
            // y = 2(x - floor(x + 0.5))
            var x = t * f;
            var v = /*2.0 **/ (x - Math.Floor(x + 0.5));
            return (float)v;
        }

        //"трикутна"
        private static float Sawtooth2(double t, double f)
        {
            var x = t * f;
            var s = 2.0 * (x - Math.Floor(x + 0.5));  // [-1..1]
            var tri = 1.0 - 2.0 * Math.Abs(s);        // [-1..1]
            return (float)tri;
        }
    }
}
