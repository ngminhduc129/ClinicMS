using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicMS.Models;

[Table("visits")]
public class Visit
{
    [Key]
    [Column("visit_id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int     VisitId           { get; set; }
    [Column("patient_id")]       public int     PatientId         { get; set; }
    [Column("doctor_id")]        public int     DoctorId          { get; set; }
    [Column("appointment_id")]   public int?    AppointmentId     { get; set; }
    [Column("visit_time")]       public string? VisitTime         { get; set; }
    [Column("visit_type")]       public string  VisitType         { get; set; } = "Walk-in";
    [Column("status")]           public string  Status            { get; set; } = "Waiting";
    [Column("symptoms")]         public string? Symptoms          { get; set; }
    [Column("diagnosis")]        public string? Diagnosis         { get; set; }
    [Column("note")]             public string? Note              { get; set; }
    [Column("pulse")]            public string? Pulse             { get; set; }
    [Column("blood_pressure")]   public string? BloodPressure     { get; set; }
    [Column("temperature")]      public string? Temperature       { get; set; }
    [Column("weight")]           public string? Weight            { get; set; }
    [Column("prescription_text")] public string? PrescriptionText { get; set; }

    // Navigation
    public Patient? Patient { get; set; }
    public Doctor?  Doctor  { get; set; }
    public ICollection<PrescriptionDetail> PrescriptionDetails { get; set; } = new List<PrescriptionDetail>();

    // Computed
    [NotMapped] public string PatientName    => Patient?.FullName ?? "";
    [NotMapped] public string DoctorName     => Doctor?.FullName ?? "";
    [NotMapped] public string VisitTypeDisplay => VisitType switch
    {
        "Walk-in"     => "Khám trực tiếp",
        "Appointment" => "Hẹn trước",
        "Emergency"   => "Cấp cứu",
        "Follow-up"   => "Tái khám",
        _             => VisitType
    };
    [NotMapped] public string StatusDisplay => Status switch
    {
        "Waiting"     => "Đang chờ",
        "In Progress" => "Đang khám",
        "Completed"   => "Hoàn thành",
        _             => Status
    };

    [NotMapped]
    public int StatusOrder => Status switch
    {
        "Waiting"     => 1,
        "In Progress" => 2,
        "Completed"   => 3,
        _             => 1
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
        "Waiting"     => "CHỜ KHÁM",
        "In Progress" => "ĐANG KHÁM",
        "Completed"   => "ĐÃ KHÁM",
        _             => "CHỜ KHÁM"
    };

    [NotMapped]
    public string StatusDotColor => Status switch
    {
        "In Progress" => "#3B82F6",
        "Completed"   => "#16A34A",
        _             => "#F59E0B"
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
