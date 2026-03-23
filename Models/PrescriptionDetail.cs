using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicMS.Models;

public class PrescriptionDetail
{
    [Column("visit_id")]    public int VisitId { get; set; }
    [Column("medicine_id")] public int MedicineId { get; set; }

    // Số lượng thuốc kê (Ví dụ: 10 viên, 2 chai)
    [Column("quantity")]    public double Quantity { get; set; }

    // Cách dùng (Ví dụ: "Sáng 1 viên, tối 1 viên sau ăn")
    [Column("dosage")]      public string? Dosage { get; set; }

    [ForeignKey("MedicineId")]
    public Medicine? Medicine { get; set; }

    public Visit? Visit { get; set; }
}
