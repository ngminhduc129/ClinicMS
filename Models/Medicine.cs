using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicMS.Models;

[Table("medicines")]
public class Medicine
{
    [Key]
    [Column("medicine_id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int    MedicineId   { get; set; }
    [Column("medicine_name")] public string  MedicineName { get; set; } = "";
    [Column("unit")]          public string? Unit         { get; set; }
    [Column("price")]         public double  Price        { get; set; }
    [Column("stock_quantity")] public int    StockQuantity { get; set; }
    [Column("created_at")]    public string? CreatedAt    { get; set; }

    [NotMapped] public string PriceDisplay => Price.ToString("N0") + " ₫";
    
    // Thành tiền tồn kho = Giá x Số lượng
    [NotMapped] public double StockValue => Price * StockQuantity;
    [NotMapped] public string StockValueDisplay => StockValue.ToString("N0") + " ₫";

    // Tính toán tỉ lệ tồn kho để vẽ ProgressBar (Giả định mức tối đa là 100 để hiển thị đầy thanh)
    [NotMapped]
    public double StockPercent => Math.Clamp(StockQuantity, 0, 100);

    [NotMapped]
    public string StockStatus => StockQuantity switch
    {
        <= 0 => "Hết hàng",
        <= 10 => "Sắp hết",
        _ => "Còn hàng"
    };

    [NotMapped]
    public string StockStatusColor => StockQuantity switch
    {
        <= 0 => "#EF4444", // Red 500
        <= 10 => "#F59E0B", // Amber 500
        _ => "#10B981"      // Emerald 500
    };

    [NotMapped]
    public string StockStatusBg => StockQuantity switch
    {
        <= 0 => "#FEE2E2", // Red 100
        <= 10 => "#FEF3C7", // Amber 100
        _ => "#D1FAE5"      // Emerald 100
    };
}
