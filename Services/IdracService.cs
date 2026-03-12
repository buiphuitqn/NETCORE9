using System.Text.Json;
using System.Net.Http.Headers;
using CORE_BE.Models;
using System.Text.Json.Serialization;

namespace CORE_BE.Services
{
    public interface IIdracService
    {
        Task<IdracDataPackage> PollServerAsync(Server server, HttpClient client, CancellationToken token);
    }

    public class IdracDataPackage
    {
        public List<IdracLog> Logs { get; set; } = new();
        public List<StatusModule> StatusModules { get; set; } = new();
        public List<StatusModuleHistory> StatusHistories { get; set; } = new();
        public InfoServer Info { get; set; }
    }

    public class IdracService : IIdracService
    {
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
        private static readonly Guid SystemUserId = Guid.Parse("F0AE0A48-4872-44B2-35A7-08DE5270D12E");

        #region DTOs (Copied from IdracWorker)
        public class LcLogResponse { public List<LcLogEntry> Members { get; set; } }
        public class LcLogEntry { public string Severity { get; set; } public string Message { get; set; } public string Id { get; set; } public DateTime Created { get; set; } }
        public class LcInfoResponse { public string Manufacturer { get; set; } public string Model { get; set; } public string SKU { get; set; } public string BiosVersion { get; set; } public string HostName { get; set; } public string OperatingSystem { get; set; } public LcMemoryInfo MemorySummary { get; set; } public LcProcessInfoResponse ProcessorSummary { get; set; } public LcStatus Status { get; set; } }
        public class LcMemoryInfo { public LcStatus Status { get; set; } public float TotalSystemMemoryGiB { get; set; } }
        public class LcProcessInfoResponse { public int Count { get; set; } public string Model { get; set; } public LcStatus Status { get; set; } }
        public class LcThermalResponse { public List<LcFanStatus> Fans { get; set; } public List<LcTemperatureStatus> Temperatures { get; set; } }
        public class LcTemperatureStatus { public string Name { get; set; } public float? ReadingCelsius { get; set; } public LcStatus Status { get; set; } }
        public class LcFanStatus { public string FanName { get; set; } public LcStatus Status { get; set; } }
        public class LcStatus { public string Health { get; set; } }
        public class LcStorageResponse { public string Name { get; set; } public LcStatus Status { get; set; } public List<LcStorageController> Devices { get; set; } }
        public class LcStorageController { public string Manufacturer { get; set; } public string Model { get; set; } public string Name { get; set; } public LcStatus Status { get; set; } }
        public class LcStorageIdrac9Response { public string Name { get; set; } public LcStatus Status { get; set; } public List<LcStorageIdrac9Controller> Drives { get; set; } }
        public class LcStorageIdrac9Controller { [JsonPropertyName("@odata.id")] public string OdataId { get; set; } }
        public class LcPowerResponse { public List<LcPowerSupply> PowerSupplies { get; set; } }
        public class LcPowerSupply { public string Model { get; set; } public float PowerCapacityWatts { get; set; } public float LineInputVoltage { get; set; } public LcStatus Status { get; set; } }
        public class LcNetworklink { public List<LcMemberNetworklink> Members { get; set; } }
        public class LcMemberNetworklink { [JsonPropertyName("@odata.id")] public string OdataId { get; set; } }
        public class LcNetworkLinkStatus { public string Id { get; set; } public float? SpeedMbps { get; set; } public LcStatus Status { get; set; } }
        #endregion

        public async Task<IdracDataPackage> PollServerAsync(Server server, HttpClient client, CancellationToken token)
        {
            var result = new IdracDataPackage();
            var now = DateTime.UtcNow;

            try
            {
                if (server.IDRACVersion == "IDRAC8")
                {
                    await FetchLogs(client, server, result, token);
                    await FetchLogsSel(client, server, result, token);
                    await FetchSystemInfo(client, server, result, now, token);
                    await FetchThermal(client, server, result, now, token);
                    await FetchStorage(client, server, result, now, token);
                    await FetchPower(client, server, result, now, token);
                    await FetchNetwork(client, server, result, now, token);
                }
                else if (server.IDRACVersion == "IDRAC9")
                {
                    await FetchLogs(client, server, result, token);
                    await FetchSystemInfoIDRAC9(client, server, result, now, token);
                    await FetchThermalIDRAC9(client, server, result, now, token);
                    await FetchStorageIDRAC9(client, server, result, now, token);
                    await FetchPowerIDRAC9(client, server, result, now, token);
                    await FetchNetworkIDRAC9(client, server, result, now, token);
                }
            }
            catch (Exception ex)
            {
                result.Logs.Add(new IdracLog {
                    ServerId = server.Id,
                    Serverity = "Error",
                    LogMessage = ex.Message,
                    ExternalLogId = Guid.NewGuid().ToString(),
                    Timestamp = now
                });
                AddStatusEntry(result.StatusModules, result.StatusHistories, server.Id, "System", "Offline", now);
            }

            return result;
        }

