using CORE_BE.Data;
using CORE_BE.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CORE_BE.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AgentController : ControllerBase
    {
        private readonly MyDbContext _db;
        private readonly IConfiguration _config;
        private static readonly Guid SystemUserId = Guid.Parse("F0AE0A48-4872-44B2-35A7-08DE5270D12E");

        public AgentController(MyDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        private bool IsValidApiKey()
        {
            var requestKey = Request.Headers["X-Api-Key"].ToString();
            var serverKey = _config["DistributedSettings:ApiKey"];
            return !string.IsNullOrEmpty(serverKey) && requestKey == serverKey;
        }

        /// <summary>
        /// Lấy danh sách máy chủ theo mã đơn vị
        /// </summary>
        [HttpGet("servers")]
        public async Task<IActionResult> GetServers([FromQuery] string unitCode)
        {
            if (!IsValidApiKey()) return Unauthorized("API Key không hợp lệ");
            if (string.IsNullOrEmpty(unitCode)) return BadRequest("Mã đơn vị không được trống");

            var servers = await _db.Servers
                .Where(x => x.IsActive && x.DonVi.MaDonVi == unitCode)
                .Select(x => new
                {
                    x.Id,
                    x.MaServer,
                    x.TenServer,
                    x.DiaChiIP,
                    x.Username,
                    x.Password,
                    x.IDRACVersion
                })
                .ToListAsync();

            return Ok(servers);
        }

        /// <summary>
        /// Nhận Log từ Agent
        /// </summary>
        [HttpPost("logs")]
        public async Task<IActionResult> PushLogs([FromBody] List<IdracLog> logs)
        {
            if (!IsValidApiKey()) return Unauthorized("API Key không hợp lệ");
            if (logs == null || !logs.Any()) return Ok();

            foreach (var log in logs)
            {
                bool exists = await _db.IdracLogs.AnyAsync(x => x.ServerId == log.ServerId && x.ExternalLogId == log.ExternalLogId);
                if (!exists)
                {
                    log.Id = Guid.NewGuid();
                    log.CreatedBy = SystemUserId;
                    log.CreatedDate = DateTime.UtcNow;
                    _db.IdracLogs.Add(log);
                }
            }

            await _db.SaveChangesAsync();
            return Ok();
        }

        /// <summary>
        /// Nhận thông tin phần cứng từ Agent
        /// </summary>
        [HttpPost("info")]
        public async Task<IActionResult> PushInfo([FromBody] InfoServer info)
        {
            if (!IsValidApiKey()) return Unauthorized("API Key không hợp lệ");
            if (info == null) return BadRequest();

            var existing = await _db.InfoServers.FirstOrDefaultAsync(x => x.ServerId == info.ServerId);
            if (existing == null)
            {
                info.Id = Guid.NewGuid();
                info.CreatedBy = SystemUserId;
                info.CreatedDate = DateTime.UtcNow;
                _db.InfoServers.Add(info);
            }
            else
            {
                existing.Manufacturer = info.Manufacturer;
                existing.SystemMode = info.SystemMode;
                existing.ServiceTag = info.ServiceTag;
                existing.BiosVersion = info.BiosVersion;
                existing.HostName = info.HostName;
                existing.OperatingSystem = info.OperatingSystem;
                existing.MemorySize = info.MemorySize;
                existing.CpuModel = info.CpuModel;
                existing.UpdatedDate = DateTime.UtcNow;
                existing.UpdatedBy = SystemUserId;
            }

            await _db.SaveChangesAsync();
            return Ok();
        }

        /// <summary>
        /// Nhận trạng thái module từ Agent
        /// </summary>
        [HttpPost("status")]
        public async Task<IActionResult> PushStatus([FromBody] AgentStatusUpdate update)
        {
            if (!IsValidApiKey()) return Unauthorized("API Key không hợp lệ");
            if (update == null || update.StatusModules == null) return BadRequest();

            var now = DateTime.UtcNow;
            
            // Xử lý StatusModules (Upsert)
            foreach (var status in update.StatusModules)
            {
                var existing = await _db.StatusModules
                    .FirstOrDefaultAsync(x => x.ServerId == status.ServerId && x.ModuleName == status.ModuleName);

                if (existing != null)
                {
                    existing.ValueMonitor = status.ValueMonitor;
                    existing.Status = status.Status;
                    existing.UpdatedDate = now;
                    existing.UpdatedBy = SystemUserId;
                }
                else
                {
                    status.Id = Guid.NewGuid();
                    status.CreatedBy = SystemUserId;
                    status.CreatedDate = now;
                    _db.StatusModules.Add(status);
                }
            }

            // Xử lý StatusHistory (Chỉ thêm mới)
            if (update.StatusHistories != null)
            {
                foreach (var history in update.StatusHistories)
                {
                    history.Id = Guid.NewGuid();
                    history.CreatedBy = SystemUserId;
                    history.CreatedDate = now;
                    _db.StatusModuleHistories.Add(history);
                }
            }

            await _db.SaveChangesAsync();
            return Ok();
        }

        public class AgentStatusUpdate
        {
            public List<StatusModule> StatusModules { get; set; }
            public List<StatusModuleHistory> StatusHistories { get; set; }
        }
    }
}
