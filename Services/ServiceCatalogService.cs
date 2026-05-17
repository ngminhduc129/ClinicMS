using Microsoft.EntityFrameworkCore;
using ClinicMS.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ClinicMS.Services;

public class ServiceCatalogService
{
    private readonly CoreDatabaseService _core;

    public ServiceCatalogService(CoreDatabaseService core)
    {
        _core = core;
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
        return s;
    }

    public async Task UpdateServiceAsync(Service s)
    {
        await using var db = _core.Ctx();
        db.Services.Update(s);
        await db.SaveChangesAsync();
    }

    public async Task DeleteServiceAsync(int id)
    {
        await using var db = _core.Ctx();
        var s = await db.Services.FindAsync(id);
        if (s != null)
        {
            db.Services.Remove(s);
            await db.SaveChangesAsync();
        }
    }
}
