using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicMS.Models;

[Table("service_orders")]
public class ServiceOrder
{
    [Key]
    [Column("service_order_id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int    ServiceOrderId { get; set; }
    [Column("visit_id")]   public int    VisitId   { get; set; }
    [Column("service_id")] public int    ServiceId { get; set; }
    [Column("result")]     public string? Result   { get; set; }
    public Service? Service { get; set; }
}
