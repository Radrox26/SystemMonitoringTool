using Domain.Interfaces;
using Domain.Models;
using Plugins.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Plugins.API
{
    public class ApiPosterPlugin : IMonitorPlugin
    {
        private readonly HttpClient _httpClient;
        private readonly string _endpointUrl;
        private FileLoggerPlugin _fileLoggerPlugin;

        public ApiPosterPlugin(string endpointUrl)
        {
            _httpClient = new HttpClient();
            _endpointUrl = endpointUrl;
            _fileLoggerPlugin = new FileLoggerPlugin();
        }

        public async Task OnMetricsCollected(SystemMetricsDto metrics)
        {
            try
            {
                var content = new StringContent(
                    JsonSerializer.Serialize(metrics),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await _httpClient.PostAsync(_endpointUrl, content);

                if (!response.IsSuccessStatusCode)
                {
                    Console.Error.WriteLine($"[API Error] Failed to POST metrics. Status: {response.StatusCode}");
                    await _fileLoggerPlugin.OtherExceptionCollected($"Failed to POST metrics. Status: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API POST ERROR] {ex.Message}");
                await _fileLoggerPlugin.OtherExceptionCollected($"[API POST ERROR] {ex.Message}");
            }
        }
    }
}
