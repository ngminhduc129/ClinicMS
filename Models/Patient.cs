using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace ClinicMS.Models;

[Table("patients")]
public class Patient
{
    [Key]
    [Column("patient_id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int PatientId { get; set; }
    [Column("full_name")] public string FullName { get; set; } = "";
    [Column("date_of_birth")] public string? DateOfBirth { get; set; }
    [Column("gender")] public string? Gender { get; set; }
    [Column("blood_type")] public string? BloodType { get; set; }
    [Column("height_cm")] public double? HeightCm { get; set; }
    [Column("weight_kg")] public double? WeightKg { get; set; }
    [Column("note")] public string? Note { get; set; }
    [Column("created_at")] public string? CreatedAt { get; set; }

    // Navigation
    public List<PatientContact> PatientContacts { get; set; } = new();
    public List<Visit> Visits { get; set; } = new();

    // UI helpers (loaded from contacts, not stored in patients table)
    [NotMapped] public string? Phone { get; set; }
    [NotMapped] public string? Email { get; set; }
    [NotMapped] public string? Address { get; set; }

    [NotMapped] public string DisplayId => $"#{PatientId:D3}";

    [NotMapped]
    public string Initials
    {
        get
        {
            if (string.IsNullOrWhiteSpace(FullName)) return "?";
            var w = FullName.Trim().Split(' ');
            return string.Join("", w.TakeLast(2).Select(x => x.Length > 0 ? x[0].ToString().ToUpper() : ""));
        }
    }

    [NotMapped] public string HeightDisplay => HeightCm.HasValue ? $"{HeightCm} cm" : "—";
    [NotMapped] public string WeightDisplay => WeightKg.HasValue ? $"{WeightKg} kg" : "—";
    [NotMapped] public string PhoneDisplay => string.IsNullOrWhiteSpace(Phone) ? "—" : Phone;
    [NotMapped] public string EmailDisplay => string.IsNullOrWhiteSpace(Email) ? "—" : Email;
    [NotMapped] public string AddressDisplay => string.IsNullOrWhiteSpace(Address) ? "—" : Address;

    // ĐÃ SỬA CHỖ NÀY: Ép định dạng ngày sinh khi hiển thị lên giao diện
    [NotMapped]
    public string DateOfBirthDisplay
    {
        get
        {
            if (string.IsNullOrWhiteSpace(DateOfBirth)) return "—";

            // Cố gắng đọc ngày tháng từ Database (thường là yyyy-MM-dd) và chuyển sang dd/MM/yyyy
            if (DateTime.TryParse(DateOfBirth, out DateTime dt))
                return dt.ToString("dd/MM/yyyy");

            return DateOfBirth; // Trả về nguyên gốc nếu bị lỗi định dạng
        }
    }

    [NotMapped] public string GenderDisplay => string.IsNullOrWhiteSpace(Gender) ? "—" : Gender;
    [NotMapped] public string NoteDisplay => string.IsNullOrWhiteSpace(Note) ? "—" : Note;
}