using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicMS.Models;

[Table("payments")]
public class Payment
{
    [Key][Column("payment_id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int     PaymentId     { get; set; }
    [Column("visit_id")]          public int     VisitId       { get; set; }
    [Column("amount")]            public double  Amount        { get; set; }
    [Column("payment_method")]    public string? PaymentMethod { get; set; }
    [Column("payment_time")]      public string? PaymentTime   { get; set; }

    public Invoice? Invoice { get; set; }
    [NotMapped] public string AmountDisplay => Amount.ToString("N0") + " ₫";
}
