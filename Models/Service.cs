using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicMS.Models;

[Table("services")]
public class Service
{
    [Key]
    [Column("service_id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int    ServiceId   { get; set; }
    [Column("service_name")] public string ServiceName { get; set; } = "";
    [Column("price")]        public double Price       { get; set; }
    [NotMapped] public string PriceDisplay => Price.ToString("N0") + " ₫";
}
