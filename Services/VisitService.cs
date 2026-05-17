using Microsoft.EntityFrameworkCore;
using ClinicMS.Models;
using ClinicMS.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ClinicMS.Services;

public class VisitService
{
    private readonly CoreDatabaseService _core;

    public VisitService(CoreDatabaseService core)
    {
        _core = core;
    }

    public async Task<List<Visit>> GetTodayVisitsAsync()
    {
        await using var db = _core.Ctx();
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
        await using var db = _core.Ctx();
        var query = db.Visits
                       .Include(v => v.Patient)
                       .Include(v => v.Doctor)
                       .AsQueryable();

        if (!string.IsNullOrEmpty(dateFilter))
            query = query.Where(v => v.VisitTime != null && v.VisitTime.StartsWith(dateFilter));

        return await query.OrderByDescending(v => v.VisitTime)
                           .Skip(offset).Take(limit)
                           .ToListAsync();
    }

    public async Task<List<Visit>> SearchVisitsAsync(string searchTerm, int offset = 0, int limit = 50, string? dateFilter = null)
    {
        await using var db = _core.Ctx();
        var term = searchTerm.ToLower();
        var query = db.Visits
                       .Include(v => v.Patient)
                       .Include(v => v.Doctor)
                       .AsQueryable();

        if (!string.IsNullOrEmpty(dateFilter))
            query = query.Where(v => v.VisitTime != null && v.VisitTime.StartsWith(dateFilter));

        if (!string.IsNullOrEmpty(term))
            query = query.Where(v => (v.Patient != null && v.Patient.FullName.ToLower().Contains(term)) ||
                                     (v.Doctor != null && v.Doctor.FullName.ToLower().Contains(term)) ||
                                     (v.Diagnosis != null && v.Diagnosis.ToLower().Contains(term)));

        return await query.OrderByDescending(v => v.VisitTime)
                           .Skip(offset).Take(limit)
                           .ToListAsync();
    }

