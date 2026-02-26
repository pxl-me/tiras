namespace Oscilloscope.Client.Application.Utils;

// буфер для апдейтів ui (tocheck)
// Thread-safe (single lock).
public sealed class CircularBuffer
{
    private readonly float[] _data;
    private int _pos;
    private bool _filled;

    //для послідновності "доступу" PushRange та Snapshot
    private readonly object _gate = new();

    public CircularBuffer(int capacity)
    {
        if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        _data = new float[capacity];
    }

    public void PushRange(ReadOnlySpan<float> samples)
    {
        lock (_gate)
        {
            for (int i = 0; i < samples.Length; i++)
            {
                _data[_pos++] = samples[i];
                if (_pos == _data.Length)
                {
                    _pos = 0;
                    _filled = true;
                }
            }
        }
    }

    //public float[] Snapshot()
    //{
    //    lock (_gate)
    //    {
    //        if (!_filled)
    //        {
    //            var copy = new float[_pos];
    //            Array.Copy(_data, 0, copy, 0, _pos);
    //            return copy;
    //        }

    //        var result = new float[_data.Length];
    //        Array.Copy(_data, _pos, result, 0, _data.Length - _pos);
    //        Array.Copy(_data, 0, result, _data.Length - _pos, _pos);
    //        return result;
    //    }
    //}

    public float[] Snapshot()
    {
        lock (_gate)
        {
            var result = new float[_data.Length];

            if (!_filled)
            {
                // Дані ще не заповнили буфер — кладемо їх в кінець, решта 0
                // _data[1,2,3,0,0] -> result: [0,0,1,2,3]
                Array.Copy(_data, 0, result, _data.Length - _pos, _pos);
                return result;
            }

            // Буфер заповнений — робимо "розгортку" по колу
            // _data=[4,5,1,2,3] -> _pos = 2 -> result[1,2,3,4,5]
            Array.Copy(_data, _pos, result, 0, _data.Length - _pos);
            Array.Copy(_data, 0, result, _data.Length - _pos, _pos);
            return result;
        }
    }
}
