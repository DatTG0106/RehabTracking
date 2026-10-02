using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using RehabTracking.Web.Entities;

namespace RehabTracking.Web.Services;

public class AuditService
{
    private readonly RehabTrackingContext _db;
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public AuditService(RehabTrackingContext db, IHttpContextAccessor? httpContextAccessor = null)
    {
        _db = db;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogAsync(
        string action,
        string entityName,
        string? recordId = null,
        int? targetPatientId = null,
        string? details = null,
        int? explicitUserId = null,
        string? explicitUserRole = null)
    {
        try
        {
            var httpContext = _httpContextAccessor?.HttpContext;
            var user = httpContext?.User;

            int userId = explicitUserId ?? 0;
            string userRole = explicitUserRole ?? "System";

            if (userId <= 0 && user?.Identity?.IsAuthenticated == true)
            {
                var idClaim = user.FindFirst(ClaimTypes.NameIdentifier);
                if (idClaim != null) int.TryParse(idClaim.Value, out userId);
                if (string.IsNullOrEmpty(explicitUserRole))
                {
                    userRole = user.FindFirst(ClaimTypes.Role)?.Value ?? "Unknown";
                }
            }

            var ipAddress = httpContext?.Connection?.RemoteIpAddress?.ToString() ?? "Local";

            var audit = new AuditLog
            {
                UserId = userId > 0 ? userId : 1, // Fallback nếu chạy system action
                UserRole = userRole,
                Action = action,
                EntityName = entityName,
                RecordId = recordId,
                TargetPatientId = targetPatientId,
                IpAddress = ipAddress,
                Details = details,
                Timestamp = DateTime.UtcNow
            };

            _db.AuditLogs.Add(audit);
            await _db.SaveChangesAsync();
        }
        catch
        {
            // Bảo đảm audit log không làm crash luồng chính của người dùng
        }
    }
}
