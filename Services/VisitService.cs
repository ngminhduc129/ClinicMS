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
    private readonly AuditLogService _auditLog;

    public VisitService(CoreDatabaseService core, AuditLogService auditLog)
    {
        _core = core;
        _auditLog = auditLog;
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
                       .Include(v => v.PrescriptionDetails!)
                           .ThenInclude(pd => pd.Medicine)
                       .Include(v => v.ServiceOrders!)
                           .ThenInclude(so => so.Service)
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

            // Tách riêng đơn thuốc và dịch vụ để tránh lỗi tracking navigation property
            var details = v.PrescriptionDetails?.ToList() ?? new List<PrescriptionDetail>();
            v.PrescriptionDetails = new List<PrescriptionDetail>();
            var orders = v.ServiceOrders?.ToList() ?? new List<ServiceOrder>();
            v.ServiceOrders = new List<ServiceOrder>();

            db.Visits.Add(v);
            await db.SaveChangesAsync();

            if (details.Any())
            {
                foreach (var d in details)
                {
                    db.PrescriptionDetails.Add(new PrescriptionDetail 
                    {
                        VisitId = v.VisitId,
                        MedicineId = d.MedicineId,
                        Quantity = d.Quantity,
                        Dosage = d.Dosage
                    });
                }
            }

            if (orders.Any())
            {
                foreach (var o in orders)
                {
                    db.ServiceOrders.Add(new ServiceOrder
                    {
                        VisitId = v.VisitId,
                        ServiceId = o.ServiceId,
                        Result = o.Result
                    });
                }
            }
            await db.SaveChangesAsync();

            _auditLog.Log(db, "Visit", v.VisitId.ToString(), "Create",
                newValues: AuditLogService.Serialize(new { v.PatientId, v.DoctorId, v.Status, v.Symptoms, v.Diagnosis }),
                details: $"Thêm hồ sơ khám #{v.VisitId}");
            await db.SaveChangesAsync();

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

            // Cập nhật các trường scalar (shallow update)
            db.Entry(existingVisit).CurrentValues.SetValues(v);

            // Xử lý đơn thuốc: Xóa cũ, thêm mới
            var oldDetails = await db.PrescriptionDetails.Where(pd => pd.VisitId == v.VisitId).ToListAsync();
            db.PrescriptionDetails.RemoveRange(oldDetails);
            
            // Xử lý dịch vụ chỉ định: Xóa cũ, thêm mới
            var oldOrders = await db.ServiceOrders.Where(so => so.VisitId == v.VisitId).ToListAsync();
            db.ServiceOrders.RemoveRange(oldOrders);
            await db.SaveChangesAsync(); // Lưu bước xóa để tránh trùng khóa chính khi thêm lại

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

            if (v.ServiceOrders != null && v.ServiceOrders.Any())
            {
                foreach (var order in v.ServiceOrders)
                {
                    db.ServiceOrders.Add(new ServiceOrder
                    {
                        VisitId = v.VisitId,
                        ServiceId = order.ServiceId,
                        Result = order.Result
                    });
                }
            }

            _auditLog.Log(db, "Visit", v.VisitId.ToString(), "Update",
                details: $"Cập nhật hồ sơ khám #{v.VisitId} - {v.Status}");
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

            _auditLog.Log(db, "Visit", id.ToString(), "Delete",
                details: $"Xóa hồ sơ khám #{id}");
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

    public async Task<List<ServiceOrder>> GetServiceOrdersAsync(int visitId)
    {
        await using var db = _core.Ctx();
        return await db.ServiceOrders
            .Include(so => so.Service)
            .Where(so => so.VisitId == visitId)
            .ToListAsync();
    }

    public async Task SaveServiceOrdersAsync(int visitId, List<ServiceOrder> items)
    {
        await using var db = _core.Ctx();
        await using var trans = await db.Database.BeginTransactionAsync();
        try
        {
            var oldOrders = await db.ServiceOrders.Where(so => so.VisitId == visitId).ToListAsync();
            db.ServiceOrders.RemoveRange(oldOrders);
            
            foreach (var item in items)
            {
                db.ServiceOrders.Add(new ServiceOrder
                {
                    VisitId = visitId,
                    ServiceId = item.ServiceId,
                    Result = item.Result
                });
            }

            _auditLog.Log(db, "ServiceOrder", visitId.ToString(), "Update",
                details: $"Cập nhật dịch vụ chỉ định cho hồ sơ khám #{visitId}");
            await db.SaveChangesAsync();
            await trans.CommitAsync();
        }
        catch { await trans.RollbackAsync(); throw; }
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

            _auditLog.Log(db, "PrescriptionDetail", visitId.ToString(), "Update",
                details: $"Cập nhật đơn thuốc cho hồ sơ khám #{visitId}");
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
