using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ClinicMS.Models;

namespace ClinicMS.Services.PdfExport;

public class PdfExportService
{
    private readonly string _tempFolder;

    public PdfExportService()
    {
        _tempFolder = Path.Combine(Path.GetTempPath(), "ClinicMS", "Exports");
        if (!Directory.Exists(_tempFolder)) Directory.CreateDirectory(_tempFolder);
    }

    public string ExportPrescription(Visit visit)
    {
        var fileName = $"DonThuoc_{visit.Patient?.FullName ?? visit.PatientId.ToString()}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
        var filePath = Path.Combine(_tempFolder, fileName);

        var document = new PrescriptionDocument(visit);
        document.GeneratePdf(filePath);

        return filePath;
    }

    public string ExportMedicalRecord(Visit visit)
    {
        var fileName = $"BenhAn_{visit.Patient?.FullName ?? visit.PatientId.ToString()}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
        var filePath = Path.Combine(_tempFolder, fileName);

        var document = new MedicalRecordDocument(visit);
        document.GeneratePdf(filePath);

        return filePath;
    }

    public string ExportInvoice(Invoice invoice)
    {
        var fileName = $"HoaDon_{invoice.VisitId}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
        var filePath = Path.Combine(_tempFolder, fileName);

        var items = invoice.Items ?? new List<InvoiceItem>();
        var document = new InvoiceDocument(invoice, items, "HÓA ĐƠN THANH TOÁN");
        document.GeneratePdf(filePath);

        return filePath;
    }

    public string ExportMedicineInvoice(Invoice invoice)
    {
        var fileName = $"HoaDonThuoc_{invoice.VisitId}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
        var filePath = Path.Combine(_tempFolder, fileName);

        var items = invoice.Items?.Where(i => i.ItemType == "Medicine").ToList() ?? new List<InvoiceItem>();
        var document = new InvoiceDocument(invoice, items, "HÓA ĐƠN THUỐC");
        document.GeneratePdf(filePath);

        return filePath;
    }

    public string ExportServiceInvoice(Invoice invoice)
    {
        var fileName = $"HoaDonDichVu_{invoice.VisitId}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
        var filePath = Path.Combine(_tempFolder, fileName);

        var items = invoice.Items?.Where(i => i.ItemType == "Service").ToList() ?? new List<InvoiceItem>();
        var document = new InvoiceDocument(invoice, items, "HÓA ĐƠN DỊCH VỤ");
        document.GeneratePdf(filePath);

        return filePath;
    }

    private void OpenFile(string filePath)
    {
        try
        {
            Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Không thể mở file PDF sau khi xuất: {FilePath}", filePath);
        }
    }
}

// ═══════════════════════════════════════════════
//  DOCUMENT TEMPLATES
// ═══════════════════════════════════════════════

