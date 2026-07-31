using System;
using System.Text.Json;
using ClinicMS.Data;
using ClinicMS.Models;

namespace ClinicMS.Services;

public class AuditLogService
{
    public void Log(ClinicDbContext db, string entityName, string? entityId, string action,
        string? changedFields = null, string? oldValues = null, string? newValues = null,
        string? performedBy = null, string? details = null)
    {
        db.AuditLogs.Add(new AuditLog
        {
            EntityName = entityName,
            EntityId = entityId,
            Action = action,
            ChangedFields = changedFields,
            OldValues = oldValues,
            NewValues = newValues,
            PerformedBy = performedBy,
            Details = details,
            Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        });
    }

    public static string Serialize(object value)
    {
        return JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = false });
    }
}
