using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace ClinicMS.Services;

public class SignalRClientService
{
    private HubConnection? _connection;
    private readonly string _hubUrl;
    private CancellationTokenSource? _reconnectCts;

    public event Action<string>? OnDataChanged;

    public bool IsConnected =>
        _connection?.State == HubConnectionState.Connected;

    public SignalRClientService(IConfiguration config)
    {
        _hubUrl = config["SignalRServer"]
            ?? throw new InvalidOperationException("SignalRServer URL not configured");
    }

    public async Task ConnectAsync()
    {
        try
        {
            _reconnectCts = new CancellationTokenSource();

            _connection = new HubConnectionBuilder()
                .WithUrl(_hubUrl)
                .WithAutomaticReconnect(new ForeverRetryPolicy())
                .Build();

            _connection.On<string>("OnDataChanged", entityType =>
            {
                OnDataChanged?.Invoke(entityType);
            });

            _connection.Reconnecting += ex =>
            {
                Log.Warning(ex, "SignalR reconnecting...");
                return Task.CompletedTask;
            };

            _connection.Reconnected += async id =>
            {
                Log.Information("SignalR reconnected: {Id}", id);
                await Task.CompletedTask;
            };

            _connection.Closed += async ex =>
            {
                Log.Warning(ex, "SignalR closed — reconnecting in 5s...");
                try
                {
                    await Task.Delay(5000, _reconnectCts.Token);
                    await ConnectAsync();
                }
                catch (TaskCanceledException) { }
            };

            await _connection.StartAsync();
            Log.Information("SignalR connected to {Url}", _hubUrl);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "SignalR connection failed — retry in 5s...");
            try
            {
                await Task.Delay(5000, _reconnectCts?.Token ?? CancellationToken.None);
                await ConnectAsync();
            }
            catch (TaskCanceledException) { }
        }
    }

    public async Task NotifyDataChangedAsync(string entityType)
    {
        try
        {
            if (_connection?.State != HubConnectionState.Connected) return;
            await _connection.InvokeAsync("NotifyDataChanged", entityType);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "SignalR notify failed (offline?)");
        }
    }

    public async Task DisconnectAsync()
    {
        try
        {
            _reconnectCts?.Cancel();
            if (_connection != null)
                await _connection.DisposeAsync();
        }
        catch { }
    }
}

public class ForeverRetryPolicy : IRetryPolicy
{
    public TimeSpan? NextRetryDelay(RetryContext retryContext)
    {
        var delay = Math.Min(5 * Math.Pow(2, retryContext.PreviousRetryCount), 30);
        return TimeSpan.FromSeconds(delay);
    }
}
