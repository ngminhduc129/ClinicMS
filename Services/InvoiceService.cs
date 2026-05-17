using Microsoft.EntityFrameworkCore;
using ClinicMS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ClinicMS.Services;

public class InvoiceService
{
    private readonly CoreDatabaseService _core;

    public InvoiceService(CoreDatabaseService core)
    {
        _core = core;
    }

    public async Task<List<Invoice>> GetInvoicesAsync(int offset = 0, int limit = 1000)
    {
        await using var db = _core.Ctx();
        return await db.Invoices
                       .Include(i => i.Visit).ThenInclude(v => v!.Patient)
                       .Include(i => i.Visit).ThenInclude(v => v!.Doctor)
                       .Include(i => i.Items)
                       .Include(i => i.Payments)
                       .OrderByDescending(i => i.VisitId)
                       .Skip(offset).Take(limit)
                       .ToListAsync();
    }

    public async Task<Invoice?> GetInvoiceByVisitIdAsync(int visitId)
    {
        await using var db = _core.Ctx();
        return await db.Invoices
                       .Include(i => i.Visit).ThenInclude(v => v!.Patient)
                       .Include(i => i.Visit).ThenInclude(v => v!.Doctor)
                       .Include(i => i.Items)
                       .Include(i => i.Payments)
                       .FirstOrDefaultAsync(i => i.VisitId == visitId);
    }

    public async Task GenerateOrUpdateInvoiceAsync(int visitId)
    {
        await using var db = _core.Ctx();
        
        // 1. Lấy thông tin Visit kèm thuốc và dịch vụ
        var visit = await db.Visits
                            .Include(v => v.PrescriptionDetails).ThenInclude(pd => pd.Medicine)
                            .Include(v => v.ServiceOrders).ThenInclude(so => so.Service)
                            .FirstOrDefaultAsync(v => v.VisitId == visitId);
        
        if (visit == null) return;

        // NGHIỆP VỤ: Chỉ sinh hóa đơn khi trạng thái là "Completed" (Đã khám)
        if (visit.Status != "Completed")
        {
            // Nếu hồ sơ chưa hoàn thành mà lỡ có hóa đơn "Chưa thanh toán" thì xóa đi để dọn rác cho Thu ngân
            var existingUnpaid = await db.Invoices
                                         .Include(i => i.Payments)
                                         .FirstOrDefaultAsync(i => i.VisitId == visitId);
            if (existingUnpaid != null && !existingUnpaid.Payments.Any() && existingUnpaid.Status == "Unpaid")
            {
                db.InvoiceItems.RemoveRange(db.InvoiceItems.Where(x => x.VisitId == visitId));
                db.Invoices.Remove(existingUnpaid);
                await db.SaveChangesAsync();
            }
            return;
        }

        // 2. Tìm hoặc tạo Invoice
        var invoice = await db.Invoices.Include(i => i.Items).FirstOrDefaultAsync(i => i.VisitId == visitId);
        bool isNew = false;
        if (invoice == null)
        {
            invoice = new Invoice { VisitId = visitId, Status = "Unpaid", CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") };
            isNew = true;
        }

        // 3. Xây dựng danh sách Item mới
        var newItems = new List<InvoiceItem>();
        
        // Thuốc
        foreach (var pd in visit.PrescriptionDetails)
        {
            newItems.Add(new InvoiceItem 
            { 
                VisitId = visitId, 
                ItemType = "Medicine", 
                ItemId = pd.MedicineId, 
                ItemName = pd.Medicine?.MedicineName ?? "Thuốc không tên",
                Quantity = (int)pd.Quantity, 
                UnitPrice = pd.Medicine?.Price ?? 0 
            });
        }
        
        // Dịch vụ
        foreach (var so in visit.ServiceOrders)
        {
            newItems.Add(new InvoiceItem 
            { 
                VisitId = visitId, 
                ItemType = "Service", 
                ItemId = so.ServiceId, 
                ItemName = so.Service?.ServiceName ?? "Dịch vụ không tên",
                Quantity = 1, 
                UnitPrice = so.Service?.Price ?? 0 
            });
        }

        // 4. Cập nhật Invoice
        invoice.TotalAmount = newItems.Sum(x => x.Subtotal);
        
        if (isNew) db.Invoices.Add(invoice);
        else 
        {
            // Xóa item cũ và add lại (đơn giản nhất để đồng bộ)
            var oldItems = db.InvoiceItems.Where(x => x.VisitId == visitId);
            db.InvoiceItems.RemoveRange(oldItems);
            db.Invoices.Update(invoice);
        }
        
        await db.SaveChangesAsync();
        db.InvoiceItems.AddRange(newItems);
        await db.SaveChangesAsync();
    }

    public async Task UpdateInvoiceStatusAsync(int visitId, string status)
    {
        await using var db = _core.Ctx();
        var inv = await db.Invoices.FindAsync(visitId);
        if (inv != null) { inv.Status = status; db.Invoices.Update(inv); await db.SaveChangesAsync(); }
    }

    public async Task AddPaymentAsync(Payment pay)
    {
        await using var db = _core.Ctx();
        await using var trans = await db.Database.BeginTransactionAsync();
        try
        {
            if (string.IsNullOrEmpty(pay.PaymentTime))
                pay.PaymentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            db.Payments.Add(pay);

            var inv = await db.Invoices.Include(i => i.Items).FirstOrDefaultAsync(i => i.VisitId == pay.VisitId);
            if (inv != null)
            {
                var paid = await db.Payments
                                   .Where(p => p.VisitId == pay.VisitId)
                                   .SumAsync(p => p.Amount) + pay.Amount;
                
                if (paid >= inv.TotalAmount) inv.Status = "Paid";
                else if (paid > 0) inv.Status = "Partial";

                // TRỪ TỒN KHO THỰC TẾ (Nếu chưa trừ)
                if (inv.IsDeducted == 0 && (inv.Status == "Paid" || inv.Status == "Partial"))
                {
                    foreach (var item in inv.Items.Where(x => x.ItemType == "Medicine"))
                    {
                        var med = await db.Medicines.FindAsync(item.ItemId);
                        if (med != null)
                        {
                            med.StockQuantity -= item.Quantity;
                            med.ReservedQuantity -= item.Quantity;
                            db.Medicines.Update(med);
                        }
                    }
                    inv.IsDeducted = 1;
                }
                
                db.Invoices.Update(inv);
            }
            await db.SaveChangesAsync();
            await trans.CommitAsync();
        }
        catch { await trans.RollbackAsync(); throw; }
    }
}
