using Microsoft.EntityFrameworkCore;
using ClinicMS.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ClinicMS.Services;

public class DoctorService
{
    private readonly CoreDatabaseService _core;
    private readonly AuditLogService _auditLog;

    public DoctorService(CoreDatabaseService core, AuditLogService auditLog)
    {
        _core = core;
        _auditLog = auditLog;
    }

    public async Task<int> GetDoctorsCountAsync()
    {
        await using var db = _core.Ctx();
        return await db.Doctors.CountAsync();
    }

    public async Task<List<Doctor>> GetDoctorsAsync()
    {
        await using var db = _core.Ctx();
        return await db.Doctors.OrderBy(d => d.FullName).ToListAsync();
    }

    public async Task<List<string>> GetAllDoctorNamesAsync()
    {
        await using var db = _core.Ctx();
        return await db.Doctors.Select(d => d.FullName ?? "").Where(n => n != "").OrderBy(n => n).ToListAsync();
    }

    public async Task<Dictionary<string, int>> GetDoctorsLookupAsync()
    {
        await using var db = _core.Ctx();
        var list = await db.Doctors
            .Select(d => new { d.DoctorId, d.FullName })
            .ToListAsync();

        var dict = new Dictionary<string, int>();
        foreach (var d in list)
        {
            var key = $"{d.FullName} (ID: {d.DoctorId})";
            dict[key] = d.DoctorId;
        }
        return dict;
    }

    public async Task<List<Doctor>> SearchDoctorsAsync(string searchTerm, int limit = 50)
    {
        await using var db = _core.Ctx();
        var term = searchTerm.ToLower();
        return await db.Doctors
            .Where(d => (d.FullName != null && d.FullName.ToLower().Contains(term)) || (d.Specialty != null && d.Specialty.ToLower().Contains(term)))
            .Take(limit)
            .ToListAsync();
    }

    public async Task<Doctor?> GetDoctorByNameAsync(string name)
    {
        await using var db = _core.Ctx();
        return await db.Doctors.FirstOrDefaultAsync(d => d.FullName == name);
    }

    public async Task<Doctor> AddDoctorAsync(Doctor d)
    {
        await using var db = _core.Ctx();
        db.Doctors.Add(d);
        await db.SaveChangesAsync();

        _auditLog.Log(db, "Doctor", d.DoctorId.ToString(), "Create",
            newValues: AuditLogService.Serialize(new { d.FullName, d.Specialty, d.Phone, d.Email }),
            details: $"Thêm bác sĩ #{d.DoctorId} - {d.FullName}");
        await db.SaveChangesAsync();
        return d;
    }

    public async Task UpdateDoctorAsync(Doctor d)
    {
        await using var db = _core.Ctx();
        var existing = await db.Doctors.AsNoTracking().FirstOrDefaultAsync(x => x.DoctorId == d.DoctorId);
        var oldValues = existing != null
            ? AuditLogService.Serialize(new { existing.FullName, existing.Specialty, existing.Phone, existing.Email })
            : null;

        db.Doctors.Update(d);
        await db.SaveChangesAsync();

        var changedFields = new List<string>();
        if (existing != null)
        {
            if (existing.FullName != d.FullName) changedFields.Add("full_name");
            if (existing.Specialty != d.Specialty) changedFields.Add("specialty");
            if (existing.Phone != d.Phone) changedFields.Add("phone");
            if (existing.Email != d.Email) changedFields.Add("email");
        }
        _auditLog.Log(db, "Doctor", d.DoctorId.ToString(), "Update",
            changedFields: changedFields.Count > 0 ? AuditLogService.Serialize(changedFields) : null,
            oldValues: oldValues,
            newValues: AuditLogService.Serialize(new { d.FullName, d.Specialty, d.Phone, d.Email }),
            details: $"Cập nhật bác sĩ #{d.DoctorId} - {d.FullName}");
        await db.SaveChangesAsync();
    }

    public async Task DeleteDoctorAsync(int id)
    {
        await using var db = _core.Ctx();
        var d = await db.Doctors.FindAsync(id);
        if (d != null)
        {
            var oldValues = AuditLogService.Serialize(new { d.FullName, d.Specialty, d.Phone, d.Email });

            var visits = db.Visits.Where(v => v.DoctorId == id);
            db.Visits.RemoveRange(visits);
            db.Doctors.Remove(d);
            await db.SaveChangesAsync();

            _auditLog.Log(db, "Doctor", id.ToString(), "Delete",
                oldValues: oldValues,
                details: $"Xóa bác sĩ #{id} - {d.FullName}");
            await db.SaveChangesAsync();
        }
    }
}
