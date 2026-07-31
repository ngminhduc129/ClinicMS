using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();
builder.WebHost.UseUrls("http://0.0.0.0:5000");

var app = builder.Build();
app.MapHub<DataSyncHub>("/hub");
app.Run();

public class DataSyncHub : Hub
{
    public async Task NotifyDataChanged(string entityType)
    {
        await Clients.Others.SendCoreAsync("OnDataChanged", new object[] { entityType });
    }
}