public class PrescriptionDocument : IDocument
{
    private readonly Visit _visit;
    public PrescriptionDocument(Visit visit) => _visit = visit;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A5);
            page.Margin(1, Unit.Centimetre);
            page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

            page.Header().Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("PHÒNG KHÁM ĐA KHOA CLINICMS").FontSize(14).SemiBold().FontColor(Colors.Green.Medium);
                    col.Item().Text("Địa chỉ: 123 Đường ABC, Quận XYZ, TP.HCM").FontSize(9);
                    col.Item().Text("Điện thoại: 0123.456.789").FontSize(9);
                });
            });

            page.Content().PaddingVertical(10).Column(col =>
            {
                col.Item().AlignCenter().Text("ĐƠN THUỐC").FontSize(18).ExtraBold();
                col.Item().PaddingTop(10).Row(row =>
                {
                    row.RelativeItem().Text(t => { t.Span("Họ tên: "); t.Span(_visit.Patient?.FullName ?? "N/A").SemiBold(); });
                    
                    int age = 0;
                    if (DateTime.TryParse(_visit.Patient?.DateOfBirth, out var dob)) {
                        age = DateTime.Now.Year - dob.Year;
                        if (DateTime.Now.DayOfYear < dob.DayOfYear) age--;
                    }
                    row.ConstantItem(80).Text(t => { t.Span("Tuổi: "); t.Span(age > 0 ? age.ToString() : "N/A"); });
                });
                col.Item().Text(t => { t.Span("Địa chỉ: "); t.Span(_visit.Patient?.Address ?? "N/A"); });
                col.Item().PaddingTop(5).Text(t => { t.Span("Chẩn đoán: "); t.Span(_visit.Diagnosis ?? "Chưa có chẩn đoán").Italic(); });

                col.Item().PaddingTop(15).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(25);
                        columns.RelativeColumn();
                        columns.ConstantColumn(60);
                        columns.ConstantColumn(40);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(CellStyle).Text("STT");
                        header.Cell().Element(CellStyle).Text("Tên thuốc");
                        header.Cell().Element(CellStyle).Text("Số lượng");
                        header.Cell().Element(CellStyle).Text("ĐVT");
                        static IContainer CellStyle(IContainer container) => container.BorderBottom(1).PaddingVertical(5).AlignCenter();
                    });

                    int i = 1;
                    var details = _visit.PrescriptionDetails ?? new List<PrescriptionDetail>();
                    foreach (var d in details)
                    {
                        table.Cell().Element(RowStyle).Text(i++.ToString());
                        table.Cell().Element(RowStyle).Column(c => {
                            c.Item().Text(d.Medicine?.MedicineName ?? "Thuốc không tên").SemiBold();
                            if (!string.IsNullOrEmpty(d.Dosage)) c.Item().Text(d.Dosage).FontSize(9).Italic();
                        });
                        table.Cell().Element(RowStyle).AlignCenter().Text(d.Quantity.ToString());
                        table.Cell().Element(RowStyle).AlignCenter().Text(d.Medicine?.Unit ?? "");
                        static IContainer RowStyle(IContainer container) => container.PaddingVertical(5);
                    }
                });
            });

            page.Footer().PaddingTop(20).Row(row =>
            {
                row.RelativeItem().Column(c => {
                    c.Item().Text($"Ngày khám: {_visit.VisitTime ?? DateTime.Now.ToString("dd/MM/yyyy")}");
                });
                row.ConstantItem(150).AlignCenter().Column(c => {
                    c.Item().Text("Bác sĩ điều trị").SemiBold();
                    c.Item().PaddingTop(40).Text(_visit.Doctor?.FullName ?? "........................");
                });
            });
        });
    }
}

public class MedicalRecordDocument : IDocument
{
    private readonly Visit _visit;
    public MedicalRecordDocument(Visit visit) => _visit = visit;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(2, Unit.Centimetre);
            page.DefaultTextStyle(x => x.FontSize(12).FontFamily("Arial"));

            page.Header().Text("PHÒNG KHÁM ĐA KHOA CLINICMS").FontSize(14).SemiBold().FontColor(Colors.Green.Medium);

            page.Content().PaddingVertical(20).Column(col =>
            {
                col.Item().AlignCenter().Text("PHIẾU KHÁM BỆNH").FontSize(20).ExtraBold();
                col.Item().PaddingTop(20).Row(row =>
                {
                    row.RelativeItem().Text(t => { t.Span("Bệnh nhân: "); t.Span(_visit.Patient?.FullName ?? "N/A").Bold(); });
                    row.ConstantItem(100).Text(t => { t.Span("Mã BN: "); t.Span(_visit.PatientId.ToString()); });
                });
                col.Item().Text(t => { t.Span("Ngày sinh: "); t.Span(_visit.Patient?.DateOfBirth ?? "N/A"); });
                
                col.Item().PaddingTop(20).Text("1. LÝ DO KHÁM / TRIỆU CHỨNG").SemiBold();
                col.Item().PaddingLeft(10).Text(_visit.Symptoms ?? "N/A");

                col.Item().PaddingTop(10).Text("2. CHẨN ĐOÁN").SemiBold();
                col.Item().PaddingLeft(10).Text(_visit.Diagnosis ?? "Chưa xác định");

                col.Item().PaddingTop(10).Text("3. LỜI DẶN BÁC SĨ").SemiBold();
                col.Item().PaddingLeft(10).Text(_visit.Note ?? "N/A");
            });

