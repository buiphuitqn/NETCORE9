using CORE_BE.Data;
using CORE_BE.Models;
using Microsoft.EntityFrameworkCore;

namespace CORE_BE.Services
{
    public class IdracWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<IdracWorker> _logger;
        private readonly IConfiguration _config;

        public IdracWorker(IServiceScopeFactory serviceScopeFactory, ILogger<IdracWorker> logger, IConfiguration config)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
            _config = config;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var mode = _config["DistributedSettings:Mode"];
            if (mode != "Center")
            {
                _logger.LogInformation("IdracWorker (Center Mode) is disabled (Mode: {Mode})", mode);
                return;
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessServers(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Worker error");
                }

                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        private async Task ProcessServers(CancellationToken token)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();
            var httpFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
            var idracService = scope.ServiceProvider.GetRequiredService<IIdracService>();

            var servers = await db.Servers.Where(x => x.IsActive).ToListAsync(token);
            var idracClient = httpFactory.CreateClient("idrac");
            
            foreach (var server in servers)
            {
                try 
                {
                    var package = await idracService.PollServerAsync(server, idracClient, token);
                    await SavePackageToDb(db, server, package, token);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error polling {ServerName}", server.TenServer);
                }
            }
        }

        private async Task SavePackageToDb(MyDbContext db, Server server, IdracService.IdracDataPackage package, CancellationToken token)
        {
            var now = DateTime.UtcNow;

            // 1. Logs
            foreach (var log in package.Logs)
            {
                bool exists = await db.IdracLogs.AnyAsync(x => x.ServerId == server.Id && x.ExternalLogId == log.ExternalLogId, token);
                if (!exists) 
                {
                    log.CreatedDate = now;
                    db.IdracLogs.Add(log);
                }
            }

            // 2. Info
            if (package.Info != null)
            {
                var existingInfo = await db.InfoServers.FirstOrDefaultAsync(x => x.ServerId == server.Id, token);
                if (existingInfo == null) db.InfoServers.Add(package.Info);
                else
                {
                    existingInfo.Manufacturer = package.Info.Manufacturer;
                    existingInfo.SystemMode = package.Info.SystemMode;
                    existingInfo.ServiceTag = package.Info.ServiceTag;
                    existingInfo.BiosVersion = package.Info.BiosVersion;
                    existingInfo.HostName = package.Info.HostName;
                    existingInfo.OperatingSystem = package.Info.OperatingSystem;
                    existingInfo.MemorySize = package.Info.MemorySize;
                    existingInfo.CpuModel = package.Info.CpuModel;
                    existingInfo.UpdatedDate = now;
                }
            }

            // 3. Status Modules
            var moduleNames = package.StatusModules.Select(x => x.ModuleName).ToList();
            var existingRecords = await db.StatusModules
                .Where(x => x.ServerId == server.Id && moduleNames.Contains(x.ModuleName))
                .ToListAsync(token);

            foreach (var status in package.StatusModules)
            {
                var existing = existingRecords.FirstOrDefault(x => x.ModuleName == status.ModuleName);
                if (existing != null)
                {
                    existing.ValueMonitor = status.ValueMonitor;
                    existing.Status = status.Status;
                    existing.UpdatedDate = now;
                }
                else db.StatusModules.Add(status);
            }

            // 4. History
            db.StatusModuleHistories.AddRange(package.StatusHistories);

            await db.SaveChangesAsync(token);
        }
    }
}
