using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Oscilloscope.Emulator.models;

namespace Oscilloscope.Emulator.runtime;

public sealed class EmulatorState
{
    private readonly object _lock = new();

    private SignalType _signalType = SignalType.Sine;
    private EmulatorMode _mode = EmulatorMode.Normal;

    public (SignalType SignalType, EmulatorMode Mode) Snapshot()
    {
        lock (_lock)
            return (_signalType, _mode);
    }

    public void SetSignalType(SignalType type)
    {
        lock (_lock)
            _signalType = type;
    }

    public void SetMode(EmulatorMode mode)
    {
        lock (_lock)
            _mode = mode;
    }
}
