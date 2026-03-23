using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicMS.Models;

[Table("prescriptions")]
public class Prescription
{
    [Key]
    [Column("visit_id")]    public int    VisitId   { get; set; }
    [Column("created_at")] public string? CreatedAt { get; set; }
}
