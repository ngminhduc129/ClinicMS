using Microsoft.EntityFrameworkCore;
using ClinicMS.Data;
using ClinicMS.Models;

namespace ClinicMS.Services;

public class DatabaseService
{
    public async Task<List<Appointment>> GetUpcomingAppointmentsAsync(int limit = 10)
    {
        await using var db = Ctx();
        var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        return await db.Appointments
                       .Include(a => a.Patient)
                       .Include(a => a.Doctor)
                       .Where(a => a.AppointmentTime.CompareTo(now) >= 0 && a.Status == "Scheduled")
                       .OrderBy(a => a.AppointmentTime)
                       .Take(limit)
                       .ToListAsync();
    }

    public async Task<Dictionary<string, int>> GetVisitCountByDayAsync(int daysBack = 7)
    {
        await using var db = Ctx();
        var startDate = DateTime.Today.AddDays(-daysBack + 1).ToString("yyyy-MM-dd");

        var list = await db.Visits
                           .Where(v => v.PatientId > 0 && v.VisitTime != null && v.VisitTime.CompareTo(startDate) >= 0)
                           .ToListAsync();

        return list.GroupBy(v => v.VisitTime?.Length >= 10 ? v.VisitTime.Substring(0, 10) : "")
                   .Where(g => !string.IsNullOrEmpty(g.Key))
                   .ToDictionary(g => g.Key, g => g.Count());
    }

