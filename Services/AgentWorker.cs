using System.Text.Json;
using System.Net.Http.Headers;
using CORE_BE.Models;
using System.Text.Json.Serialization;

namespace CORE_BE.Services
{
    public class AgentWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AgentWorker> _logger;
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _httpFactory;

        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

        public AgentWorker(IServiceProvider serviceProvider, ILogger<AgentWorker> logger, IConfiguration config, IHttpClientFactory httpFactory)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _config = config;
            _httpFactory = httpFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var mode = _config["DistributedSettings:Mode"];
            if (mode != "Agent")
            {
                _logger.LogInformation("AgentWorker is disabled (Mode: {Mode})", mode);
                return;
            }

            var interval = _config.GetValue<int>("DistributedSettings:IntervalMinutes", 5);
            _logger.LogInformation("AgentWorker started in Agent Mode for Unit: {UnitCode}", _config["DistributedSettings:UnitCode"]);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessAgentWork(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "AgentWorker error");
                }

                await Task.Delay(TimeSpan.FromMinutes(interval), stoppingToken);
            }
        }

        private async Task ProcessAgentWork(CancellationToken token)
        {
            var centerUrl = _config["DistributedSettings:CenterApiUrl"];
            var unitCode = _config["DistributedSettings:UnitCode"];

            var centerClient = _httpFactory.CreateClient();
            centerClient.BaseAddress = new Uri(centerUrl);
            var apiKey = _config["DistributedSettings:ApiKey"];
            if (!string.IsNullOrEmpty(apiKey))
            {
                centerClient.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
            }

            _logger.LogInformation("Fetching server list from Center for unit: {UnitCode}", unitCode);
            var response = await centerClient.GetAsync($"/api/agent/servers?unitCode={unitCode}", token);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to fetch servers from center: {Status}", response.StatusCode);
                return;
            }

            var json = await response.Content.ReadAsStringAsync(token);
            var servers = JsonSerializer.Deserialize<List<Server>>(json, JsonOptions);

            if (servers == null || !servers.Any())
            {
                _logger.LogWarning("No active servers found for unit {UnitCode}", unitCode);
                return;
            }

            // 2. Poll từng máy chủ và đẩy kết quả về Trung tâm
            var idracClient = _httpFactory.CreateClient("idrac");
            
            foreach (var server in servers)
            {
                using var scope = _serviceProvider.CreateScope();
                var idracService = scope.ServiceProvider.GetRequiredService<IIdracService>();
                
                _logger.LogInformation("Polling server {ServerName} ({IP})", server.TenServer, server.DiaChiIP);
                var package = await idracService.PollServerAsync(server, idracClient, token);
                
                // Đẩy dữ liệu về Trung tâm
                await PushDataToCenter(centerClient, package, token);
            }
        }

        private async Task PushDataToCenter(HttpClient centerClient, IdracService.IdracDataPackage package, CancellationToken token)
        {
            try 
            {
                // Đẩy Logs
                if (package.Logs.Any())
                {
                    await centerClient.PostAsJsonAsync("/api/agent/logs", package.Logs, token);
                }

                // Đẩy Info phần cứng
                if (package.Info != null)
                {
                    await centerClient.PostAsJsonAsync("/api/agent/info", package.Info, token);
                }

                // Đẩy Status & History
                if (package.StatusModules.Any())
                {
                    var update = new {
                        StatusModules = package.StatusModules,
                        StatusHistories = package.StatusHistories
                    };
                    await centerClient.PostAsJsonAsync("/api/agent/status", update, token);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to push data to center");
            }
        }
    }
}
