using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using ClinicMS.Data;
using ClinicMS.Models;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace ClinicMS.Services;

public class DatabaseService
{
    public static void Initialize()
    {
        using var db = new ClinicDbContext();

        // ═══════════════════════════════════════════════
        //  TẠO BẢNG
        // ═══════════════════════════════════════════════
        db.Database.ExecuteSqlRaw(@"
            CREATE TABLE IF NOT EXISTS patients (
                patient_id    INTEGER PRIMARY KEY AUTOINCREMENT,
                full_name     TEXT    NOT NULL DEFAULT '',
                date_of_birth TEXT,
                gender        TEXT,
                blood_type    TEXT,
                height_cm     REAL,
                weight_kg     REAL,
                note          TEXT,
                created_at    TEXT
            )");

        db.Database.ExecuteSqlRaw(@"
            CREATE TABLE IF NOT EXISTS doctors (
                doctor_id  INTEGER PRIMARY KEY AUTOINCREMENT,
                full_name  TEXT NOT NULL DEFAULT '',
                specialty  TEXT,
                phone      TEXT,
                email      TEXT,
                note       TEXT,
                created_at TEXT
            )");

        db.Database.ExecuteSqlRaw(@"
            CREATE TABLE IF NOT EXISTS visits (
                visit_id          INTEGER PRIMARY KEY AUTOINCREMENT,
                patient_id        INTEGER NOT NULL,
                doctor_id         INTEGER NOT NULL,
                appointment_id    INTEGER,
                visit_time        TEXT,
                visit_type        TEXT NOT NULL DEFAULT 'Walk-in',
                status            TEXT NOT NULL DEFAULT 'Waiting',
                symptoms          TEXT,
                diagnosis         TEXT,
                note              TEXT,
                pulse             TEXT,
                blood_pressure    TEXT,
                temperature       TEXT,
                weight            TEXT,
                prescription_text TEXT,
                created_at        TEXT
            )");

        db.Database.ExecuteSqlRaw(@"
            CREATE TABLE IF NOT EXISTS medicines (
                medicine_id    INTEGER PRIMARY KEY AUTOINCREMENT,
                medicine_name  TEXT NOT NULL DEFAULT '',
                unit           TEXT,
                price          REAL NOT NULL DEFAULT 0,
                stock_quantity INTEGER NOT NULL DEFAULT 0,
                created_at     TEXT
            )");

        db.Database.ExecuteSqlRaw(@"
            CREATE TABLE IF NOT EXISTS contacts (
                contact_id INTEGER PRIMARY KEY AUTOINCREMENT,
                phone      TEXT,
                email      TEXT,
                address    TEXT,
                created_at TEXT
            )");

        db.Database.ExecuteSqlRaw(@"
            CREATE TABLE IF NOT EXISTS patient_contacts (
                patient_id INTEGER NOT NULL,
                contact_id INTEGER NOT NULL,
                relationship TEXT,
                is_primary INTEGER NOT NULL DEFAULT 0,
                PRIMARY KEY (patient_id, contact_id)
            )");

        db.Database.ExecuteSqlRaw(@"
            CREATE TABLE IF NOT EXISTS services (
                service_id   INTEGER PRIMARY KEY AUTOINCREMENT,
                service_name TEXT NOT NULL DEFAULT '',
                price        REAL NOT NULL DEFAULT 0,
                created_at   TEXT
            )");

        db.Database.ExecuteSqlRaw(@"
            CREATE TABLE IF NOT EXISTS service_orders (
                service_order_id INTEGER PRIMARY KEY AUTOINCREMENT,
                visit_id         INTEGER NOT NULL,
                service_id       INTEGER NOT NULL,
                result           TEXT
            )");

        db.Database.ExecuteSqlRaw(@"
            CREATE TABLE IF NOT EXISTS PrescriptionDetails (
                visit_id    INTEGER NOT NULL,
                medicine_id INTEGER NOT NULL,
                quantity    REAL    NOT NULL DEFAULT 0,
                dosage      TEXT,
                PRIMARY KEY (visit_id, medicine_id)
            )");

        db.Database.ExecuteSqlRaw(@"
            CREATE TABLE IF NOT EXISTS prescriptions (
                visit_id   INTEGER PRIMARY KEY,
                created_at TEXT
            )");

        db.Database.ExecuteSqlRaw(@"
            CREATE TABLE IF NOT EXISTS invoices (
                visit_id     INTEGER PRIMARY KEY,
                total_amount REAL NOT NULL DEFAULT 0,
                status       TEXT NOT NULL DEFAULT 'Unpaid',
                created_at   TEXT
            )");

        db.Database.ExecuteSqlRaw(@"
            CREATE TABLE IF NOT EXISTS invoice_items (
                visit_id   INTEGER NOT NULL,
                item_type  TEXT    NOT NULL,
                item_id    INTEGER NOT NULL,
                quantity   INTEGER NOT NULL DEFAULT 0,
                unit_price REAL    NOT NULL DEFAULT 0,
                PRIMARY KEY (visit_id, item_type, item_id)
            )");

        db.Database.ExecuteSqlRaw(@"
            CREATE TABLE IF NOT EXISTS payments (
                payment_id     INTEGER PRIMARY KEY AUTOINCREMENT,
                visit_id       INTEGER NOT NULL,
                amount         REAL    NOT NULL DEFAULT 0,
                payment_method TEXT,
                payment_time   TEXT
            )");

        db.Database.ExecuteSqlRaw(@"
            CREATE TABLE IF NOT EXISTS AppSettings (
                Key   TEXT PRIMARY KEY,
                Value TEXT
            )");

        db.Database.ExecuteSqlRaw(@"
            CREATE TABLE IF NOT EXISTS appointments (
                appointment_id   INTEGER PRIMARY KEY AUTOINCREMENT,
                patient_id       INTEGER,
                doctor_id        INTEGER,
                appointment_time TEXT,
                notes            TEXT,
                created_at       TEXT
            )");

        // ═══════════════════════════════════════════════
        //  VÁ SCHEMA CHO DB CŨ (thêm cột còn thiếu)
        // ═══════════════════════════════════════════════
        // patients
        try
        {
            db.Database.ExecuteSqlRaw("ALTER TABLE patients ADD COLUMN blood_type  TEXT");
        }
        catch
        {
        }

        try
        {
            db.Database.ExecuteSqlRaw("ALTER TABLE patients ADD COLUMN height_cm   REAL");
        }
        catch
        {
        }

        try
        {
            db.Database.ExecuteSqlRaw("ALTER TABLE patients ADD COLUMN weight_kg   REAL");
        }
        catch
        {
        }

        try
        {
            db.Database.ExecuteSqlRaw("ALTER TABLE patients ADD COLUMN note        TEXT");
        }
        catch
        {
        }

        try
        {
            db.Database.ExecuteSqlRaw("ALTER TABLE patients ADD COLUMN created_at  TEXT");
        }
        catch
        {
        }

        // doctors
        try
        {
            db.Database.ExecuteSqlRaw("ALTER TABLE doctors ADD COLUMN note        TEXT");
        }
        catch
        {
        }

        try
        {
            db.Database.ExecuteSqlRaw("ALTER TABLE doctors ADD COLUMN created_at  TEXT");
        }
        catch
        {
        }

        // visits
        try
        {
            db.Database.ExecuteSqlRaw("ALTER TABLE visits ADD COLUMN appointment_id    INTEGER");
        }
        catch
        {
        }

        try
        {
            db.Database.ExecuteSqlRaw(
                "ALTER TABLE visits ADD COLUMN visit_type        TEXT NOT NULL DEFAULT 'Walk-in'");
        }
        catch
        {
        }

        try
        {
            db.Database.ExecuteSqlRaw(
                "ALTER TABLE visits ADD COLUMN status            TEXT NOT NULL DEFAULT 'Waiting'");
        }
        catch
        {
        }

        try
        {
            db.Database.ExecuteSqlRaw("ALTER TABLE visits ADD COLUMN symptoms          TEXT");
        }
        catch
        {
        }

        try
        {
            db.Database.ExecuteSqlRaw("ALTER TABLE visits ADD COLUMN pulse             TEXT");
        }
        catch
        {
        }

        try
        {
            db.Database.ExecuteSqlRaw("ALTER TABLE visits ADD COLUMN blood_pressure    TEXT");
        }
        catch
        {
        }

        try
        {
            db.Database.ExecuteSqlRaw("ALTER TABLE visits ADD COLUMN temperature       TEXT");
        }
        catch
        {
        }

        try
        {
            db.Database.ExecuteSqlRaw("ALTER TABLE visits ADD COLUMN weight            TEXT");
        }
        catch
        {
        }

        try
        {
            db.Database.ExecuteSqlRaw("ALTER TABLE visits ADD COLUMN prescription_text TEXT");
        }
        catch
        {
        }

        try
        {
            db.Database.ExecuteSqlRaw("ALTER TABLE visits ADD COLUMN created_at        TEXT");
        }
        catch
        {
        }

        // medicines
        try
        {
            db.Database.ExecuteSqlRaw("ALTER TABLE medicines ADD COLUMN stock_quantity INTEGER NOT NULL DEFAULT 0");
        }
        catch
        {
        }

        try
        {
            db.Database.ExecuteSqlRaw("ALTER TABLE medicines ADD COLUMN created_at     TEXT");
        }
        catch
        {
        }

        // services
        try
        {
            db.Database.ExecuteSqlRaw("ALTER TABLE services ADD COLUMN created_at TEXT");
        }
        catch
        {
        }

        // PrescriptionDetails
        try
        {
            db.Database.ExecuteSqlRaw("ALTER TABLE PrescriptionDetails ADD COLUMN quantity             REAL DEFAULT 0");
        }
        catch
        {
        }

        try
        {
            db.Database.ExecuteSqlRaw("ALTER TABLE PrescriptionDetails ADD COLUMN dosage               TEXT");
        }
        catch
        {
        }

        try
        {
            db.Database.ExecuteSqlRaw("ALTER TABLE PrescriptionDetails ADD COLUMN PrescriptionVisitId  INTEGER");
        }
        catch
        {
        }

        try
        {
            db.Database.ExecuteSqlRaw(
                "UPDATE PrescriptionDetails SET PrescriptionVisitId = visit_id WHERE PrescriptionVisitId IS NULL");
        }
        catch
        {
        }
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

    public async Task<List<string>> GetAllPatientNamesAsync()
    {
        await using var db = Ctx();
        return await db.Patients.Select(p => p.FullName ?? "").Where(n => n != "").OrderBy(n => n).ToListAsync();
    }

    public async Task<List<string>> GetAllDoctorNamesAsync()
    {
        await using var db = Ctx();
        return await db.Doctors.Select(d => d.FullName ?? "").Where(n => n != "").OrderBy(n => n).ToListAsync();
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

    public async Task<List<Patient>> SearchPatientsAsync(string searchTerm, int limit = 50)
    {
        await using var db = Ctx();
        var query = from p in db.Patients
            join pc in db.PatientContacts.Where(x => x.IsPrimary == 1) on p.PatientId equals pc.PatientId into contacts
            from subPc in contacts.DefaultIfEmpty()
            join c in db.Contacts on (subPc != null ? subPc.ContactId : -1) equals c.ContactId into finalContacts
            from subC in finalContacts.DefaultIfEmpty()
            where string.IsNullOrEmpty(searchTerm) || p.FullName.ToLower().Contains(searchTerm.ToLower())
            orderby p.FullName
            select new { p, subC };

        var results = await query.Take(limit).ToListAsync();
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

    public async Task<Patient?> GetPatientByNameAsync(string name)
    {
        await using var db = Ctx();
        return await db.Patients.FirstOrDefaultAsync(p => p.FullName == name);
    }

    public async Task<List<Doctor>> SearchDoctorsAsync(string searchTerm, int limit = 50)
    {
        await using var db = Ctx();
        var term = (searchTerm ?? "").ToLower();
        return await db.Doctors
            .Where(d => string.IsNullOrEmpty(term) ||
                        (d.FullName != null && d.FullName.ToLower().Contains(term)) ||
                        (d.Phone != null && d.Phone.ToLower().Contains(term)))
            .OrderBy(d => d.FullName)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<Doctor?> GetDoctorByNameAsync(string name)
    {
        await using var db = Ctx();
        return await db.Doctors.FirstOrDefaultAsync(d => d.FullName == name);
    }

    public async Task<List<Patient>> GetPatientsAsync()
    {
        await using var db = Ctx();
        return await db.Patients.OrderBy(p => p.FullName).ToListAsync();
    }

    public async Task<Patient> AddPatientAsync(Patient p)
    {
        await using var db = Ctx();
        try
        {
            var contact = new Contact
            {
                Phone   = p.Phone,
                Email   = p.Email,
                Address = p.Address
            };
            db.Contacts.Add(contact);
            await db.SaveChangesAsync();

            var newPatient = new Patient
            {
                FullName    = p.FullName,
                DateOfBirth = p.DateOfBirth,
                Gender      = p.Gender,
                BloodType   = p.BloodType,
                HeightCm    = p.HeightCm,
                WeightKg    = p.WeightKg,
                Note        = p.Note,
                CreatedAt   = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };
            db.Patients.Add(newPatient);
            await db.SaveChangesAsync();

            db.PatientContacts.Add(new PatientContact
            {
                PatientId = newPatient.PatientId,
                ContactId = contact.ContactId,
                IsPrimary = 1
            });
            await db.SaveChangesAsync();

            p.PatientId = newPatient.PatientId;
            return p;
        }
        catch (Exception ex)
        {
            // Hiện full exception bao gồm InnerException
            var fullError = ex.InnerException?.Message ?? ex.Message;
            System.Windows.MessageBox.Show(
                $"Lỗi AddPatient:\n\n{fullError}\n\nFull:\n{ex}",
                "DB Error");
            throw;
        }
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
                c.Phone = p.Phone;
                c.Email = p.Email;
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
        var pcs = db.PatientContacts.Where(x => x.PatientId == id);
        var cids = await pcs.Select(x => x.ContactId).ToListAsync();
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
        var p = await db.Patients.FindAsync(id);
        var pc = await db.PatientContacts
            .FirstOrDefaultAsync(x => x.PatientId == id && x.IsPrimary == 1);
        Contact? c = pc != null ? await db.Contacts.FindAsync(pc.ContactId) : null;
        if (p != null)
        {
            p.Phone = c?.Phone;
            p.Email = c?.Email;
            p.Address = c?.Address;
        }

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

    public async Task<List<PrescriptionDetail>> GetPrescriptionDetailsAsync(int visitId)
    {
        await using var db = Ctx();
        return await db.PrescriptionDetails
            .Include(pd => pd.Medicine)
            .Where(pd => pd.VisitId == visitId)
            .ToListAsync();
    }

    public async Task SavePrescriptionDetailsAsync(int visitId, List<PrescriptionDetail> items)
    {
        await using var db = Ctx();

        // Xóa đơn cũ
        var old = await db.PrescriptionDetails.Where(pd => pd.VisitId == visitId).ToListAsync();
        if (old.Any()) db.PrescriptionDetails.RemoveRange(old);
        await db.SaveChangesAsync();

        // Thêm mới
        foreach (var item in items)
        {
            var detail = new PrescriptionDetail
            {
                VisitId = visitId,
                MedicineId = item.MedicineId,
                Quantity = item.Quantity,
                Dosage = item.Dosage
            };
            db.PrescriptionDetails.Add(detail);
        }

        await db.SaveChangesAsync();
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
        if (m != null)
        {
            db.Medicines.Remove(m);
            await db.SaveChangesAsync();
        }
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
        if (s != null)
        {
            db.Services.Remove(s);
            await db.SaveChangesAsync();
        }
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
        if (inv != null)
        {
            inv.Status = status;
            await db.SaveChangesAsync();
        }
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