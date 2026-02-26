using System.Collections.ObjectModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using Oscilloscope.Client.Application.Models;
using Oscilloscope.Client.Application.Services;
using Oscilloscope.Client.Application.Utils;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System.Windows.Controls;
using System.Diagnostics;

namespace Oscilloscope.Client.Ui.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IAuthService _auth;
    private readonly ISignalHubClient _hub;
    private readonly IExternalAppsService _apps;

    //private readonly CircularBuffer _buffer = new(capacity: 800);
    private const int BufferCapacity = 800;
    private readonly CircularBuffer _buffer = new(capacity: BufferCapacity);

    private readonly DispatcherTimer _uiTimer;
    private readonly Dispatcher _dispatcher;

    private readonly LineSeries<double> _series;

    public ObservableCollection<SignalType> SignalTypes { get; } =
        new(Enum.GetValues<SignalType>());

    public ObservableCollection<ISeries> Series { get; } = new();

    public Axis[] XAxes { get; } = { 
        new Axis() {
            MinLimit = 0,
            MaxLimit = BufferCapacity // - 1
        }
    };

    public Axis[] YAxes { get; } =
    {
        new Axis
        {
            MinLimit = -1.15,
            MaxLimit = 1.15
        }
    };

    [ObservableProperty] private string _email = "";
    [ObservableProperty] private string _password = "";

    [ObservableProperty] private string _authStatus = "Not logged in";
    [ObservableProperty] private string _connectionStatus = "Disconnected";
    [ObservableProperty] private bool _anomalyDetected;

    [ObservableProperty] private string _externalAppsStatus = "";
    [ObservableProperty] private string _errorMessage = "";
    [ObservableProperty] private string _infoMessage = "";

    [ObservableProperty] private EmulatorMode _currentMode = EmulatorMode.Normal;

    [ObservableProperty] private SignalType _selectedSignalType = SignalType.Sine;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanStartEmulator))]
    private bool _isAuthenticated;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanConnect))]
    [NotifyPropertyChangedFor(nameof(CanDisconnect))]
    [NotifyPropertyChangedFor(nameof(CanSendCommands))]
    [NotifyPropertyChangedFor(nameof(CanChangeSignalType))]
    [NotifyPropertyChangedFor(nameof(IsNoiseToggleEnabled))]

    private bool _isConnected;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanStartEmulator))]
    [NotifyPropertyChangedFor(nameof(CanStopEmulator))]
    [NotifyPropertyChangedFor(nameof(CanSendCommands))]
    [NotifyPropertyChangedFor(nameof(CanChangeSignalType))]
    [NotifyPropertyChangedFor(nameof(IsNoiseToggleEnabled))]
    private bool _isEmulatorRunning;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanStartServer))]
    [NotifyPropertyChangedFor(nameof(CanStopServer))]
    private bool _isServerRunning;

    // UI toggles:
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNoiseToggleEnabled))]
    private bool _isNoiseEnabled;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNoiseToggleEnabled))]
    private bool _isCutEnabled;

    // computed (для IsEnabled)
    public bool CanStartEmulator => IsAuthenticated && !IsEmulatorRunning;
    public bool CanStopEmulator => IsEmulatorRunning;

    public bool CanStartServer => !IsServerRunning;
    public bool CanStopServer => IsServerRunning;

    public bool CanConnect => !IsConnected;
    public bool CanDisconnect => IsConnected;

    public bool CanSendCommands => IsConnected && IsEmulatorRunning;
    public bool CanChangeSignalType => CanSendCommands;

    // Noise неактивний під час Cut (тільки UX)
    public bool IsNoiseToggleEnabled => CanSendCommands && !IsCutEnabled;

    public MainViewModel(IAuthService auth, ISignalHubClient hub, IExternalAppsService apps)
    {
        _auth = auth;
        _hub = hub;
        _apps = apps;

        // VM створюється в UI thread
        _dispatcher = Dispatcher.CurrentDispatcher;

        _series = new LineSeries<double>
        {
            Name = "Signal",
            Values = Array.Empty<double>(),
            GeometrySize = 0,
            LineSmoothness = 0,
            Fill = null,
            Stroke = new SolidColorPaint(SKColors.Blue) { StrokeThickness = 0.5f }
        };

        Series.Add(_series);

        //_hub.ConnectionStatusChanged += s => ConnectionStatus = s;
        _hub.ConnectionStatusChanged += s =>
        {
            ConnectionStatus = s;
            IsConnected = string.Equals(s, "Connected", StringComparison.OrdinalIgnoreCase);
        };
        _hub.FrameReceived += OnFrame;
        _hub.EmulatorStatusReceived += s =>
            _dispatcher.Invoke(() => InfoMessage = $"Emulator status: {s}");

        IsAuthenticated = _auth.IsAuthenticated;
        IsServerRunning = _apps.IsServerRunning;
        IsEmulatorRunning = _apps.IsEmulatorRunning;

        _uiTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(22) //50 -> 20fps; 33 -> 30fps
        };
        _uiTimer.Tick += (_, _) => RenderSnapshot();
        _uiTimer.Start();
    }

    //fps
    private long _frames;
    private long _lastFrames;
    private readonly Stopwatch _sw = Stopwatch.StartNew();

    private void OnFrame(SignalFrame f)
    {
        Interlocked.Increment(ref _frames);
        _buffer.PushRange(f.Samples);
        //AnomalyDetected = f.IsAnomaly;
        _dispatcher.Invoke(() => AnomalyDetected = f.IsAnomaly);
    }

    //private void RenderSnapshot()
    //{
    //    var snap = _buffer.Snapshot();
    //    if (snap.Length == 0) return;

    //    // LiveCharts2 loop
    //    var vals = new double[snap.Length];
    //    for (int i = 0; i < snap.Length; i++)
    //        vals[i] = snap[i];

    //    _series.Values = vals;
    //}

    
    private void RenderSnapshot()
    {
        var t0 = _sw.ElapsedMilliseconds;

        ///

        var snap = _buffer.Snapshot();
        if (snap.Length == 0) return;

        // якщо Cut (всі 0) — не вирівнюємо
        bool allZero = true;
        for (int i = 0; i < snap.Length; i++)
        {
            if (snap[i] != 0f) { allZero = false; break; }
        }

        int shift = 0;
        if (!allZero)
        {
            // шукаємо перехід через 0 з мінуса в плюс
            for (int i = 1; i < snap.Length; i++)
            {
                if (snap[i - 1] <= 0f && snap[i] > 0f)
                {
                    shift = i;
                    break;
                }
            }
        }

        // конвертація + rotate
        var vals = new double[snap.Length];
        if (shift == 0)
        {
            for (int i = 0; i < snap.Length; i++) vals[i] = snap[i];
        }
        else
        {
            int n = snap.Length;
            for (int i = 0; i < n; i++)
            {
                int src = (i + shift) % n;
                vals[i] = snap[src];
            }
        }

        ///
        _series.Values = vals;

        var t1 = _sw.ElapsedMilliseconds;
        var dt = t1 - t0;

        if (_sw.ElapsedMilliseconds > 1000)
        {
            var cur = Interlocked.Read(ref _frames);
            var fps = cur - _lastFrames;
            _lastFrames = cur;
            InfoMessage = $"Frames/s: {fps}, renderMs: {dt}";
            _sw.Restart();
        }
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        ErrorMessage = "";
        InfoMessage = "";
        try
        {
            await _auth.LoginAsync(Email, Password, CancellationToken.None);
            AuthStatus = "Logged in";
            IsAuthenticated = _auth.IsAuthenticated;
        }
        catch (Exception ex)
        {
            AuthStatus = "Not logged in";
            IsAuthenticated = _auth.IsAuthenticated;
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task RegisterAsync()
    {
        ErrorMessage = "";
        InfoMessage = "";
        try
        {
            await _auth.RegisterAsync(Email, Password, CancellationToken.None);
            AuthStatus = "Registered + logged in";
            IsAuthenticated = _auth.IsAuthenticated;
        }
        catch (Exception ex)
        {
            IsAuthenticated = _auth.IsAuthenticated;
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task ConnectAsync()
    {
        ErrorMessage = "";
        InfoMessage = "";
        try
        {
            await _hub.ConnectAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task DisconnectAsync()
    {
        ErrorMessage = "";
        InfoMessage = "";
        try
        {
            await _hub.DisconnectAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    partial void OnSelectedSignalTypeChanged(SignalType value)
    {
        _ = SafeSetSignalTypeAsync(value);
    }

    private async Task SafeSetSignalTypeAsync(SignalType type)
    {
        try
        {
            if (!EnsureHubConnected()) return;
            await _hub.SetSignalTypeAsync(type, CancellationToken.None);
            InfoMessage = $"Type set to: {type}";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task StatusAsync()
    {
        ErrorMessage = "";

        var server = _apps.IsServerRunning ? "Running" : "Stopped";
        var emu = _apps.IsEmulatorRunning ? "Running" : "Stopped";
        var hub = _hub.IsConnected ? "Connected" : "Disconnected";
        var auth = IsAuthenticated ? "Yes" : "No";

        InfoMessage = $"Auth: {auth} | Server: {server} | Emulator: {emu} | Hub: {hub} | Type: {SelectedSignalType} | Mode: {CurrentMode}";

        // якщо є з'єднання з SignalR - реальний статус від емулятора
        try
        {
            if (_hub.IsConnected)
                await _hub.RequestStatusAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task NoiseOnAsync() => await SafeSetModeAsync(EmulatorMode.Noise);

    [RelayCommand]
    private async Task NoiseOffAsync() => await SafeSetModeAsync(EmulatorMode.Normal);

    [RelayCommand]
    private async Task CutOnAsync() => await SafeSetModeAsync(EmulatorMode.CutOff);

    [RelayCommand]
    private async Task RestoreAsync() => await SafeSetModeAsync(EmulatorMode.Normal);

    private async Task SafeSetModeAsync(EmulatorMode mode)
    {
        ErrorMessage = "";

        try
        {
            if (!EnsureHubConnected()) return;
            await _hub.SetModeAsync(mode, CancellationToken.None);
            CurrentMode = mode;
            InfoMessage = $"Mode set to: {mode}";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private void Help()
    {
        ErrorMessage = "";
        InfoMessage =
            "Emulator commands:\n" +
            "status\n" +
            "type sine | type square | type saw\n" +
            "noise on | noise off\n" +
            "cut on\n" +
            "restore\n" +
            "help\n" +
            "exit | quit";
    }

    [RelayCommand]
    private void ExitEmulator()
    {
        StopEmulator();
    }

    private bool EnsureHubConnected()
    {
        if (_hub.IsConnected) return true;
        ErrorMessage = "Немає підключення до SignalR. Натисніть Connect.";
        return false;
    }

    [RelayCommand]
    private void StartServer()
    {
        ErrorMessage = "";
        InfoMessage = "";
        try
        {
            _apps.StartServer();
            IsServerRunning = _apps.IsServerRunning;
            ExternalAppsStatus = "Server started";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private void StopServer()
    {
        ErrorMessage = "";
        InfoMessage = "";
        try
        {
            _apps.StopServer();
            IsServerRunning = _apps.IsServerRunning;
            ExternalAppsStatus = "Server stopped";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private void StartEmulator()
    {
        ErrorMessage = "";
        InfoMessage = "";

        if (!IsAuthenticated || string.IsNullOrWhiteSpace(_auth.AccessToken))
        {
            ErrorMessage = "Login або Register спочатку (потрібен JWT).";
            return;
        }

        try
        {
            _apps.StartEmulator(_auth.AccessToken);
            IsEmulatorRunning = _apps.IsEmulatorRunning;
            ExternalAppsStatus = "Emulator started";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private void StopEmulator()
    {
        ErrorMessage = "";
        InfoMessage = "";

        try
        {
            _apps.StopEmulator();
            IsEmulatorRunning = _apps.IsEmulatorRunning;
            ExternalAppsStatus = "Emulator stopped";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    // helper
    partial void OnIsNoiseEnabledChanged(bool value)
    {
        _ = ApplyNoiseAsync(value);
    }

    private async Task ApplyNoiseAsync(bool enabled)
    {
        if (!CanSendCommands) return;// UI все одно буде disabled
        if (IsCutEnabled) return; // при Cut шум не змінюєтсья

        try
        {
            await _hub.SetModeAsync(enabled ? EmulatorMode.Noise : EmulatorMode.Normal, CancellationToken.None);
            CurrentMode = enabled ? EmulatorMode.Noise : EmulatorMode.Normal;
            InfoMessage = enabled ? "Noise: enabled" : "Noise: disabled";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    partial void OnIsCutEnabledChanged(bool value)
    {
        _ = ApplyCutAsync(value);
    }

    private async Task ApplyCutAsync(bool enabled)
    {
        if (!CanSendCommands) return;

        try
        {
            if (enabled)
            {
                await _hub.SetModeAsync(EmulatorMode.CutOff, CancellationToken.None);  // "cut on"
                CurrentMode = EmulatorMode.CutOff;
                InfoMessage = "Signal status: none";
            }
            else
            {
                await _hub.SetModeAsync(EmulatorMode.Normal, CancellationToken.None); // "restore"
                CurrentMode = EmulatorMode.Normal;
                InfoMessage = "Signal status: restored";

                // якщо користувач мав Noise on — після restore повертаємо
                if (IsNoiseEnabled)
                {
                    await _hub.SetModeAsync(EmulatorMode.Noise, CancellationToken.None);
                    CurrentMode = EmulatorMode.Noise;
                }
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }
}