        private static HttpRequestMessage CreateAuthRequest(HttpMethod method, string url, string username, string password)
        {
            var request = new HttpRequestMessage(method, url);
            var byteArray = System.Text.Encoding.ASCII.GetBytes($"{username}:{password}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
            return request;
        }

        private static async Task<T> SendAndDeserialize<T>(HttpClient client, HttpRequestMessage request, CancellationToken token)
        {
            var response = await client.SendAsync(request, token);
            if (!response.IsSuccessStatusCode) return default;
            var json = await response.Content.ReadAsStringAsync(token);
            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }

        private static void AddStatusEntry(List<StatusModule> statusData, List<StatusModuleHistory> historyData,
            Guid serverId, string moduleName, string health, DateTime now, float? value = null)
        {
            var status = health ?? "Unknown";
            statusData.Add(new StatusModule { ServerId = serverId, ModuleName = moduleName, ValueMonitor = value, Status = status, CreatedDate = now });
            historyData.Add(new StatusModuleHistory { ServerId = serverId, ModuleName = moduleName, ValueMonitor = value, Status = status, RecordedAt = now, CreatedDate = now });
        }

        #region Fetch Methods (Modified to populate result PACKAGE)
        private async Task FetchLogs(HttpClient client, Server server, IdracDataPackage result, CancellationToken token)
        {
            var request = CreateAuthRequest(HttpMethod.Get, $"https://{server.DiaChiIP}/redfish/v1/Managers/iDRAC.Embedded.1/Logs/Lclog", server.Username, server.Password);
            var data = await SendAndDeserialize<LcLogResponse>(client, request, token);
            if (data?.Members != null)
            {
                foreach (var log in data.Members) result.Logs.Add(new IdracLog { ServerId = server.Id, Serverity = log.Severity, LogMessage = log.Message, ExternalLogId = log.Id, Timestamp = log.Created });
            }
        }

        private async Task FetchLogsSel(HttpClient client, Server server, IdracDataPackage result, CancellationToken token)
        {
            var request = CreateAuthRequest(HttpMethod.Get, $"https://{server.DiaChiIP}/redfish/v1/Managers/iDRAC.Embedded.1/Logs/Sel", server.Username, server.Password);
            var data = await SendAndDeserialize<LcLogResponse>(client, request, token);
            if (data?.Members != null)
            {
                foreach (var log in data.Members) result.Logs.Add(new IdracLog { ServerId = server.Id, Serverity = log.Severity, LogMessage = log.Message, ExternalLogId = log.Id, Timestamp = log.Created });
            }
        }

        private async Task FetchSystemInfo(HttpClient client, Server server, IdracDataPackage result, DateTime now, CancellationToken token)
        {
            var request = CreateAuthRequest(HttpMethod.Get, $"https://{server.DiaChiIP}/redfish/v1/Systems/System.Embedded.1", server.Username, server.Password);
            var infoData = await SendAndDeserialize<LcInfoResponse>(client, request, token);
            if (infoData == null) return;

            result.Info = new InfoServer
            {
                ServerId = server.Id,
                Manufacturer = infoData.Manufacturer,
                SystemMode = infoData.Model,
                ServiceTag = infoData.SKU,
                BiosVersion = infoData.BiosVersion,
                HostName = infoData.HostName,
                OperatingSystem = infoData.OperatingSystem ?? "Unknown",
                MemorySize = infoData.MemorySummary.TotalSystemMemoryGiB,
                CpuModel = infoData.ProcessorSummary.Count + "x" + infoData.ProcessorSummary.Model,
                CreatedDate = now
            };

            AddStatusEntry(result.StatusModules, result.StatusHistories, server.Id, "System", infoData.Status?.Health, now);
            AddStatusEntry(result.StatusModules, result.StatusHistories, server.Id, "Memory", infoData.MemorySummary.Status?.Health, now);
            AddStatusEntry(result.StatusModules, result.StatusHistories, server.Id, "Processor", infoData.ProcessorSummary.Status?.Health, now);
        }

        private async Task FetchSystemInfoIDRAC9(HttpClient client, Server server, IdracDataPackage result, DateTime now, CancellationToken token)
        {
            await FetchSystemInfo(client, server, result, now, token);
        }

        private async Task FetchThermal(HttpClient client, Server server, IdracDataPackage result, DateTime now, CancellationToken token)
        {
            var request = CreateAuthRequest(HttpMethod.Get, $"https://{server.DiaChiIP}/redfish/v1/Chassis/System.Embedded.1/Thermal", server.Username, server.Password);
            var thermalData = await SendAndDeserialize<LcThermalResponse>(client, request, token);
            if (thermalData == null) return;

            foreach (var fan in thermalData.Fans) AddStatusEntry(result.StatusModules, result.StatusHistories, server.Id, fan.FanName, fan.Status?.Health, now);
            foreach (var temp in thermalData.Temperatures) AddStatusEntry(result.StatusModules, result.StatusHistories, server.Id, temp.Name, temp.Status?.Health, now, temp.ReadingCelsius);
        }

        private async Task FetchThermalIDRAC9(HttpClient client, Server server, IdracDataPackage result, DateTime now, CancellationToken token)
        {
            await FetchThermal(client, server, result, now, token);
        }

        private async Task FetchStorage(HttpClient client, Server server, IdracDataPackage result, DateTime now, CancellationToken token)
        {
            var request = CreateAuthRequest(HttpMethod.Get, $"https://{server.DiaChiIP}/redfish/v1/Systems/System.Embedded.1/Storage/Controllers/RAID.Integrated.1-1", server.Username, server.Password);
            var storageData = await SendAndDeserialize<LcStorageResponse>(client, request, token);
            if (storageData == null) return;

            AddStatusEntry(result.StatusModules, result.StatusHistories, server.Id, storageData.Name, storageData.Status?.Health, now);
            if (storageData.Devices != null)
            {
                foreach (var device in storageData.Devices) AddStatusEntry(result.StatusModules, result.StatusHistories, server.Id, device.Name, device.Status?.Health, now);
            }
        }

        private async Task FetchStorageIDRAC9(HttpClient client, Server server, IdracDataPackage result, DateTime now, CancellationToken token)
        {
            var request = CreateAuthRequest(HttpMethod.Get, $"https://{server.DiaChiIP}/redfish/v1/Systems/System.Embedded.1/Storage/RAID.Integrated.1-1", server.Username, server.Password);
            var storageData = await SendAndDeserialize<LcStorageIdrac9Response>(client, request, token);
            if (storageData == null) return;

            AddStatusEntry(result.StatusModules, result.StatusHistories, server.Id, storageData.Name, storageData.Status?.Health, now);
            foreach (var member in storageData.Drives)
            {
                var memberRequest = CreateAuthRequest(HttpMethod.Get, $"https://{server.DiaChiIP}{member.OdataId}", server.Username, server.Password);
                var memberData = await SendAndDeserialize<LcStorageController>(client, memberRequest, token);
                if (memberData != null) AddStatusEntry(result.StatusModules, result.StatusHistories, server.Id, memberData.Name, memberData.Status?.Health, now);
            }
        }

        private async Task FetchPower(HttpClient client, Server server, IdracDataPackage result, DateTime now, CancellationToken token)
        {
            var request = CreateAuthRequest(HttpMethod.Get, $"https://{server.DiaChiIP}/redfish/v1/Chassis/System.Embedded.1/Power", server.Username, server.Password);
            var powerData = await SendAndDeserialize<LcPowerResponse>(client, request, token);
            if (powerData?.PowerSupplies == null) return;

            foreach (var power in powerData.PowerSupplies) AddStatusEntry(result.StatusModules, result.StatusHistories, server.Id, power.Model, power.Status?.Health, now, power.PowerCapacityWatts);
        }

        private async Task FetchPowerIDRAC9(HttpClient client, Server server, IdracDataPackage result, DateTime now, CancellationToken token)
        {
            await FetchPower(client, server, result, now, token);
        }

        private async Task FetchNetwork(HttpClient client, Server server, IdracDataPackage result, DateTime now, CancellationToken token)
        {
            var request = CreateAuthRequest(HttpMethod.Get, $"https://{server.DiaChiIP}/redfish/v1/Systems/System.Embedded.1/EthernetInterfaces", server.Username, server.Password);
            var networkData = await SendAndDeserialize<LcNetworklink>(client, request, token);
            if (networkData?.Members == null) return;

            foreach (var member in networkData.Members)
            {
                var memberRequest = CreateAuthRequest(HttpMethod.Get, $"https://{server.DiaChiIP}{member.OdataId}", server.Username, server.Password);
                var memberData = await SendAndDeserialize<LcNetworkLinkStatus>(client, memberRequest, token);
                if (memberData != null) AddStatusEntry(result.StatusModules, result.StatusHistories, server.Id, memberData.Id, memberData.Status?.Health, now, memberData.SpeedMbps);
            }
        }

        private async Task FetchNetworkIDRAC9(HttpClient client, Server server, IdracDataPackage result, DateTime now, CancellationToken token)
        {
            await FetchNetwork(client, server, result, now, token);
        }
        #endregion
    }
}
