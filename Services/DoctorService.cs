using Microsoft.EntityFrameworkCore;
using ClinicMS.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ClinicMS.Services;

public class DoctorService
{
    private readonly CoreDatabaseService _core;

    public DoctorService(CoreDatabaseService core)
    {
        _core = core;
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
        return d;
    }

    public async Task UpdateDoctorAsync(Doctor d)
    {
        await using var db = _core.Ctx();
        db.Doctors.Update(d);
        await db.SaveChangesAsync();
    }

    public async Task DeleteDoctorAsync(int id)
    {
        await using var db = _core.Ctx();
        var d = await db.Doctors.FindAsync(id);
        if (d != null)
        {
            // Note: Cascade deletion logic might be needed if not handled by EF Core
            var visits = db.Visits.Where(v => v.DoctorId == id);
            db.Visits.RemoveRange(visits);
            db.Doctors.Remove(d);
            await db.SaveChangesAsync();
        }
    }
}