            page.Footer().AlignCenter().Text(x => {
                x.Span("Trang ");
                x.CurrentPageNumber();
            });
        });
    }
}

public class InvoiceDocument : IDocument
{
    private readonly Invoice _invoice;
    private readonly List<InvoiceItem> _items;
    private readonly string _title;

    public InvoiceDocument(Invoice invoice, List<InvoiceItem> items, string title)
    {
        _invoice = invoice;
        _items = items;
        _title = title;
    }

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A5);
            page.Margin(1, Unit.Centimetre);
            page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

            page.Header().Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("PHÒNG KHÁM ĐA KHOA CLINICMS").FontSize(12).SemiBold().FontColor(Colors.Green.Medium);
                    col.Item().Text(_title).FontSize(14).ExtraBold();
                });
                row.ConstantItem(80).Text($"Mã: INV-{_invoice.VisitId}").FontSize(8).Italic();
            });

            page.Content().PaddingVertical(10).Column(col =>
            {
                col.Item().Row(row =>
                {
                    row.RelativeItem().Text(t => { t.Span("Bệnh nhân: "); t.Span(_invoice.Visit?.Patient?.FullName ?? "N/A").Bold(); });
                    row.ConstantItem(100).AlignRight().Text(_invoice.CreatedAt ?? DateTime.Now.ToString("dd/MM/yyyy"));
                });
                
                col.Item().PaddingTop(10).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn();
                        columns.ConstantColumn(30);
                        columns.ConstantColumn(60);
                        columns.ConstantColumn(70);
                    });

                    table.Header(header =>
                    {
                        header.Cell().BorderBottom(1).Text("Nội dung");
                        header.Cell().BorderBottom(1).AlignCenter().Text("SL");
                        header.Cell().BorderBottom(1).AlignRight().Text("Đơn giá");
                        header.Cell().BorderBottom(1).AlignRight().Text("Thành tiền");
                    });

                    foreach(var item in _items)
                    {
                        table.Cell().PaddingVertical(2).Text(item.ItemName);
                        table.Cell().PaddingVertical(2).AlignCenter().Text(item.Quantity.ToString());
                        table.Cell().PaddingVertical(2).AlignRight().Text(item.UnitPrice.ToString("N0"));
                        table.Cell().PaddingVertical(2).AlignRight().Text(item.Subtotal.ToString("N0"));
                    }

                    table.Cell().ColumnSpan(3).PaddingTop(10).Text("TỔNG CỘNG").Bold();
                    table.Cell().PaddingTop(10).AlignRight().Text(_items.Sum(i => i.Subtotal).ToString("N0") + " đ").Bold();
                });

                col.Item().PaddingTop(20).Row(row =>
                {
                    row.RelativeItem().Column(c => {
                        c.Item().Text("Lịch sử thanh toán:").Italic().FontSize(9);
                        foreach(var p in _invoice.Payments ?? new List<Payment>())
                        {
                            c.Item().Text($"- {p.PaymentTime}: {p.AmountDisplay} ({p.PaymentMethod})").FontSize(8);
                        }
                    });
                    row.RelativeItem().AlignCenter().Column(c => {
                        c.Item().PaddingTop(10).Text(_invoice.Status == "Paid" ? "ĐÃ THANH TOÁN" : "CHƯA THANH TOÁN").FontSize(12).Bold().FontColor(_invoice.Status == "Paid" ? Colors.Green.Medium : Colors.Red.Medium);
                    });
                });
            });

            page.Footer().AlignCenter().Text(t => {
                t.Span("Cảm ơn quý khách đã tin tưởng ClinicMS!").Italic().FontSize(8);
            });
        });
    }
}
