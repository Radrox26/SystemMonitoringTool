using ApplicationLogic.Services;
using Domain.Interfaces;
using Domain.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Plugins.API;
using Plugins.Logger;
using Plugins.Monitor;
using Plugins.Monitors;
using System.Diagnostics;
using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using static System.Net.WebRequestMethods;

// Here we are reading the config file
var config = new ConfigurationBuilder()
   .SetBasePath(Directory.GetCurrentDirectory())
   .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
   .Build();

// Here we are reading the config values
var apiUrl = config["ApiEndpoint"];
var interval = int.Parse(config["MonitoringIntervalSeconds"]);

FileLoggerPlugin _fileLoggerPlugin = new FileLoggerPlugin();
Console.WriteLine("Monitoring started. Press 'Ctrl+C' to exit.");

// Here we are starting the browser
Console.WriteLine("Opening browser. Close it manually when done.");
var htmlFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AutoRefresh.html");
Process.Start(new ProcessStartInfo
{
    FileName = htmlFilePath,
    UseShellExecute = true,
});

// Here we are creating the monitor based on the OS platform
ISystemResourceMonitor monitor;
if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
    monitor = new WindowsSystemResourceMonitor();
}
else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
{
    monitor = new LinuxSystemResourceMonitor();
}
else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
{
    monitor = new MacSystemResourceMonitor();
}
else
{
    await _fileLoggerPlugin.OtherExceptionCollected("Unsupported OS platform");
    throw new PlatformNotSupportedException("Unsupported OS platform");
}

// Here we are creating the plugins
var plugins = new List<IMonitorPlugin>
{
   new ApiPosterPlugin($"{apiUrl}/api/metrics"),
   new FileLoggerPlugin()
};

// Here we are creating the monitoring service
var service = new MonitoringService(monitor, plugins, interval);

// Here we are setting up cancellation
using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (sender, e) =>
{
    e.Cancel = true; 
    cts.Cancel();
    Console.WriteLine("Cancellation requested... Shutting down.");
};

// Here we are hosting the API
var builder = Host.CreateDefaultBuilder(args)
   .ConfigureWebHostDefaults(webBuilder =>
   {
       webBuilder.UseUrls(apiUrl);
       webBuilder.ConfigureServices(services =>
       {
           services.AddRouting();
       });
       webBuilder.Configure(app =>
         {
             app.UseRouting();

             List<SystemMetricsDto> _receivedMetrics = new();

             app.UseEndpoints(endpoints =>
             {
                 endpoints.MapPost("/api/metrics", async context =>
                 {
                     try
                     {
                         var metrics = await JsonSerializer.DeserializeAsync<SystemMetricsDto>(context.Request.Body);
                         if(metrics == null)
                         {
                             context.Response.StatusCode = StatusCodes.Status400BadRequest;
                             return;
                         }
                         _receivedMetrics.Add(metrics);
                         Console.WriteLine($"[API] Received metrics: CPU={metrics.CpuUsage}%, RAM={metrics.RamUsedMb}MB, DISK={metrics.DiskUsedMb}MB");

                         if(metrics.CpuUsage > 80)
                         {
                             using (var client = new HttpClient())
                             {
                                 var slackWebhookUrl = config["slackWebhookUrl"];
                                 var payload = new
                                 {
                                     text = $"High CPU usage detected: {metrics.CpuUsage}%"
                                 };
                                 var json = JsonSerializer.Serialize(payload);
                                 var content = new StringContent(json, Encoding.UTF8, "application/json");

                                 var response = await client.PostAsync(slackWebhookUrl, content);

                                 if (response.IsSuccessStatusCode)
                                 {
                                     Console.WriteLine("Slack message sent successfully.");
                                 }
                                 else
                                 {
                                     await _fileLoggerPlugin.OtherExceptionCollected($"Error sending message: {response.StatusCode}");
                                     Console.WriteLine($"Error sending message: {response.StatusCode}");
                                 }
                             }
                         }

                         context.Response.StatusCode = StatusCodes.Status200OK;
                     }
                     catch (Exception ex)
                     {
                         await _fileLoggerPlugin.OtherExceptionCollected($"Error processing metrics: {ex.Message}");
                         Console.WriteLine($"[API ERROR] {ex.Message}");
                         context.Response.StatusCode = StatusCodes.Status400BadRequest;
                     }
                 });

                 endpoints.MapGet("/api/metrics", async context =>
                 {
                     context.Response.ContentType = "application/json";
                     await JsonSerializer.SerializeAsync(context.Response.Body, _receivedMetrics);
                 });
             });
         });
   });

await builder.StartAsync(cts.Token);
// Here we are starting the monitoring service
await service.StartAsync(cts.Token);



