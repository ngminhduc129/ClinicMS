namespace ClinicMS.ViewModels;

public class ChartItem
{
    public string Label { get; set; } = "";
    public int Value { get; set; }
    public double Percentage { get; set; } // Dùng để vẽ chiều cao cột (0-100)
}
