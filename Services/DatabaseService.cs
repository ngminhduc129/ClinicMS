using Microsoft.EntityFrameworkCore;
using ClinicMS.Data;
using ClinicMS.Models;
using System.Data;

namespace ClinicMS.Services;

public class DatabaseService
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
            "CREATE TABLE IF NOT EXISTS PrescriptionDetails (" +
            "VisitId INTEGER NOT NULL, " +
            "MedicineId INTEGER NOT NULL, " +
            "Quantity REAL DEFAULT 0, " +
            "Dosage TEXT, " +
            "PRIMARY KEY (VisitId, MedicineId))");
        db.Database.ExecuteSqlRaw(
            "CREATE TABLE IF NOT EXISTS prescriptions (" +
            "visit_id INTEGER PRIMARY KEY, " +
            "created_at TEXT)");
        db.Database.ExecuteSqlRaw(
            "CREATE TABLE IF NOT EXISTS service_orders (" +
            "service_order_id INTEGER PRIMARY KEY AUTOINCREMENT, " +
            "visit_id INTEGER NOT NULL, " +
            "service_id INTEGER NOT NULL, " +
            "result TEXT)");
        db.Database.ExecuteSqlRaw(
            "CREATE TABLE IF NOT EXISTS invoice_items (" +
            "visit_id INTEGER NOT NULL, " +
            "item_type TEXT NOT NULL, " +
            "item_id INTEGER NOT NULL, " +
            "quantity INTEGER NOT NULL DEFAULT 0, " +
            "unit_price REAL NOT NULL DEFAULT 0, " +
            "PRIMARY KEY (visit_id, item_type, item_id))");
        db.Database.ExecuteSqlRaw(
            "CREATE TABLE IF NOT EXISTS invoices (" +
            "visit_id INTEGER PRIMARY KEY, " +
            "total_amount REAL NOT NULL DEFAULT 0, " +
            "status TEXT NOT NULL DEFAULT 'Unpaid', " +
            "created_at TEXT)");
        db.Database.ExecuteSqlRaw(
            "CREATE TABLE IF NOT EXISTS payments (" +
            "payment_id INTEGER PRIMARY KEY AUTOINCREMENT, " +
            "visit_id INTEGER NOT NULL, " +
            "amount REAL NOT NULL DEFAULT 0, " +
            "payment_method TEXT, " +
            "payment_time TEXT)");

        // ═══════════════════════════════════════════════
        //  DATABASE SCHEMA UPDATES
        // ═══════════════════════════════════════════════
        // SQLite không hỗ trợ Migrations tốt bằng SQL Server, nên ta dùng SQL thủ công cho các cột mới
        try { db.Database.ExecuteSqlRaw("ALTER TABLE PrescriptionDetails ADD COLUMN Quantity REAL DEFAULT 0"); } catch { }
        try { db.Database.ExecuteSqlRaw("ALTER TABLE PrescriptionDetails ADD COLUMN Dosage TEXT"); } catch { }
        try { db.Database.ExecuteSqlRaw("ALTER TABLE PrescriptionDetails ADD COLUMN PrescriptionVisitId INTEGER"); } catch { }
        try { db.Database.ExecuteSqlRaw("UPDATE PrescriptionDetails SET PrescriptionVisitId = VisitId WHERE PrescriptionVisitId IS NULL"); } catch { }
    }

    private static ClinicDbContext Ctx() => new();

    // ═══════════════════════════════════════════════
    //  SETTINGS
    // ═══════════════════════════════════════════════
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

    // ═══════════════════════════════════════════════
    //  PATIENTS
    // ═══════════════════════════════════════════════
    public async Task<int> GetPatientsCountAsync()
    {
        await using var db = Ctx();
        return await db.Patients.CountAsync();
    }

    public async Task<List<Patient>> GetPatientsWithContactsAsync(int offset = 0, int limit = 1000)
    {
        await using var db = Ctx();
        var query = from p in db.Patients
                    join pc in db.PatientContacts.Where(x => x.IsPrimary == 1) on p.PatientId equals pc.PatientId into contacts
                    from subPc in contacts.DefaultIfEmpty()
                    join c in db.Contacts on (subPc != null ? subPc.ContactId : -1) equals c.ContactId into finalContacts
                    from subC in finalContacts.DefaultIfEmpty()
                    orderby p.FullName
                    select new { p, subC };

        var results = await query.Skip(offset).Take(limit).ToListAsync();
        var list = new List<Patient>();

        foreach (var item in results)
        {
            var p = item.p;
            if (item.subC != null)
            {
                p.Phone = item.subC.Phone;
                p.Email = item.subC.Email;
                p.Address = item.subC.Address;
            }
            list.Add(p);
        }
        return list;
    }

    public async Task<List<Patient>> GetPatientsWithContactsAsync() 
        => await GetPatientsWithContactsAsync(0, 50000); // Backwards compatibility

    public async Task<List<Patient>> GetPatientsAsync()
    {
        await using var db = Ctx();
        return await db.Patients.OrderBy(p => p.FullName).ToListAsync();
    }

    public async Task<Patient> AddPatientAsync(Patient p)
    {
        await using var db = Ctx();
        // Tạo contact trống đi kèm
        var contact = new Contact
        {
            Phone   = p.Phone,
            Email   = p.Email,
            Address = p.Address
        };
        db.Contacts.Add(contact);
        await db.SaveChangesAsync();

        p.Phone = null; p.Email = null; p.Address = null; // stored in contacts
        db.Patients.Add(p);
        await db.SaveChangesAsync();

        db.PatientContacts.Add(new PatientContact
        {
            PatientId = p.PatientId,
            ContactId = contact.ContactId,
            IsPrimary = 1
        });
        await db.SaveChangesAsync();
        return p;
    }

    public async Task UpdatePatientAsync(Patient p)
    {
        await using var db = Ctx();
        db.Patients.Update(p);
        // Update liên kết contact nếu có
        var pc = await db.PatientContacts
                         .FirstOrDefaultAsync(x => x.PatientId == p.PatientId && x.IsPrimary == 1);
        if (pc != null)
        {
            var c = await db.Contacts.FindAsync(pc.ContactId);
            if (c != null)
            {
                c.Phone   = p.Phone;
                c.Email   = p.Email;
                c.Address = p.Address;
                db.Contacts.Update(c);
            }
        }
        await db.SaveChangesAsync();
    }

    public async Task DeletePatientAsync(int id)
    {
        await using var db = Ctx();
        var p = await db.Patients.FindAsync(id);
        if (p == null) return;
        var pcs   = db.PatientContacts.Where(x => x.PatientId == id);
        var cids  = await pcs.Select(x => x.ContactId).ToListAsync();
        db.PatientContacts.RemoveRange(pcs);
        await db.SaveChangesAsync();
        var visitIds = await db.Visits.Where(v => v.PatientId == id).Select(v => v.VisitId).ToListAsync();
        await DeleteVisitRelatedDataAsync(db, visitIds);
        var visits = db.Visits.Where(v => v.PatientId == id);
        db.Visits.RemoveRange(visits);
        db.Patients.Remove(p);
        var contacts = db.Contacts.Where(c => cids.Contains(c.ContactId));
        db.Contacts.RemoveRange(contacts);
        await db.SaveChangesAsync();
    }

    // Lấy patient kèm contact
    public async Task<(Patient? patient, Contact? contact)> GetPatientWithContactAsync(int id)
    {
        await using var db = Ctx();
        var p  = await db.Patients.FindAsync(id);
        var pc = await db.PatientContacts
                         .FirstOrDefaultAsync(x => x.PatientId == id && x.IsPrimary == 1);
        Contact? c = pc != null ? await db.Contacts.FindAsync(pc.ContactId) : null;
        if (p != null) { p.Phone = c?.Phone; p.Email = c?.Email; p.Address = c?.Address; }
        return (p, c);
    }

    public async Task<List<Visit>> GetPatientVisitsAsync(int patientId)
    {
        await using var db = Ctx();
        return await db.Visits
                       .Include(v => v.Doctor)
                       .Where(v => v.PatientId == patientId)
                       .OrderByDescending(v => v.VisitTime)
                       .ToListAsync();
    }

    // ═══════════════════════════════════════════════
    //  DOCTORS
    // ═══════════════════════════════════════════════
    public async Task<List<Doctor>> GetDoctorsAsync()
    {
        await using var db = Ctx();
        return await db.Doctors.OrderBy(d => d.FullName).ToListAsync();
    }

    public async Task<Doctor> AddDoctorAsync(Doctor d)
    {
        await using var db = Ctx();
        db.Doctors.Add(d);
        await db.SaveChangesAsync();
        return d;
    }

    public async Task UpdateDoctorAsync(Doctor d)
    {
        await using var db = Ctx();
        db.Doctors.Update(d);
        await db.SaveChangesAsync();
    }

    public async Task DeleteDoctorAsync(int id)
    {
        await using var db = Ctx();
        var d = await db.Doctors.FindAsync(id);
        if (d != null)
        {
            // Xóa tất cả các lượt khám liên quan đến bác sĩ này trước
            var visitIds = await db.Visits.Where(v => v.DoctorId == id).Select(v => v.VisitId).ToListAsync();
            await DeleteVisitRelatedDataAsync(db, visitIds);
            var visits = db.Visits.Where(v => v.DoctorId == id);
            db.Visits.RemoveRange(visits);
            await db.SaveChangesAsync();

            db.Doctors.Remove(d);
            await db.SaveChangesAsync();
        }
    }

    // ═══════════════════════════════════════════════
    //  VISITS
    // ═══════════════════════════════════════════════
    public async Task<List<Visit>> GetVisitsAsync()
    {
        await using var db = Ctx();
        return await db.Visits
                       .Include(v => v.Patient)
                       .Include(v => v.Doctor)
                       .OrderByDescending(v => v.VisitTime)
                       .ToListAsync();
    }

    public async Task<Visit> AddVisitAsync(Visit v)
    {
        await using var db = Ctx();
        if (string.IsNullOrEmpty(v.VisitTime))
            v.VisitTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        db.Visits.Add(v);
        await db.SaveChangesAsync();
        return v;
    }

    public async Task UpdateVisitAsync(Visit v)
    {
        await using var db = Ctx();
        db.Visits.Update(v);
        await db.SaveChangesAsync();
    }

    public async Task DeleteVisitAsync(int id)
    {
        await using var db = Ctx();
        var v = await db.Visits.FindAsync(id);
        if (v != null)
        {
            await DeleteVisitRelatedDataAsync(db, new List<int> { id });
            db.Visits.Remove(v);
            await db.SaveChangesAsync();
        }
    }

    // ═══════════════════════════════════════════════
    //  MEDICINES
    // ═══════════════════════════════════════════════
    public async Task<List<Medicine>> GetMedicinesAsync()
    {
        await using var db = Ctx();
        return await db.Medicines.OrderBy(m => m.MedicineName).ToListAsync();
    }

    public async Task<Medicine> AddMedicineAsync(Medicine m)
    {
        await using var db = Ctx();
        if (string.IsNullOrEmpty(m.CreatedAt)) m.CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        db.Medicines.Add(m);
        await db.SaveChangesAsync();
        return m;
    }

    public async Task UpdateMedicineAsync(Medicine m)
    {
        await using var db = Ctx();
        db.Medicines.Update(m);
        await db.SaveChangesAsync();
    }

    public async Task DeleteMedicineAsync(int id)
    {
        await using var db = Ctx();
        var m = await db.Medicines.FindAsync(id);
        if (m != null) { db.Medicines.Remove(m); await db.SaveChangesAsync(); }
    }

    // ═══════════════════════════════════════════════
    //  SERVICES
    // ═══════════════════════════════════════════════
    public async Task<List<Service>> GetServicesAsync()
    {
        await using var db = Ctx();
        return await db.Services.OrderBy(s => s.ServiceName).ToListAsync();
    }

    public async Task<Service> AddServiceAsync(Service s)
    {
        await using var db = Ctx();
        db.Services.Add(s);
        await db.SaveChangesAsync();
        return s;
    }

    public async Task UpdateServiceAsync(Service s)
    {
        await using var db = Ctx();
        db.Services.Update(s);
        await db.SaveChangesAsync();
    }

    public async Task DeleteServiceAsync(int id)
    {
        await using var db = Ctx();
        var s = await db.Services.FindAsync(id);
        if (s != null) { db.Services.Remove(s); await db.SaveChangesAsync(); }
    }

    // ═══════════════════════════════════════════════
    //  INVOICES
    // ═══════════════════════════════════════════════
    public async Task<List<Invoice>> GetInvoicesAsync()
    {
        await using var db = Ctx();
        return await db.Invoices
                       .Include(i => i.Visit).ThenInclude(v => v!.Patient)
                       .Include(i => i.Visit).ThenInclude(v => v!.Doctor)
                       .Include(i => i.Payments)
                       .OrderByDescending(i => i.CreatedAt)
                       .ToListAsync();
    }

    public async Task UpdateInvoiceStatusAsync(int visitId, string status)
    {
        await using var db = Ctx();
        var inv = await db.Invoices.FindAsync(visitId);
        if (inv != null) { inv.Status = status; await db.SaveChangesAsync(); }
    }

    public async Task AddPaymentAsync(Payment pay)
    {
        await using var db = Ctx();
        if (string.IsNullOrEmpty(pay.PaymentTime))
            pay.PaymentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        db.Payments.Add(pay);
        // Tự động đánh dấu Paid nếu đủ tiền
        var inv = await db.Invoices.FindAsync(pay.VisitId);
        if (inv != null)
        {
            var paid = await db.Payments
                               .Where(p => p.VisitId == pay.VisitId)
                               .SumAsync(p => p.Amount);
            paid += pay.Amount;
            inv.Status = paid >= inv.TotalAmount ? "Paid" : "Partial";
            db.Invoices.Update(inv);
        }
        await db.SaveChangesAsync();
    }

    private static async Task DeleteVisitRelatedDataAsync(ClinicDbContext db, List<int> visitIds)
    {
        if (visitIds.Count == 0) return;

        if (await TableExistsAsync(db, "payments"))
        {
            var payments = db.Payments.Where(p => visitIds.Contains(p.VisitId));
            db.Payments.RemoveRange(payments);
        }

        if (await TableExistsAsync(db, "invoice_items"))
        {
            var invoiceItems = db.InvoiceItems.Where(ii => visitIds.Contains(ii.VisitId));
            db.InvoiceItems.RemoveRange(invoiceItems);
        }

        if (await TableExistsAsync(db, "invoices"))
        {
            var invoices = db.Invoices.Where(i => visitIds.Contains(i.VisitId));
            db.Invoices.RemoveRange(invoices);
        }

        if (await TableExistsAsync(db, "PrescriptionDetails"))
        {
            var prescriptionDetails = db.PrescriptionDetails.Where(pd => visitIds.Contains(pd.VisitId));
            db.PrescriptionDetails.RemoveRange(prescriptionDetails);
        }

        if (await TableExistsAsync(db, "prescriptions"))
        {
            var prescriptions = db.Prescriptions.Where(p => visitIds.Contains(p.VisitId));
            db.Prescriptions.RemoveRange(prescriptions);
        }

        if (await TableExistsAsync(db, "service_orders"))
        {
            var serviceOrders = db.ServiceOrders.Where(so => visitIds.Contains(so.VisitId));
            db.ServiceOrders.RemoveRange(serviceOrders);
        }

        await db.SaveChangesAsync();
    }

    private static async Task<bool> TableExistsAsync(ClinicDbContext db, string tableName)
    {
        var conn = db.Database.GetDbConnection();
        var closeAfter = conn.State != ConnectionState.Open;
        if (closeAfter) await conn.OpenAsync();
        try
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT 1 FROM sqlite_master WHERE type='table' AND name = $name LIMIT 1";
            var p = cmd.CreateParameter();
            p.ParameterName = "$name";
            p.Value = tableName;
            cmd.Parameters.Add(p);
            var result = await cmd.ExecuteScalarAsync();
            return result != null && result != DBNull.Value;
        }
        finally
        {
            if (closeAfter) await conn.CloseAsync();
        }
    }
}
