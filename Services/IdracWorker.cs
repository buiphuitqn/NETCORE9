using System.Text.Json;
using CORE_BE.Data;
using CORE_BE.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

public class IdracWorker : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<IdracWorker> _logger;

    public class LcLogResponse
    {
        public List<LcLogEntry> Members { get; set; }
    }

    public class LcLogEntry
    {
        public string Severity { get; set; }
        public string Message { get; set; }

        public string Id { get; set; } // iDRAC log entry ID, dùng Id làm ExternalLogId để tránh trùng lặp khi lưu vào DB
        public DateTime Created { get; set; }
    }

    public class LcInfoResponse
    {
        public string Manufacturer { get; set; }
        public string Model { get; set; }
        public string SKU { get; set; }
        public string BiosVersion { get; set; }
        public string HostName { get; set; }
        public string OperatingSystem { get; set; }
        public LcMemoryInfo MemorySummary { get; set; }
        public LcProcessInfoResponse ProcessorSummary { get; set; }
        public LcStatus Status { get; set; }
    }

    public class LcMemoryInfo
    {
        public LcStatus Status { get; set; }
        public float TotalSystemMemoryGiB { get; set; }
    }

    public class LcProcessInfoResponse
    {
        public int Count { get; set; }
        public string Model { get; set; }
        public LcStatus Status { get; set; }
    }

    public class LcThermalResponse
    {
        public List<LcFanStatus> Fans { get; set; }
        public List<LcTemperatureStatus> Temperatures { get; set; }
    }

    public class LcTemperatureStatus
    {
        public string Name { get; set; }

        public float? ReadingCelsius { get; set; }
        public LcStatus Status { get; set; }
    }

    public class LcFanStatus
    {
        public string FanName { get; set; }
        public LcStatus Status { get; set; }
    }

    public class LcStatus
    {
        public string Health { get; set; }
    }

    public class LcStorageResponse
    {
        public string Name { get; set; }
        public LcStatus Status { get; set; }
        public List<LcStorageController> Devices { get; set; }
    }

    public class LcStorageController
    {
        public string Manufacturer { get; set; }
        public string Model { get; set; }
        public string Name { get; set; }
        public LcStatus Status { get; set; }
    }

    public class LcPowerResponse
    {
        public List<LcPowerSupply> PowerSupplies { get; set; }
    }

    public class LcPowerSupply
    {
        public string Model { get; set; }
        public float PowerCapacityWatts { get; set; }
        public float LineInputVoltage { get; set; }
        public LcStatus Status { get; set; }
    }

    public class LcNetworklink
    {
        public List<LcMemberNetworklink> Members { get; set; }
    }

    public class LcMemberNetworklink
    {
        [JsonPropertyName("@odata.id")]
        public string OdataId { get; set; }
    }

    public class LcNetworkLinkStatus
    {
        public string Id { get; set; }
        public float? SpeedMbps { get; set; }
        public LcStatus Status { get; set; }
    }

    public IdracWorker(IServiceScopeFactory serviceScopeFactory, ILogger<IdracWorker> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
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

        var servers = await db.Server.Where(x => x.IsActive).ToListAsync(token);

        var tasks = servers.Select(server => PollServer(server, httpFactory, token));

        await Task.WhenAll(tasks);
    }

    private async Task PollServer(
        Server server,
        IHttpClientFactory httpFactory,
        CancellationToken token
    )
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();
        var client = httpFactory.CreateClient("idrac");

        client.Timeout = TimeSpan.FromSeconds(60); // tăng timeout lên 60s

        try
        {
            if (server.IDRACVersion == "IDRAC8")
            {
                // Gọi API iDRAC để lấy log
                var request = new HttpRequestMessage(
                    HttpMethod.Get,
                    $"https://{server.DiaChiIP}/redfish/v1/Managers/iDRAC.Embedded.1/Logs/Lclog"
                );

                var byteArray = System.Text.Encoding.ASCII.GetBytes(
                    $"{server.Username}:{server.Password}"
                );

                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Basic",
                    Convert.ToBase64String(byteArray)
                );

                var response = await client.SendAsync(request, token);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync(token);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                var data = JsonSerializer.Deserialize<LcLogResponse>(json, options);

                if (data?.Members != null)
                {
                    foreach (var logdata in data.Members)
                    {
                        bool exists = await db.IdracLog.AnyAsync(x =>
                            x.ServerId == server.Id && x.ExternalLogId == logdata.Id
                        );
                        if (!exists)
                        {
                            var log = new IdracLog
                            {
                                ServerId = server.Id,
                                Serverity = logdata.Severity,
                                LogMessage = logdata.Message,
                                ExternalLogId = logdata.Id,
                                CreatedDate = DateTime.Now,
                                Timestamp = logdata.Created,
                                CreatedBy = Guid.Parse("F0AE0A48-4872-44B2-35A7-08DE5270D12E"),
                            };

                            db.IdracLog.Add(log);
                        }
                    }
                }

                //Gọi API iDRAC để lấy thông tin server
                var statusData = new List<StatusModule>();
                var statusHistoryData = new List<StatusModuleHistory>();
                var infoRequest = new HttpRequestMessage(
                    HttpMethod.Get,
                    $"https://{server.DiaChiIP}/redfish/v1/Systems/System.Embedded.1"
                );

                infoRequest.Headers.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue(
                        "Basic",
                        Convert.ToBase64String(byteArray)
                    );

                var infoResponse = await client.SendAsync(infoRequest, token);
                infoResponse.EnsureSuccessStatusCode();

                var infoJson = await infoResponse.Content.ReadAsStringAsync(token);
                var infoData = JsonSerializer.Deserialize<LcInfoResponse>(infoJson, options);
                if (infoData != null)
                {
                    bool infoExists = await db.InfoServer.AnyAsync(x => x.ServerId == server.Id);
                    if (!infoExists)
                    {
                        var info = new InfoServer
                        {
                            ServerId = server.Id,
                            Manufacturer = infoData.Manufacturer,
                            SystemMode = infoData.Model,
                            ServiceTag = infoData.SKU,
                            BiosVersion = infoData.BiosVersion,
                            HostName = infoData.HostName,
                            OperatingSystem = infoData.OperatingSystem ?? "Unknown", // iDRAC API không trả về OS version, cần gọi thêm API khác hoặc để trống
                            MemorySize = infoData.MemorySummary.TotalSystemMemoryGiB,
                            CpuModel =
                                infoData.ProcessorSummary.Count + "x" + infoData.ProcessorSummary.Model,
                            CreatedDate = DateTime.Now,
                            CreatedBy = Guid.Parse("F0AE0A48-4872-44B2-35A7-08DE5270D12E"),
                        };
                        db.InfoServer.Add(info);
                    }
                    else
                    {
                        var existingInfo = await db.InfoServer.FirstOrDefaultAsync(
                            x => x.ServerId == server.Id,
                            token
                        );
                        if (existingInfo != null)
                        {
                            existingInfo.Manufacturer = infoData.Manufacturer;
                            existingInfo.SystemMode = infoData.Model;
                            existingInfo.ServiceTag = infoData.SKU;
                            existingInfo.BiosVersion = infoData.BiosVersion;
                            existingInfo.HostName = infoData.HostName;
                            existingInfo.OperatingSystem = infoData.OperatingSystem ?? "Unknown";
                            existingInfo.MemorySize = infoData.MemorySummary.TotalSystemMemoryGiB;
                            existingInfo.CpuModel = infoData.ProcessorSummary.Model;
                            existingInfo.UpdatedDate = DateTime.Now;
                            existingInfo.UpdatedBy = Guid.Parse("F0AE0A48-4872-44B2-35A7-08DE5270D12E");
                        }
                    }
                    statusHistoryData.Add(
                        new StatusModuleHistory
                        {
                            ServerId = server.Id,
                            ModuleName = "Memory",
                            Status = infoData.MemorySummary.Status?.Health ?? "Unknown",
                            RecordedAt = DateTime.Now,
                            CreatedDate = DateTime.Now,
                            CreatedBy = Guid.Parse("F0AE0A48-4872-44B2-35A7-08DE5270D12E"),
                        }
                    );
                    statusHistoryData.Add(
                        new StatusModuleHistory
                        {
                            ServerId = server.Id,
                            ModuleName = "System",
                            Status = infoData.Status?.Health ?? "Unknown",
                            RecordedAt = DateTime.Now,
                            CreatedDate = DateTime.Now,
                            CreatedBy = Guid.Parse("F0AE0A48-4872-44B2-35A7-08DE5270D12E"),
                        }
                    );
                    statusHistoryData.Add(
                        new StatusModuleHistory
                        {
                            ServerId = server.Id,
                            ModuleName = "Processor",
                            Status = infoData.ProcessorSummary.Status?.Health ?? "Unknown",
                            RecordedAt = DateTime.Now,
                            CreatedDate = DateTime.Now,
                            CreatedBy = Guid.Parse("F0AE0A48-4872-44B2-35A7-08DE5270D12E"),
                        }
                    );

                    statusData.Add(
                        new StatusModule
                        {
                            ServerId = server.Id,
                            ModuleName = "System",
                            Status = infoData.Status?.Health ?? "Unknown",
                            CreatedBy = Guid.Parse("F0AE0A48-4872-44B2-35A7-08DE5270D12E"),
                            CreatedDate = DateTime.Now,
                        }
                    );
                    statusData.Add(
                        new StatusModule
                        {
                            ServerId = server.Id,
                            ModuleName = "Memory",
                            Status = infoData.MemorySummary.Status?.Health ?? "Unknown",
                            CreatedBy = Guid.Parse("F0AE0A48-4872-44B2-35A7-08DE5270D12E"),
                            CreatedDate = DateTime.Now,
                        }
                    );
                    statusData.Add(
                        new StatusModule
                        {
                            ServerId = server.Id,
                            ModuleName = "Processor",
                            Status = infoData.ProcessorSummary.Status?.Health ?? "Unknown",
                            CreatedBy = Guid.Parse("F0AE0A48-4872-44B2-35A7-08DE5270D12E"),
                            CreatedDate = DateTime.Now,
                        }
                    );
                }

                // Gọi API iDRAC để lấy trạng thái module
                var statusRequest = new HttpRequestMessage(
                    HttpMethod.Get,
                    $"https://{server.DiaChiIP}/redfish/v1/Chassis/System.Embedded.1/Thermal"
                );
                statusRequest.Headers.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue(
                        "Basic",
                        Convert.ToBase64String(byteArray)
                    );

                var statusResponse = await client.SendAsync(statusRequest, token);
                statusResponse.EnsureSuccessStatusCode();
                // var infoJson = await infoResponse.Content.ReadAsStringAsync(token);
                // var infoData = JsonSerializer.Deserialize<LcInfoResponse>(infoJson, options);
                var statusJson = await statusResponse.Content.ReadAsStringAsync(token);
                var statusDoc = JsonSerializer.Deserialize<LcThermalResponse>(statusJson, options);
                if (statusDoc != null)
                {
                    foreach (var fan in statusDoc.Fans)
                    {
                        statusData.Add(
                            new StatusModule
                            {
                                ServerId = server.Id,
                                ModuleName = fan.FanName,
                                Status = fan.Status?.Health ?? "Unknown",
                                CreatedBy = Guid.Parse("F0AE0A48-4872-44B2-35A7-08DE5270D12E"),
                                CreatedDate = DateTime.Now,
                            }
                        );
                        statusHistoryData.Add(
                            new StatusModuleHistory
                            {
                                ServerId = server.Id,
                                ModuleName = fan.FanName,
                                Status = fan.Status?.Health ?? "Unknown",
                                RecordedAt = DateTime.Now,
                                CreatedDate = DateTime.Now,
                                CreatedBy = Guid.Parse("F0AE0A48-4872-44B2-35A7-08DE5270D12E"),
                            }
                        );
                    }

                    foreach (var temp in statusDoc.Temperatures)
                    {
                        statusData.Add(
                            new StatusModule
                            {
                                ServerId = server.Id,
                                ModuleName = temp.Name,
                                ValueMonitor = temp.ReadingCelsius,
                                Status = temp.Status?.Health ?? "Unknown",
                                CreatedBy = Guid.Parse("F0AE0A48-4872-44B2-35A7-08DE5270D12E"),
                                CreatedDate = DateTime.Now,
                            }
                        );
                        statusHistoryData.Add(
                            new StatusModuleHistory
                            {
                                ServerId = server.Id,
                                ModuleName = temp.Name,
                                ValueMonitor = temp.ReadingCelsius,
                                Status = temp.Status?.Health ?? "Unknown",
                                RecordedAt = DateTime.Now,
                                CreatedDate = DateTime.Now,
                                CreatedBy = Guid.Parse("F0AE0A48-4872-44B2-35A7-08DE5270D12E"),
                            }
                        );
                    }
                }


                //Call API iDRAC để lấy thông tin storage
                var storageRequest = new HttpRequestMessage(
                    HttpMethod.Get,
                    $"https://{server.DiaChiIP}/redfish/v1/Systems/System.Embedded.1/Storage/Controllers/RAID.Integrated.1-1"
                );
                storageRequest.Headers.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue(
                        "Basic",
                        Convert.ToBase64String(byteArray)
                    );

                var storageResponse = await client.SendAsync(storageRequest, token);
                storageResponse.EnsureSuccessStatusCode();
                var storageJson = await storageResponse.Content.ReadAsStringAsync(token);
                var storageData = JsonSerializer.Deserialize<LcStorageResponse>(storageJson, options);
                if (storageData != null)
                {
                    statusData.Add(
                        new StatusModule
                        {
                            ServerId = server.Id,
                            ModuleName = storageData.Name,
                            Status = storageData.Status?.Health ?? "Unknown",
                            CreatedBy = Guid.Parse("F0AE0A48-4872-44B2-35A7-08DE5270D12E"),
                            CreatedDate = DateTime.Now,
                        }
                    );
                }
                if (storageData?.Devices != null)
                {
                    foreach (var storage in storageData.Devices)
                    {
                        statusData.Add(
                            new StatusModule
                            {
                                ServerId = server.Id,
                                ModuleName = storage.Name,
                                Status = storage.Status?.Health ?? "Unknown",
                                CreatedBy = Guid.Parse("F0AE0A48-4872-44B2-35A7-08DE5270D12E"),
                                CreatedDate = DateTime.Now,
                            }
                        );
                        statusHistoryData.Add(
                            new StatusModuleHistory
                            {
                                ServerId = server.Id,
                                ModuleName = storage.Name,
                                Status = storage.Status?.Health ?? "Unknown",
                                RecordedAt = DateTime.Now,
                                CreatedDate = DateTime.Now,
                                CreatedBy = Guid.Parse("F0AE0A48-4872-44B2-35A7-08DE5270D12E"),
                            }
                        );
                    }
                }

                //Call API iDRAC để lấy thông tin Power
                var powerRequest = new HttpRequestMessage(
                    HttpMethod.Get,
                    $"https://{server.DiaChiIP}/redfish/v1/Chassis/System.Embedded.1/Power"
                );
                powerRequest.Headers.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue(
                        "Basic",
                        Convert.ToBase64String(byteArray)
                    );

                var powerResponse = await client.SendAsync(powerRequest, token);
                powerResponse.EnsureSuccessStatusCode();
                var powerJson = await powerResponse.Content.ReadAsStringAsync(token);
                var powerData = JsonSerializer.Deserialize<LcPowerResponse>(powerJson, options);
                if (powerData.PowerSupplies != null)
                {
                    foreach (var power in powerData.PowerSupplies)
                    {
                        statusData.Add(
                            new StatusModule
                            {
                                ServerId = server.Id,
                                ModuleName = power.Model,
                                ValueMonitor = power.PowerCapacityWatts,
                                Status = power.Status?.Health ?? "Unknown",
                                CreatedBy = Guid.Parse("F0AE0A48-4872-44B2-35A7-08DE5270D12E"),
                                CreatedDate = DateTime.Now,
                            }
                        );
                        statusHistoryData.Add(
                            new StatusModuleHistory
                            {
                                ServerId = server.Id,
                                ModuleName = power.Model,
                                Status = power.Status?.Health ?? "Unknown",
                                RecordedAt = DateTime.Now,
                                CreatedDate = DateTime.Now,
                                CreatedBy = Guid.Parse("F0AE0A48-4872-44B2-35A7-08DE5270D12E"),
                            }
                        );
                    }
                }

                // Call API iDRAC để lấy thông tin network link
                var networkRequest = new HttpRequestMessage(
                    HttpMethod.Get,
                    $"https://{server.DiaChiIP}/redfish/v1/Systems/System.Embedded.1/EthernetInterfaces"
                );
                networkRequest.Headers.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue(
                        "Basic",
                        Convert.ToBase64String(byteArray)
                    );
                var networkResponse = await client.SendAsync(networkRequest, token);
                networkResponse.EnsureSuccessStatusCode();
                var networkJson = await networkResponse.Content.ReadAsStringAsync(token);
                var networkData = JsonSerializer.Deserialize<LcNetworklink>(networkJson, options);
                if (networkData?.Members != null)
                {
                    foreach (var member in networkData.Members)
                    {
                        var memberRequest = new HttpRequestMessage(
                            HttpMethod.Get,
                            $"https://{server.DiaChiIP}{member.OdataId}"
                        );
                        memberRequest.Headers.Authorization =
                            new System.Net.Http.Headers.AuthenticationHeaderValue(
                                "Basic",
                                Convert.ToBase64String(byteArray)
                            );
                        var memberResponse = await client.SendAsync(memberRequest, token);
                        memberResponse.EnsureSuccessStatusCode();
                        var memberJson = await memberResponse.Content.ReadAsStringAsync(token);
                        var memberData = JsonSerializer.Deserialize<LcNetworkLinkStatus>(memberJson, options);
                        if (memberData != null)
                        {
                            statusData.Add(
                                new StatusModule
                                {
                                    ServerId = server.Id,
                                    ModuleName = memberData.Id,
                                    ValueMonitor = memberData.SpeedMbps,
                                    Status = memberData.Status?.Health ?? "Unknown", // iDRAC API không trả về status của network link, cần gọi thêm API khác hoặc để trống
                                    CreatedBy = Guid.Parse("F0AE0A48-4872-44B2-35A7-08DE5270D12E"),
                                    CreatedDate = DateTime.Now,
                                }
                            );
                            statusHistoryData.Add(
                                new StatusModuleHistory
                                {
                                    ServerId = server.Id,
                                    ModuleName = memberData.Id,
                                    ValueMonitor = memberData.SpeedMbps,
                                    Status = memberData.Status?.Health ?? "Unknown", // iDRAC API không trả về status của network link, cần gọi thêm API khác hoặc để trống
                                    RecordedAt = DateTime.Now,
                                    CreatedDate = DateTime.Now,
                                    CreatedBy = Guid.Parse("F0AE0A48-4872-44B2-35A7-08DE5270D12E"),
                                }
                            );
                        }
                    }
                }

                var moduleName = statusData.Select(x => x.ModuleName).ToList();
                var existingRecords = await db
                    .StatusModule.Where(x =>
                        x.ServerId == server.Id && moduleName.Contains(x.ModuleName)
                    )
                    .ToListAsync(token);
                foreach (var status in statusData)
                {
                    var existingRecord = existingRecords.FirstOrDefault(x =>
                        x.ModuleName == status.ModuleName
                    );
                    if (existingRecord != null)
                    {
                        existingRecord.ValueMonitor = status.ValueMonitor;
                        existingRecord.Status = status.Status;
                        existingRecord.UpdatedDate = DateTime.Now;
                        existingRecord.UpdatedBy = Guid.Parse("F0AE0A48-4872-44B2-35A7-08DE5270D12E");
                    }
                    else
                    {
                        db.StatusModule.Add(status);
                    }
                }
                db.statusModuleHistory.AddRange(statusHistoryData);
                await db.SaveChangesAsync(token);
            }
        }
        catch (Exception ex)
        {
            var now = DateTime.Now;
            var systemUser = Guid.Parse("F0AE0A48-4872-44B2-35A7-08DE5270D12E");

            // 1️⃣ Ghi log lỗi
            db.IdracLog.Add(
                new IdracLog
                {
                    ServerId = server.Id,
                    Serverity = "Error",
                    LogMessage = ex.Message,
                    ExternalLogId = Guid.NewGuid().ToString(),
                    Timestamp = now,
                    CreatedDate = now,
                    CreatedBy = systemUser,
                }
            );

            // 2️⃣ Check status hiện tại của server
            var existingStatus = await db.StatusModule
                .FirstOrDefaultAsync(x =>
                    x.ServerId == server.Id &&
                    x.ModuleName == "System", token);

            if (existingStatus != null)
            {
                existingStatus.Status = "Offline";
                existingStatus.UpdatedDate = now;
                existingStatus.UpdatedBy = systemUser;
            }
            else
            {
                db.StatusModule.Add(new StatusModule
                {
                    ServerId = server.Id,
                    ModuleName = "System",
                    Status = "Offline",
                    CreatedDate = now,
                    CreatedBy = systemUser
                });
            }

            // 3️⃣ Insert history
            db.statusModuleHistory.Add(new StatusModuleHistory
            {
                ServerId = server.Id,
                ModuleName = "System",
                Status = "Offline",
                RecordedAt = now,
                CreatedDate = now,
                CreatedBy = systemUser
            });

            var info = new InfoServer
            {
                ServerId = server.Id,
                Manufacturer = "Unknown",
                SystemMode = "Unknown",
                ServiceTag = "Unknown",
                BiosVersion = "Unknown",
                HostName = "Unknown",
                OperatingSystem = "Unknown", // iDRAC API không trả về OS version, cần gọi thêm API khác hoặc để trống
                MemorySize = 0,
                CpuModel = "Unknown",
                CreatedDate = DateTime.Now,
                CreatedBy = Guid.Parse("F0AE0A48-4872-44B2-35A7-08DE5270D12E"),
            };
            db.InfoServer.Add(info);

            await db.SaveChangesAsync(token);
        }
    }
}
