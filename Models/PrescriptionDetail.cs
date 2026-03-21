using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicMS.Models;

public class PrescriptionDetail
{
    public int VisitId { get; set; }
    public int MedicineId { get; set; }

    // Số lượng thuốc kê (Ví dụ: 10 viên, 2 chai)
    public double Quantity { get; set; }

    // Cách dùng (Ví dụ: "Sáng 1 viên, tối 1 viên sau ăn")
    public string? Dosage { get; set; }

    [ForeignKey("MedicineId")]
    public Medicine? Medicine { get; set; }

    [ForeignKey("VisitId")]
    public Visit? Visit { get; set; }
}
