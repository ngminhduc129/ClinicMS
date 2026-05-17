using Microsoft.EntityFrameworkCore;
using ClinicMS.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ClinicMS.Services;

public class MedicineService
{
    private readonly CoreDatabaseService _core;

    public MedicineService(CoreDatabaseService core)
    {
        _core = core;
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
        return m;
    }

    public async Task UpdateMedicineAsync(Medicine m)
    {
        await using var db = _core.Ctx();
        db.Medicines.Update(m);
        await db.SaveChangesAsync();
    }

    public async Task DeleteMedicineAsync(int id)
    {
        await using var db = _core.Ctx();
        var m = await db.Medicines.FindAsync(id);
        if (m != null)
        {
            db.Medicines.Remove(m);
            await db.SaveChangesAsync();
        }
    }
}
