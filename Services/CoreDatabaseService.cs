using Microsoft.EntityFrameworkCore;
using ClinicMS.Data;
using ClinicMS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ClinicMS.Services;

/// <summary>
/// Dịch vụ lõi quản lý kết nối và các thiết lập chung của Database.
/// </summary>
public class CoreDatabaseService
{
    public static void Initialize()
    {
        using var db = new ClinicDbContext();
        // Chỉ tạo table nếu DB chưa có — không xóa dữ liệu cũ
        db.Database.EnsureCreated();
        
        // Bổ sung bảng settings nếu DB đã tồn tại
        db.Database.ExecuteSqlRaw(
            "CREATE TABLE IF NOT EXISTS AppSettings (Key TEXT PRIMARY KEY, Value TEXT)");

        // Bổ sung các bảng có thể thiếu trên DB cũ (tránh lỗi "no such table")
        db.Database.ExecuteSqlRaw(
            "CREATE TABLE IF NOT EXISTS prescription_details (" +
            "visit_id INTEGER NOT NULL, " +
            "medicine_id INTEGER NOT NULL, " +
            "quantity REAL DEFAULT 0, " +
            "dosage TEXT, " +
            "PRIMARY KEY (visit_id, medicine_id))");
        db.Database.ExecuteSqlRaw(
            "CREATE TABLE IF NOT EXISTS prescriptions (" +
            "visit_id INTEGER PRIMARY KEY, " +
            "created_at TEXT)");
        db.Database.ExecuteSqlRaw(
            "CREATE TABLE IF NOT EXISTS medicines (" +
            "medicine_id INTEGER PRIMARY KEY AUTOINCREMENT, " +
            "medicine_name TEXT NOT NULL, " +
            "unit TEXT, " +
            "price REAL NOT NULL DEFAULT 0, " +
            "stock_quantity INTEGER NOT NULL DEFAULT 0, " +
            "reserved_quantity INTEGER NOT NULL DEFAULT 0, " +
            "created_at TEXT)");
        db.Database.ExecuteSqlRaw(
            "CREATE TABLE IF NOT EXISTS invoice_items (" +
            "visit_id INTEGER NOT NULL, " +
            "item_type TEXT NOT NULL, " +
            "item_id INTEGER NOT NULL, " +
            "item_name TEXT NOT NULL DEFAULT '', " +
            "quantity INTEGER NOT NULL DEFAULT 0, " +
            "unit_price REAL NOT NULL DEFAULT 0, " +
            "PRIMARY KEY (visit_id, item_type, item_id))");
        db.Database.ExecuteSqlRaw(
            "CREATE TABLE IF NOT EXISTS invoices (" +
            "visit_id INTEGER PRIMARY KEY, " +
            "total_amount REAL NOT NULL DEFAULT 0, " +
            "status TEXT NOT NULL DEFAULT 'Unpaid', " +
            "is_deducted INTEGER NOT NULL DEFAULT 0, " +
            "created_at TEXT)");
        db.Database.ExecuteSqlRaw(
            "CREATE TABLE IF NOT EXISTS payments (" +
            "payment_id INTEGER PRIMARY KEY AUTOINCREMENT, " +
            "visit_id INTEGER NOT NULL, " +
            "amount REAL NOT NULL DEFAULT 0, " +
            "payment_method TEXT, " +
            "payment_time TEXT)");

        // ═══════════════════════════════════════════════
        //  DATABASE SCHEMA UPDATES & SAMPLE DATA
        // ═══════════════════════════════════════════════

        // Đảm bảo cột item_name tồn tại (cho DB cũ)
        try { db.Database.ExecuteSqlRaw("ALTER TABLE invoice_items ADD COLUMN item_name TEXT NOT NULL DEFAULT ''"); } catch { }
        try { db.Database.ExecuteSqlRaw("ALTER TABLE medicines ADD COLUMN reserved_quantity INTEGER NOT NULL DEFAULT 0"); } catch { }
        try { db.Database.ExecuteSqlRaw("ALTER TABLE invoices ADD COLUMN is_deducted INTEGER NOT NULL DEFAULT 0"); } catch { }

        // Thêm thuốc mẫu nếu bảng trống
        if (!db.Medicines.Any())
        {
            db.Medicines.AddRange(new List<Medicine>
            {
                new Medicine { MedicineName = "Paracetamol 500mg", Unit = "Viên", Price = 2000, StockQuantity = 100, CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") },
                new Medicine { MedicineName = "Amoxicillin 500mg", Unit = "Viên", Price = 5000, StockQuantity = 50, CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") },
                new Medicine { MedicineName = "Siro Ho Prospan", Unit = "Chai", Price = 75000, StockQuantity = 20, CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") },
                new Medicine { MedicineName = "Vitamin C 1000mg", Unit = "Viên sủi", Price = 5000, StockQuantity = 30, CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") },
                new Medicine { MedicineName = "Hapacol 250mg", Unit = "Gói", Price = 3000, StockQuantity = 0, CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") }
            });
            db.SaveChanges();
        }
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
