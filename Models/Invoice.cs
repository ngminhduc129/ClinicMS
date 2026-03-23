using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicMS.Models;

[Table("invoices")]
public class Invoice
{
    [Key][Column("visit_id")]     public int     VisitId     { get; set; }
    [Column("total_amount")]      public double  TotalAmount { get; set; }
    [Column("status")]            public string  Status      { get; set; } = "Unpaid";
    [Column("created_at")]        public string? CreatedAt   { get; set; }

    public Visit?             Visit    { get; set; }
    public List<InvoiceItem>  Items    { get; set; } = new();
    public List<Payment>      Payments { get; set; } = new();

    [NotMapped] public string TotalDisplay  => TotalAmount.ToString("N0") + " ₫";
    [NotMapped] public string StatusDisplay => Status switch
    {
        "Unpaid"  => "Chưa thanh toán",
        "Paid"    => "Đã thanh toán",
        "Partial" => "Thanh toán 1 phần",
        _         => Status
    };
    [NotMapped] public string PatientName => Visit?.Patient?.FullName ?? "";
    [NotMapped] public string DoctorName  => Visit?.Doctor?.FullName ?? "";
}
