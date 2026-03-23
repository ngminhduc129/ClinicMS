using System;
using Microsoft.EntityFrameworkCore;
using ClinicMS.Models;
using System.IO;

namespace ClinicMS.Data;

public class ClinicDbContext : DbContext
{
    public DbSet<Patient>            Patients            { get; set; } = null!;
    public DbSet<Contact>            Contacts            { get; set; } = null!;
    public DbSet<PatientContact>     PatientContacts     { get; set; } = null!;
    public DbSet<Doctor>             Doctors             { get; set; } = null!;
    public DbSet<Visit>              Visits              { get; set; } = null!;
    public DbSet<Appointment>        Appointments        { get; set; } = null!;
    public DbSet<Medicine>           Medicines           { get; set; } = null!;
    public DbSet<Prescription>       Prescriptions       { get; set; } = null!;
    public DbSet<PrescriptionDetail> PrescriptionDetails { get; set; } = null!;
    public DbSet<Service>            Services            { get; set; } = null!;
    public DbSet<ServiceOrder>       ServiceOrders       { get; set; } = null!;
    public DbSet<Invoice>            Invoices            { get; set; } = null!;
    public DbSet<InvoiceItem>        InvoiceItems        { get; set; } = null!;
    public DbSet<Payment>            Payments            { get; set; } = null!;
    public DbSet<AppSetting>         AppSettings         { get; set; } = null!;

    public static string? OverrideDbPath { get; set; }

    // Dùng đúng file DB của người dùng
    private static string DbPath
    {
        get
        {
            if (!string.IsNullOrEmpty(OverrideDbPath)) return OverrideDbPath;

            var appDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ClinicMS");

            Directory.CreateDirectory(appDataFolder);

            return Path.Combine(appDataFolder, "clinic_database.db");
        }
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite($"Data Source={DbPath}");

    protected override void OnModelCreating(ModelBuilder mb)
    {
        base.OnModelCreating(mb);

        // Ép tên bảng về viết thường để khớp với script seed data
        mb.Entity<Patient>().ToTable("patients");
        mb.Entity<Doctor>().ToTable("doctors");
        mb.Entity<Medicine>().ToTable("medicines");
        mb.Entity<Contact>().ToTable("contacts");
        mb.Entity<PatientContact>().ToTable("patient_contacts");
        mb.Entity<Visit>().ToTable("visits");
        mb.Entity<Service>().ToTable("services");
        mb.Entity<ServiceOrder>().ToTable("service_orders");
        mb.Entity<Invoice>().ToTable("invoices");
        mb.Entity<InvoiceItem>().ToTable("invoice_items");
        mb.Entity<Payment>().ToTable("payments");
        mb.Entity<AppSetting>().ToTable("AppSettings");
        mb.Entity<Prescription>().ToTable("prescriptions");
        mb.Entity<Appointment>().ToTable("appointments");
        // Composite PKs
        mb.Entity<PatientContact>().HasKey(pc => new { pc.PatientId, pc.ContactId });
        mb.Entity<InvoiceItem>().HasKey(ii => new { ii.VisitId, ii.ItemType, ii.ItemId });
        mb.Entity<PrescriptionDetail>().HasKey(pd => new { pd.VisitId, pd.MedicineId });
        
        // ... rest of relationships

        // FK relationships
        mb.Entity<Visit>()
            .HasOne(v => v.Patient).WithMany(p => p.Visits).HasForeignKey(v => v.PatientId);
        mb.Entity<Visit>()
            .HasOne(v => v.Doctor).WithMany(d => d.Visits).HasForeignKey(v => v.DoctorId);

        mb.Entity<PatientContact>()
            .HasOne(pc => pc.Patient).WithMany(p => p.PatientContacts).HasForeignKey(pc => pc.PatientId);
        mb.Entity<PatientContact>()
            .HasOne(pc => pc.Contact).WithMany(c => c.PatientContacts).HasForeignKey(pc => pc.ContactId);

        mb.Entity<PrescriptionDetail>()
            .HasOne(pd => pd.Medicine).WithMany().HasForeignKey(pd => pd.MedicineId);

        mb.Entity<ServiceOrder>()
            .HasOne(so => so.Service).WithMany().HasForeignKey(so => so.ServiceId);

        mb.Entity<Invoice>()
            .HasOne(i => i.Visit).WithMany().HasForeignKey(i => i.VisitId);

        mb.Entity<Payment>()
            .HasOne(p => p.Invoice).WithMany(i => i.Payments).HasForeignKey(p => p.VisitId);

        mb.Entity<InvoiceItem>()
            .HasOne(ii => ii.Invoice).WithMany(i => i.Items).HasForeignKey(ii => ii.VisitId);

        mb.Entity<AppSetting>()
            .HasKey(s => s.Key);
        mb.Entity<AppSetting>()
            .ToTable("AppSettings");
    }
}
