using Microsoft.EntityFrameworkCore;
using ClinicMS.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ClinicMS.Services;

public class ServiceCatalogService
{
    private readonly CoreDatabaseService _core;
    private readonly AuditLogService _auditLog;

    public ServiceCatalogService(CoreDatabaseService core, AuditLogService auditLog)
    {
        _core = core;
        _auditLog = auditLog;
    }

    public async Task<List<Service>> GetServicesAsync()
    {
        await using var db = _core.Ctx();
        return await db.Services.OrderBy(s => s.ServiceName).ToListAsync();
    }

    public async Task<Service> AddServiceAsync(Service s)
    {
        await using var db = _core.Ctx();
        db.Services.Add(s);
        await db.SaveChangesAsync();

        _auditLog.Log(db, "Service", s.ServiceId.ToString(), "Create",
            newValues: AuditLogService.Serialize(new { s.ServiceName, s.Price }),
            details: $"Thêm dịch vụ #{s.ServiceId} - {s.ServiceName}");
        await db.SaveChangesAsync();
        return s;
    }

    public async Task UpdateServiceAsync(Service s)
    {
        await using var db = _core.Ctx();
        var existing = await db.Services.AsNoTracking().FirstOrDefaultAsync(x => x.ServiceId == s.ServiceId);
        var oldValues = existing != null
            ? AuditLogService.Serialize(new { existing.ServiceName, existing.Price })
            : null;

        db.Services.Update(s);
        await db.SaveChangesAsync();

        var changedFields = new List<string>();
        if (existing != null)
        {
            if (existing.ServiceName != s.ServiceName) changedFields.Add("service_name");
            if (existing.Price != s.Price) changedFields.Add("price");
        }
        _auditLog.Log(db, "Service", s.ServiceId.ToString(), "Update",
            changedFields: changedFields.Count > 0 ? AuditLogService.Serialize(changedFields) : null,
            oldValues: oldValues,
            newValues: AuditLogService.Serialize(new { s.ServiceName, s.Price }),
            details: $"Cập nhật dịch vụ #{s.ServiceId} - {s.ServiceName}");
        await db.SaveChangesAsync();
    }

    public async Task DeleteServiceAsync(int id)
    {
        await using var db = _core.Ctx();
        var s = await db.Services.FindAsync(id);
        if (s != null)
        {
            var oldValues = AuditLogService.Serialize(new { s.ServiceName, s.Price });

            db.Services.Remove(s);
            await db.SaveChangesAsync();

            _auditLog.Log(db, "Service", id.ToString(), "Delete",
                oldValues: oldValues,
                details: $"Xóa dịch vụ #{id} - {s.ServiceName}");
            await db.SaveChangesAsync();
        }
    }
}
