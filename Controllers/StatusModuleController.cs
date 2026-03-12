using CORE_BE.Infrastructure;
using CORE_BE.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CORE_BE.Controllers;

[Authorize]
[EnableCors("CorsApi")]
[Route("api/[controller]")]
[ApiController]
public class StatusModuleController : ControllerBase
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<StatusModuleController> _logger;

    public StatusModuleController(IUnitOfWork uow, ILogger<StatusModuleController> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    [HttpGet("GetByServerId")]
    public IActionResult GetByServerId(Guid serverId)
    {
        var list = _uow.GetRepository<StatusModule>()
            .GetAll(x => x.ServerId == serverId && !x.IsDeleted, null, null);
        return Ok(list);
    }

    [HttpGet("GetCurrentTemperatures")]
    public IActionResult GetCurrentTemperatures(Guid ServerId)
    {
        var list = _uow.GetRepository<StatusModule>()
            .GetAll(x => !x.IsDeleted && x.ServerId == ServerId && x.ModuleName.Contains("Temp"), null, new string[] { "Server" });

        var result = list
            .Where(x => x.Server != null)
            .GroupBy(x => x.Server.TenServer)
            .Select(g => new
            {
                name = g.Key,
                temps = g.Select(s => new
                {
                    label = s.ModuleName,
                    value = s.ValueMonitor ?? 0
                }).ToList()
            })
            .ToList();

        return Ok(result);
    }

    [HttpGet("GetTemperatureHistory")]
    public IActionResult GetTemperatureHistory(Guid serverId)
    {
        var past24Hours = DateTime.Now.AddHours(-24);
        
        var list = _uow.GetRepository<StatusModuleHistory>()
            .GetAll(x => !x.IsDeleted && x.ModuleName.Contains("Temp") && x.ServerId == serverId && x.RecordedAt >= past24Hours && x.ModuleName.Contains("Temp"), null, new string[] { "Server" });

        var groupedByHour = list
            .Where(x => x.Server != null)
            .GroupBy(x => 
            {
                var localTime = x.RecordedAt;
                if (localTime.Kind == DateTimeKind.Utc)
                {
                    localTime = TimeZoneInfo.ConvertTimeFromUtc(x.RecordedAt, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
                }
                else if (localTime.Kind == DateTimeKind.Unspecified)
                {
                    localTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(x.RecordedAt, DateTimeKind.Utc), TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
                }
                return new { localTime.Date, localTime.Hour };
            })
            .OrderBy(g => g.Key.Date).ThenBy(g => g.Key.Hour)
            .ToList();

        var result = new List<Dictionary<string, object>>();

        foreach (var hourGroup in groupedByHour)
        {
            var dict = new Dictionary<string, object>();
            dict["time"] = $"{hourGroup.Key.Hour:D2}:00";
            
            var moduleGroups = hourGroup.GroupBy(x => x.ModuleName);
            foreach (var sg in moduleGroups)
            {
                var maxTemp = sg.Max(x => x.ValueMonitor ?? 0);
                dict[sg.Key] = Math.Round(maxTemp, 1);
            }
            result.Add(dict);
        }

        return Ok(result);
    }
}