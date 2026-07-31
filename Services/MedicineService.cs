using Microsoft.EntityFrameworkCore;
using ClinicMS.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ClinicMS.Services;

public class MedicineService
{
    private readonly CoreDatabaseService _core;
    private readonly AuditLogService _auditLog;

    public MedicineService(CoreDatabaseService core, AuditLogService auditLog)
    {
        _core = core;
        _auditLog = auditLog;
    }

    public async Task<List<Medicine>> GetMedicinesAsync()
    {
        await using var db = _core.Ctx();
        return await db.Medicines.OrderBy(m => m.MedicineName).ToListAsync();
    }

    public async Task<Medicine> AddMedicineAsync(Medicine m)
    {
        await using var db = _core.Ctx();
        db.Medicines.Add(m);
        await db.SaveChangesAsync();

        _auditLog.Log(db, "Medicine", m.MedicineId.ToString(), "Create",
            newValues: AuditLogService.Serialize(new { m.MedicineName, m.Unit, m.Price, m.StockQuantity }),
            details: $"Thêm thuốc #{m.MedicineId} - {m.MedicineName}");
        await db.SaveChangesAsync();
        return m;
    }

    public async Task UpdateMedicineAsync(Medicine m)
    {
        await using var db = _core.Ctx();
        var existing = await db.Medicines.AsNoTracking().FirstOrDefaultAsync(x => x.MedicineId == m.MedicineId);
        var oldValues = existing != null
            ? AuditLogService.Serialize(new { existing.MedicineName, existing.Unit, existing.Price, existing.StockQuantity, existing.ReservedQuantity })
            : null;

        db.Medicines.Update(m);
        await db.SaveChangesAsync();

        var changedFields = new List<string>();
        if (existing != null)
        {
            if (existing.MedicineName != m.MedicineName) changedFields.Add("medicine_name");
            if (existing.Unit != m.Unit) changedFields.Add("unit");
            if (existing.Price != m.Price) changedFields.Add("price");
            if (existing.StockQuantity != m.StockQuantity) changedFields.Add("stock_quantity");
        }
        _auditLog.Log(db, "Medicine", m.MedicineId.ToString(), "Update",
            changedFields: changedFields.Count > 0 ? AuditLogService.Serialize(changedFields) : null,
            oldValues: oldValues,
            newValues: AuditLogService.Serialize(new { m.MedicineName, m.Unit, m.Price, m.StockQuantity }),
            details: $"Cập nhật thuốc #{m.MedicineId} - {m.MedicineName}");
        await db.SaveChangesAsync();
    }

    public async Task DeleteMedicineAsync(int id)
    {
        await using var db = _core.Ctx();
        var m = await db.Medicines.FindAsync(id);
        if (m != null)
        {
            var oldValues = AuditLogService.Serialize(new { m.MedicineName, m.Unit, m.Price, m.StockQuantity });

            db.Medicines.Remove(m);
            await db.SaveChangesAsync();

            _auditLog.Log(db, "Medicine", id.ToString(), "Delete",
                oldValues: oldValues,
                details: $"Xóa thuốc #{id} - {m.MedicineName}");
            await db.SaveChangesAsync();
        }
    }
}
