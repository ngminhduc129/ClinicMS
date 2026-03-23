using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace ClinicMS.Models;

[Table("doctors")]
public class Doctor
{
    [Key]
    [Column("doctor_id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int    DoctorId  { get; set; }
    [Column("full_name")]  public string  FullName  { get; set; } = "";
    [Column("specialty")]  public string? Specialty { get; set; }
    [Column("phone")]      public string? Phone     { get; set; }
    [Column("email")]      public string? Email     { get; set; }
    [Column("note")]       public string? Note      { get; set; }
    [Column("created_at")] public string? CreatedAt { get; set; }

    public List<Visit> Visits { get; set; } = new();

    [NotMapped] public string DisplayId => $"#{DoctorId:D3}";
    [NotMapped] public string Initials
    {
        get
        {
            if (string.IsNullOrWhiteSpace(FullName)) return "?";
            var w = FullName.Trim().Split(' ');
            return string.Join("", w.TakeLast(2).Select(x => x.Length > 0 ? x[0].ToString().ToUpper() : ""));
        }
    }
}
