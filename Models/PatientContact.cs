using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicMS.Models;

[Table("patient_contacts")]
public class PatientContact
{
    [Column("patient_id")] public int  PatientId    { get; set; }
    [Column("contact_id")] public int  ContactId    { get; set; }
    [Column("relationship")] public string? Relationship { get; set; }
    [Column("is_primary")]  public int  IsPrimary   { get; set; } = 1;

    public Patient? Patient { get; set; }
    public Contact? Contact { get; set; }
}
