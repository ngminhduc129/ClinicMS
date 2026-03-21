using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicMS.Models;

[Table("appointments")]
public class Appointment
{
    [Key]
    [Column("appointment_id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int    AppointmentId   { get; set; }
    [Column("patient_id")]        public int    PatientId       { get; set; }
    [Column("doctor_id")]         public int    DoctorId        { get; set; }
    [Column("appointment_time")]  public string AppointmentTime { get; set; } = "";
    [Column("status")]            public string Status          { get; set; } = "Scheduled";
    [Column("created_at")]        public string? CreatedAt      { get; set; }
    public Patient? Patient { get; set; }
    public Doctor?  Doctor  { get; set; }
}
