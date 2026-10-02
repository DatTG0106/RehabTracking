using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RehabTracking.Web.Entities;

namespace RehabTracking.Web.Features.Healthcare.EHR;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Doctor,Admin")]
public class AuditLogsController : ControllerBase
{
    private readonly RehabTrackingContext _db;

    public AuditLogsController(RehabTrackingContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Tra cứu lịch sử nhật ký kiểm toán (Audit Logs)
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<AuditLog>>> GetLogs(
        [FromQuery] string? action = null,
        [FromQuery] string? entityName = null,
        [FromQuery] int? targetPatientId = null,
        [FromQuery] int limit = 50)
    {
        var query = _db.AuditLogs
            .AsNoTracking()
            .Include(a => a.User)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(action))
        {
            query = query.Where(a => a.Action == action);
        }

        if (!string.IsNullOrWhiteSpace(entityName))
        {
            query = query.Where(a => a.EntityName == entityName);
        }

        if (targetPatientId.HasValue && targetPatientId.Value > 0)
        {
            query = query.Where(a => a.TargetPatientId == targetPatientId.Value);
        }

        var list = await query
            .OrderByDescending(a => a.Timestamp)
            .Take(limit)
            .ToListAsync();

        return Ok(list);
    }

    /// <summary>
    /// Tra cứu lịch sử truy cập hồ sơ của một bệnh nhân cụ thể
    /// </summary>
    [HttpGet("patient/{patientId:int}")]
    public async Task<ActionResult<List<AuditLog>>> GetPatientLogs(int patientId, [FromQuery] int limit = 30)
    {
        var logs = await _db.AuditLogs
            .AsNoTracking()
            .Include(a => a.User)
            .Where(a => a.TargetPatientId == patientId)
            .OrderByDescending(a => a.Timestamp)
            .Take(limit)
            .ToListAsync();

        return Ok(logs);
    }
}
