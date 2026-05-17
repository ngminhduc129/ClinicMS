using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;

namespace ClinicMS.Models;

[Table("visits")]
public class Visit : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    [Key]
    [Column("visit_id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int VisitId { get; set; }

    private int _patientId;
    [Column("patient_id")]
    public int PatientId { get => _patientId; set { _patientId = value; OnPropertyChanged(); } }

    private int _doctorId;
    [Column("doctor_id")]
    public int DoctorId { get => _doctorId; set { _doctorId = value; OnPropertyChanged(); } }

    [Column("appointment_id")] public int? AppointmentId { get; set; }

    private string? _visitTime;
    [Column("visit_time")]
    public string? VisitTime { get => _visitTime; set { _visitTime = value; OnPropertyChanged(); OnPropertyChanged(nameof(FormattedTime)); } }

    private string _visitType = "Walk-in";
    [Column("visit_type")]
    public string VisitType { get => _visitType; set { _visitType = value; OnPropertyChanged(); OnPropertyChanged(nameof(VisitTypeDisplay)); } }

    private string _status = "Waiting";
    [Column("status")]
    public string Status { get => _status; set { _status = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusDisplay)); OnPropertyChanged(nameof(StatusOrder)); OnPropertyChanged(nameof(StatusGroup)); OnPropertyChanged(nameof(StatusDotColor)); } }

    private string? _symptoms;
    [Column("symptoms")]
    public string? Symptoms { get => _symptoms; set { _symptoms = value; OnPropertyChanged(); } }

    private string? _diagnosis;
    [Column("diagnosis")]
    public string? Diagnosis { get => _diagnosis; set { _diagnosis = value; OnPropertyChanged(); } }

    private string? _note;
    [Column("note")]
    public string? Note { get => _note; set { _note = value; OnPropertyChanged(); } }

    private string? _pulse;
    [Column("pulse")]
    public string? Pulse { get => _pulse; set { _pulse = value; OnPropertyChanged(); } }

    private string? _bloodPressure;
    [Column("blood_pressure")]
    public string? BloodPressure { get => _bloodPressure; set { _bloodPressure = value; OnPropertyChanged(); } }

    private string? _temperature;
    [Column("temperature")]
    public string? Temperature { get => _temperature; set { _temperature = value; OnPropertyChanged(); } }

    private string? _weight;
    [Column("weight")]
    public string? Weight { get => _weight; set { _weight = value; OnPropertyChanged(); } }

    private string? _prescriptionText;
    [Column("prescription_text")]
    public string? PrescriptionText { get => _prescriptionText; set { _prescriptionText = value; OnPropertyChanged(); } }

    // Navigation
    private Patient? _patient;
    public Patient? Patient { get => _patient; set { _patient = value; OnPropertyChanged(); OnPropertyChanged(nameof(PatientName)); OnPropertyChanged(nameof(PatientInitials)); OnPropertyChanged(nameof(PatientColor)); OnPropertyChanged(nameof(PatientGenderIcon)); OnPropertyChanged(nameof(GenderColor)); } }

    private Doctor? _doctor;
    public Doctor? Doctor { get => _doctor; set { _doctor = value; OnPropertyChanged(); OnPropertyChanged(nameof(DoctorName)); } }

    public ICollection<PrescriptionDetail> PrescriptionDetails { get; set; } = new List<PrescriptionDetail>();
    public ICollection<ServiceOrder> ServiceOrders { get; set; } = new List<ServiceOrder>();

    // Computed
    [NotMapped] public string PatientName => Patient?.FullName ?? "";
    [NotMapped] public string DoctorName => Doctor?.FullName ?? "";
    [NotMapped]
    public string VisitTypeDisplay => VisitType switch
    {
        "Walk-in" => "Khám trực tiếp",
        "Appointment" => "Hẹn trước",
        "Emergency" => "Cấp cứu",
        "Follow-up" => "Tái khám",
        _ => VisitType
    };
    [NotMapped]
    public string StatusDisplay => Status switch
    {
        "Waiting" => "Đang chờ",
        "In Progress" => "Đang khám",
        "Completed" => "Hoàn thành",
        _ => Status
    };

    [NotMapped]
    public int StatusOrder => Status switch
    {
        "Waiting" => 1,
        "In Progress" => 2,
        "Completed" => 3,
        _ => 1
    };

    [NotMapped]
    public string PatientInitials
    {
        get
        {
            if (string.IsNullOrWhiteSpace(PatientName)) return "?";
            var parts = PatientName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return "?";
            if (parts.Length == 1) return parts[0][0].ToString().ToUpper();
            return (parts[0][0].ToString() + parts[^1][0].ToString()).ToUpper();
        }
    }

    [NotMapped]
    public string PatientColor
    {
        get
        {
            if (string.IsNullOrWhiteSpace(PatientName)) return "#9CA3AF";
            string[] colors = { "#EF4444", "#F59E0B", "#10B981", "#3B82F6", "#6366F1", "#8B5CF6", "#EC4899" };
            int index = Math.Abs(PatientName.GetHashCode()) % colors.Length;
            return colors[index];
        }
    }

    [NotMapped]
    public string PatientGenderIcon => Patient?.Gender == "Nữ" ? "\uE8D4" : "\uE77B";

    [NotMapped]
    public string GenderColor => Patient?.Gender == "Nữ" ? "#0D9488" : "#64748B"; // Nữ: Teal (Xanh mòng két), Nam: Slate (Xám xanh)

    [NotMapped]
    public string StatusGroup => Status switch
    {
        "Waiting" => "CHỜ KHÁM",
        "In Progress" => "ĐANG KHÁM",
        "Completed" => "ĐÃ KHÁM",
        _ => "CHỜ KHÁM"
    };

    [NotMapped]
    public string StatusDotColor => Status switch
    {
        "In Progress" => "#3B82F6",
        "Completed" => "#16A34A",
        _ => "#F59E0B"
    };

    [NotMapped]
    public string FormattedTime
    {
        get
        {
            if (string.IsNullOrWhiteSpace(VisitTime)) return "--:--";
            if (DateTime.TryParse(VisitTime, out var dt)) return dt.ToString("HH:mm");
            return "--:--";
        }
    }
}