    public async Task<int> GetVisitsSearchCountAsync(string searchTerm, string? dateFilter = null)
    {
        await using var db = _core.Ctx();
        var query = db.Visits.AsQueryable();

        if (!string.IsNullOrEmpty(dateFilter))
            query = query.Where(v => v.VisitTime != null && v.VisitTime.StartsWith(dateFilter));

        if (!string.IsNullOrEmpty(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(v => (v.Patient != null && v.Patient.FullName.ToLower().Contains(term)) ||
                                     (v.Doctor != null && v.Doctor.FullName.ToLower().Contains(term)) ||
                                     (v.Diagnosis != null && v.Diagnosis.ToLower().Contains(term)));
        }

        return await query.CountAsync();
    }

    public async Task<int> GetVisitsCountAsync()
    {
        await using var db = _core.Ctx();
        return await db.Visits.CountAsync();
    }

    public async Task<Visit?> GetVisitDetailsAsync(int visitId)
    {
        await using var db = _core.Ctx();
        return await db.Visits
                       .Include(v => v.Patient)
                       .Include(v => v.Doctor)
                       .FirstOrDefaultAsync(v => v.VisitId == visitId);
    }

    public async Task<int> GetTodayVisitsCountAsync()
    {
        await using var db = _core.Ctx();
        var today1 = DateTime.Today.ToString("yyyy-MM-dd");
        var today2 = DateTime.Today.ToString("dd/MM/yyyy");
        return await db.Visits.CountAsync(v => v.PatientId > 0 && v.VisitTime != null && (v.VisitTime.StartsWith(today1) || v.VisitTime.StartsWith(today2)));
    }

    public async Task<Visit> AddVisitAsync(Visit v)
    {
        await using var db = _core.Ctx();
        await using var trans = await db.Database.BeginTransactionAsync();
        try
        {
            if (string.IsNullOrEmpty(v.VisitTime))
                v.VisitTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            db.Visits.Add(v);
            await db.SaveChangesAsync();

            if (v.PrescriptionDetails.Any())
            {
                foreach (var detail in v.PrescriptionDetails)
                    detail.VisitId = v.VisitId;
                db.PrescriptionDetails.AddRange(v.PrescriptionDetails);
                await db.SaveChangesAsync();
            }

            await trans.CommitAsync();
            return v;
        }
        catch { await trans.RollbackAsync(); throw; }
    }

    public async Task UpdateVisitAsync(Visit v)
    {
        await using var db = _core.Ctx();
        await using var trans = await db.Database.BeginTransactionAsync();
        try
        {
            var existingVisit = await db.Visits.FirstOrDefaultAsync(x => x.VisitId == v.VisitId);
            if (existingVisit == null) throw new Exception($"Không tìm thấy hồ sơ khám ID: {v.VisitId}");

            db.Entry(existingVisit).CurrentValues.SetValues(v);

            var oldDetails = await db.PrescriptionDetails.Where(pd => pd.VisitId == v.VisitId).ToListAsync();
            db.PrescriptionDetails.RemoveRange(oldDetails);

            if (v.PrescriptionDetails.Any())
            {
                foreach (var detail in v.PrescriptionDetails)
                {
                    detail.VisitId = v.VisitId;
                    db.PrescriptionDetails.Add(detail);
                }
            }

            await db.SaveChangesAsync();
            await trans.CommitAsync();
        }
        catch { await trans.RollbackAsync(); throw; }
    }

    public async Task DeleteVisitAsync(int id)
    {
        await using var db = _core.Ctx();
        var v = await db.Visits.FindAsync(id);
        if (v != null)
        {
            await DeleteVisitRelatedDataAsync(db, new List<int> { id });
            db.Visits.Remove(v);
            await db.SaveChangesAsync();
        }
    }

    public async Task<List<PrescriptionDetail>> GetPrescriptionDetailsAsync(int visitId)
    {
        await using var db = _core.Ctx();
        return await db.PrescriptionDetails
            .Include(pd => pd.Medicine)
            .Where(pd => pd.VisitId == visitId)
            .ToListAsync();
    }

    public async Task SavePrescriptionDetailsAsync(int visitId, List<PrescriptionDetail> items)
    {
        await using var db = _core.Ctx();
        await using var trans = await db.Database.BeginTransactionAsync();
        try
        {
            // 1. Lấy đơn cũ để tính delta reserved_quantity
            var oldItems = await db.PrescriptionDetails.Where(pd => pd.VisitId == visitId).ToListAsync();
            
            // 2. Gom nhóm theo MedicineId để xử lý delta
            var deltaMap = new Dictionary<int, double>();
            foreach (var o in oldItems) deltaMap[o.MedicineId] = -o.Quantity;
            foreach (var n in items)
            {
                if (deltaMap.ContainsKey(n.MedicineId)) deltaMap[n.MedicineId] += n.Quantity;
                else deltaMap[n.MedicineId] = n.Quantity;
            }

            // 3. Cập nhật ReservedQuantity trong bảng medicines
            foreach (var kvp in deltaMap)
            {
                if (kvp.Value == 0) continue;
                var med = await db.Medicines.FindAsync(kvp.Key);
                if (med != null)
                {
                    med.ReservedQuantity += (int)kvp.Value;
                    db.Medicines.Update(med);
                }
            }

            // 4. Thay thế đơn cũ bằng đơn mới
            db.PrescriptionDetails.RemoveRange(oldItems);
            foreach (var item in items)
            {
                db.PrescriptionDetails.Add(new PrescriptionDetail
                {
                    VisitId = visitId,
                    MedicineId = item.MedicineId,
                    Quantity = item.Quantity,
                    Dosage = item.Dosage
                });
            }

            await db.SaveChangesAsync();
            await trans.CommitAsync();
        }
        catch { await trans.RollbackAsync(); throw; }
    }

    private static async Task DeleteVisitRelatedDataAsync(ClinicDbContext db, List<int> visitIds)
    {
        if (visitIds.Count == 0) return;
        
        // Hoàn trả ReservedQuantity trước khi xóa đơn thuốc
        var pds = await db.PrescriptionDetails.Where(pd => visitIds.Contains(pd.VisitId)).ToListAsync();
        foreach (var pd in pds)
        {
            var med = await db.Medicines.FindAsync(pd.MedicineId);
            if (med != null)
            {
                med.ReservedQuantity -= (int)pd.Quantity;
                db.Medicines.Update(med);
            }
        }

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
