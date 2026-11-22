using SteamVROverlayToOSC.Services;

namespace SteamVROverlayToOSC;

public partial class MainPage : ContentPage
{
    private readonly OSCClient _oscClient;
    private readonly BackgroundWorker _worker;

    public MainPage(OSCClient oscClient, BackgroundWorker worker)
    {
        InitializeComponent();
        _oscClient = oscClient;
        _worker = worker;
    }

    private void OnStartClicked(object? sender, EventArgs e)
    {
        if (_worker.IsRunning)
            _worker.Stop();
        else
            _worker.Start();

        StatusLabel.Text = _worker.IsRunning ? "Running" : "Stopped";
    }

    private async void OnSendClicked(object? sender, EventArgs e)
    {
        if (!int.TryParse(PortEntry.Text, out var port))
        {
            StatusLabel.Text = "Invalid port";
            return;
        }

        _oscClient.Configure(HostEntry.Text, port);
        await _oscClient.SendAsync("/steamvr/test", 1);
        StatusLabel.Text = "Test message sent";
    }
}
