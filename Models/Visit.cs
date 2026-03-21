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
}