    public static void Initialize()
    {
        using var db = new ClinicDbContext();
        db.Database.Migrate();
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

    public async Task<Dictionary<string, int>> GetPatientsLookupAsync()
    {
        await using var db = Ctx();
        var list = await db.Patients
            .Select(p => new { p.PatientId, p.FullName })
            .OrderBy(p => p.FullName)
            .ToListAsync();

        var dict = new Dictionary<string, int>();
        foreach (var p in list)
        {
            var key = $"{p.FullName} (ID: {p.PatientId})";
            dict[key] = p.PatientId;
        }
        return dict;
    }

    public async Task<Dictionary<string, int>> GetDoctorsLookupAsync()
    {
        await using var db = Ctx();
        var list = await db.Doctors
            .Select(d => new { d.DoctorId, d.FullName })
            .OrderBy(d => d.FullName)
            .ToListAsync();

        var dict = new Dictionary<string, int>();
        foreach (var d in list)
        {
            var key = $"{d.FullName} (ID: {d.DoctorId})";
            dict[key] = d.DoctorId;
        }
        return dict;
    }

    public async Task<List<Patient>> GetPatientsWithContactsAsync(int offset = 0, int limit = 50)
    {
        await using var db = Ctx();
        var query = from p in db.Patients
                    join pc in db.PatientContacts.Where(x => x.IsPrimary == 1) on p.PatientId equals pc.PatientId into contacts
                    from subPc in contacts.DefaultIfEmpty()
                    join c in db.Contacts on (subPc != null ? subPc.ContactId : -1) equals c.ContactId into finalContacts
                    from subC in finalContacts.DefaultIfEmpty()
                    orderby p.FullName ascending
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

    public async Task<List<Patient>> SearchPatientsAsync(string searchTerm, int offset = 0, int limit = 50)
    {
        await using var db = Ctx();
        var query = from p in db.Patients
                    join pc in db.PatientContacts.Where(x => x.IsPrimary == 1) on p.PatientId equals pc.PatientId into contacts
                    from subPc in contacts.DefaultIfEmpty()
                    join c in db.Contacts on (subPc != null ? subPc.ContactId : -1) equals c.ContactId into finalContacts
                    from subC in finalContacts.DefaultIfEmpty()
                    where string.IsNullOrEmpty(searchTerm) ||
                          p.FullName.ToLower().Contains(searchTerm.ToLower()) ||
                          (subC != null && subC.Phone != null && subC.Phone.Contains(searchTerm))
                    orderby p.FullName
                    select new { p, subC };

        var results = await query.Skip(offset).Take(limit).ToListAsync();
        var list = new List<Patient>();
        foreach (var item in results)
        {
            var p = item.p;
            if (item.subC != null) { p.Phone = item.subC.Phone; p.Email = item.subC.Email; p.Address = item.subC.Address; }
            list.Add(p);
        }
        return list;
    }

    public async Task<int> GetPatientsSearchCountAsync(string searchTerm)
    {
        await using var db = Ctx();
        var term = (searchTerm ?? "").ToLower();
        return await (from p in db.Patients
                      join pc in db.PatientContacts.Where(x => x.IsPrimary == 1) on p.PatientId equals pc.PatientId into contacts
                      from subPc in contacts.DefaultIfEmpty()
                      join c in db.Contacts on (subPc != null ? subPc.ContactId : -1) equals c.ContactId into finalContacts
                      from subC in finalContacts.DefaultIfEmpty()
                      where string.IsNullOrEmpty(term) ||
                            p.FullName.ToLower().Contains(term) ||
                            (subC != null && subC.Phone != null && subC.Phone.Contains(term))
                      select p).CountAsync();
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
        var contact = new Contact
        {
            Phone = p.Phone,
            Email = p.Email,
            Address = p.Address
        };
        db.Contacts.Add(contact);
        await db.SaveChangesAsync();

        p.Phone = null; p.Email = null; p.Address = null;
        if (string.IsNullOrEmpty(p.CreatedAt)) p.CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
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

    public async Task<(Patient? patient, Contact? contact)> GetPatientWithContactAsync(int id)
    {
        await using var db = Ctx();
        var p = await db.Patients.FindAsync(id);
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

        var old = await db.PrescriptionDetails.Where(pd => pd.VisitId == visitId).ToListAsync();
        if (old.Any()) db.PrescriptionDetails.RemoveRange(old);
        await db.SaveChangesAsync();

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
    public async Task<int> GetDoctorsCountAsync()
    {
        await using var db = Ctx();
        return await db.Doctors.CountAsync();
    }

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
    public async Task<List<Visit>> GetTodayVisitsAsync()
    {
        await using var db = Ctx();
        var today1 = DateTime.Today.ToString("yyyy-MM-dd");
        var today2 = DateTime.Today.ToString("dd/MM/yyyy");
        return await db.Visits
                       .Include(v => v.Patient)
                       .Include(v => v.Doctor)
                       .Where(v => v.PatientId > 0 && v.VisitTime != null && (v.VisitTime.StartsWith(today1) || v.VisitTime.StartsWith(today2)))
                       .OrderByDescending(v => v.VisitTime)
                       .ToListAsync();
    }

    public async Task<List<Visit>> GetVisitsAsync(int offset = 0, int limit = 50, string? dateFilter = null)
    {
        await using var db = Ctx();
        var query = db.Visits
                       .Include(v => v.Patient)
                       .Include(v => v.Doctor)
                       .AsQueryable();

        if (!string.IsNullOrEmpty(dateFilter))
        {
            query = query.Where(v => v.VisitTime != null && v.VisitTime.StartsWith(dateFilter));
        }

        return await query.OrderByDescending(v => v.VisitTime)
                       .Skip(offset)
                       .Take(limit)
                       .ToListAsync();
    }

    public async Task<List<Visit>> SearchVisitsAsync(string searchTerm, int offset = 0, int limit = 50, string? dateFilter = null)
    {
        await using var db = Ctx();
        var term = (searchTerm ?? "").ToLower();
        var query = db.Visits
                       .Include(v => v.Patient)
                       .Include(v => v.Doctor)
                       .AsQueryable();

        if (!string.IsNullOrEmpty(dateFilter))
        {
            query = query.Where(v => v.VisitTime != null && v.VisitTime.StartsWith(dateFilter));
        }

        if (!string.IsNullOrEmpty(term))
        {
            query = query.Where(v => (v.Patient != null && v.Patient.FullName.ToLower().Contains(term)) ||
                                     (v.Doctor != null && v.Doctor.FullName.ToLower().Contains(term)) ||
                                     (v.Diagnosis != null && v.Diagnosis.ToLower().Contains(term)));
        }

        return await query.OrderByDescending(v => v.VisitTime)
                       .Skip(offset)
                       .Take(limit)
                       .ToListAsync();
    }

    public async Task<int> GetVisitsSearchCountAsync(string searchTerm, string? dateFilter = null)
    {
        await using var db = Ctx();
        var term = (searchTerm ?? "").ToLower();
        var query = db.Visits.AsQueryable();

        if (!string.IsNullOrEmpty(dateFilter))
        {
            query = query.Where(v => v.VisitTime != null && v.VisitTime.StartsWith(dateFilter));
        }

        if (!string.IsNullOrEmpty(term))
        {
            query = query.Where(v => (v.Patient != null && v.Patient.FullName.ToLower().Contains(term)) ||
                                     (v.Doctor != null && v.Doctor.FullName.ToLower().Contains(term)) ||
                                     (v.Diagnosis != null && v.Diagnosis.ToLower().Contains(term)));
        }

        return await query.CountAsync();
    }

    public async Task<int> GetVisitsCountAsync()
    {
        await using var db = Ctx();
        return await db.Visits.CountAsync();
    }

    public async Task<Visit?> GetVisitDetailsAsync(int visitId)
    {
        await using var db = Ctx();
        return await db.Visits
                       .Include(v => v.Patient)
                       .Include(v => v.Doctor)
                       .Include(v => v.PrescriptionDetails!)
                           .ThenInclude(pd => pd.Medicine)
                       .FirstOrDefaultAsync(v => v.VisitId == visitId);
    }

    public async Task<int> GetTodayVisitsCountAsync()
    {
        await using var db = Ctx();
        var today1 = DateTime.Today.ToString("yyyy-MM-dd");
        var today2 = DateTime.Today.ToString("dd/MM/yyyy");
        return await db.Visits.CountAsync(v => v.PatientId > 0 && v.VisitTime != null && (v.VisitTime.StartsWith(today1) || v.VisitTime.StartsWith(today2)));
    }

    public async Task<Visit> AddVisitAsync(Visit v)
    {
        await using var db = Ctx();
        await using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            if (string.IsNullOrEmpty(v.VisitTime))
                v.VisitTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            var details = v.PrescriptionDetails?.ToList() ?? new List<PrescriptionDetail>();
            v.PrescriptionDetails = new List<PrescriptionDetail>();

            db.Visits.Add(v);
            await db.SaveChangesAsync();

            if (details.Any())
            {
                foreach (var detail in details)
                {
                    db.PrescriptionDetails.Add(new PrescriptionDetail
                    {
                        VisitId = v.VisitId,
                        MedicineId = detail.MedicineId,
                        Quantity = detail.Quantity,
                        Dosage = detail.Dosage
                    });
                }
                await db.SaveChangesAsync();
            }

            await transaction.CommitAsync();
            return v;
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task UpdateVisitAsync(Visit v)
    {
        await using var db = Ctx();
        await using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            var existingVisit = await db.Visits
                .FirstOrDefaultAsync(x => x.VisitId == v.VisitId);

            if (existingVisit == null)
                throw new Exception($"Không tìm thấy hồ sơ khám ID: {v.VisitId}");

            db.Entry(existingVisit).CurrentValues.SetValues(v);

            var oldDetails = await db.PrescriptionDetails
                .Where(pd => pd.VisitId == v.VisitId)
                .ToListAsync();

            if (oldDetails.Any())
                db.PrescriptionDetails.RemoveRange(oldDetails);

            await db.SaveChangesAsync();

            if (v.PrescriptionDetails != null && v.PrescriptionDetails.Any())
            {
                foreach (var detail in v.PrescriptionDetails)
                {
                    db.PrescriptionDetails.Add(new PrescriptionDetail
                    {
                        VisitId = v.VisitId,
                        MedicineId = detail.MedicineId,
                        Quantity = detail.Quantity,
                        Dosage = detail.Dosage
                    });
                }
            }

            await db.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
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
    public async Task<List<Invoice>> GetInvoicesAsync(int offset = 0, int limit = 1000)
    {
        await using var db = Ctx();
        return await db.Invoices
                       .Include(i => i.Visit).ThenInclude(v => v!.Patient)
                       .Include(i => i.Visit).ThenInclude(v => v!.Doctor)
                       .Include(i => i.Payments)
                       .OrderByDescending(i => i.CreatedAt)
                       .Skip(offset)
                       .Take(limit)
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

        var payments = db.Payments.Where(p => visitIds.Contains(p.VisitId));
        db.Payments.RemoveRange(payments);

        var invoiceItems = db.InvoiceItems.Where(ii => visitIds.Contains(ii.VisitId));
        db.InvoiceItems.RemoveRange(invoiceItems);

        var invoices = db.Invoices.Where(i => visitIds.Contains(i.VisitId));
        db.Invoices.RemoveRange(invoices);

        var prescriptionDetails = db.PrescriptionDetails.Where(pd => visitIds.Contains(pd.VisitId));
        db.PrescriptionDetails.RemoveRange(prescriptionDetails);

        var prescriptions = db.Prescriptions.Where(p => visitIds.Contains(p.VisitId));
        db.Prescriptions.RemoveRange(prescriptions);

        var serviceOrders = db.ServiceOrders.Where(so => visitIds.Contains(so.VisitId));
        db.ServiceOrders.RemoveRange(serviceOrders);

        await db.SaveChangesAsync();
    }
}
