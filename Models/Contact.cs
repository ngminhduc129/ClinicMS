using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicMS.Models;

[Table("contacts")]
public class Contact
{
    [Key]
    [Column("contact_id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int     ContactId  { get; set; }
    [Column("phone")]   public string? Phone     { get; set; }
    [Column("email")]   public string? Email     { get; set; }
    [Column("address")] public string? Address   { get; set; }
    [Column("created_at")] public string? CreatedAt { get; set; }

    public List<PatientContact> PatientContacts { get; set; } = new();
}
