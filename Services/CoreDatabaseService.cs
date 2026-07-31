using Microsoft.EntityFrameworkCore;
using ClinicMS.Data;
using ClinicMS.Models;

namespace ClinicMS.Services;

public class CoreDatabaseService
{
    public static void Initialize()
    {
        using var db = new ClinicDbContext();
        db.Database.Migrate();
        SeedMedicines(db);
    }

    private static void SeedMedicines(ClinicDbContext db)
    {
        if (db.Medicines.Any()) return;

        db.Medicines.AddRange(new List<Medicine>
        {
            new() { MedicineName = "Paracetamol 500mg", Unit = "Viên", Price = 2000, StockQuantity = 100, CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") },
            new() { MedicineName = "Amoxicillin 500mg", Unit = "Viên", Price = 5000, StockQuantity = 50, CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") },
            new() { MedicineName = "Siro Ho Prospan", Unit = "Chai", Price = 75000, StockQuantity = 20, CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") },
            new() { MedicineName = "Vitamin C 1000mg", Unit = "Viên sủi", Price = 5000, StockQuantity = 30, CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") },
            new() { MedicineName = "Hapacol 250mg", Unit = "Gói", Price = 3000, StockQuantity = 0, CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") }
        });
        db.SaveChanges();
    }

    public ClinicDbContext Ctx() => new();

    public async Task<string?> GetSettingAsync(string key)
    {
        await using var db = Ctx();
        var row = await db.AppSettings.FindAsync(key);
        return row?.Value;
    }

    public async Task SetSettingAsync(string key, string value)
    {
        await using var db = Ctx();
        var row = await db.AppSettings.FindAsync(key);
        if (row == null)
        {
            db.AppSettings.Add(new AppSetting { Key = key, Value = value });
        }
        else
        {
            row.Value = value;
            db.AppSettings.Update(row);
        }
        await db.SaveChangesAsync();
    }
}
