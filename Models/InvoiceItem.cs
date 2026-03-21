using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicMS.Models;

[Table("invoice_items")]
public class InvoiceItem
{
    [Column("visit_id")]    public int    VisitId   { get; set; }
    [Column("item_type")]   public string ItemType  { get; set; } = "";
    [Column("item_id")]     public int    ItemId    { get; set; }
    [Column("quantity")]    public int    Quantity  { get; set; }
    [Column("unit_price")]  public double UnitPrice { get; set; }
    [NotMapped] public double Subtotal => Quantity * UnitPrice;

    public Invoice? Invoice { get; set; }
}
