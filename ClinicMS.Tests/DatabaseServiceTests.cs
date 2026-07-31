using System;
using System.Threading.Tasks;
using Xunit;
using Microsoft.EntityFrameworkCore;
using ClinicMS.Services;
using ClinicMS.Models;
using ClinicMS.Data;
using System.Linq;

namespace ClinicMS.Tests
{
    public class DatabaseServiceTests : IDisposable
    {
        private readonly DatabaseService _service;
        private readonly string _connectionString;
        private readonly string _dbName;

        public DatabaseServiceTests()
        {
            _dbName = $"ClinicMS_Test_{Guid.NewGuid():N}";
            _connectionString = $"Server=localhost\\SQLEXPRESS;Database={_dbName};Trusted_Connection=true;TrustServerCertificate=true;";
            ClinicDbContext.OverrideConnectionString = _connectionString;

            using var db = new ClinicDbContext();
            db.Database.EnsureCreated();

            _service = new DatabaseService();
        }

        public void Dispose()
        {
            ClinicDbContext.OverrideConnectionString = _connectionString;
            try
            {
                using var db = new ClinicDbContext();
                db.Database.EnsureDeleted();
            }
            catch { }
        }

        [Fact]
        public async Task AddUpdateDeletePatient_MaintainsContactIntegrity()
        {
            var newPatient = new Patient
            {
                FullName = "Nguyen Van Test",
                Phone = "0987654321",
                Email = "test@example.com",
                Address = "123 Test St",
                Gender = "Nam"
            };

            var added = await _service.AddPatientAsync(newPatient);
            Assert.True(added.PatientId > 0, "Patient ID should be generated.");

            var (fetched, contact) = await _service.GetPatientWithContactAsync(added.PatientId);
            Assert.NotNull(fetched);
            Assert.NotNull(contact);
            Assert.Equal("0987654321", fetched.Phone);

            fetched.Phone = "0111222333";
            await _service.UpdatePatientAsync(fetched);

            var (updatedFetched, updatedContact) = await _service.GetPatientWithContactAsync(fetched.PatientId);
            Assert.NotNull(updatedFetched);
            Assert.NotNull(updatedContact);
            Assert.Equal("0111222333", updatedFetched.Phone);
            Assert.Equal("0111222333", updatedContact.Phone);

            await _service.DeletePatientAsync(fetched.PatientId);

            var deletedFetched = (await _service.GetPatientsAsync()).FirstOrDefault(p => p.PatientId == fetched.PatientId);
            Assert.Null(deletedFetched);

            using var db = new ClinicDbContext();
            var contactExists = db.Contacts.Any(c => c.ContactId == contact.ContactId);
            Assert.False(contactExists, "Contact should be deleted when Patient is deleted.");
        }

        [Fact]
        public async Task DeleteDoctor_RemovesAssociatedVisits()
        {
            var doctor = await _service.AddDoctorAsync(new Doctor { FullName = "Dr. Test" });
            var patient = await _service.AddPatientAsync(new Patient { FullName = "Patient Test" });

            var visit = await _service.AddVisitAsync(new Visit
            {
                DoctorId = doctor.DoctorId,
                PatientId = patient.PatientId,
                VisitTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                Status = "Completed"
            });

            await _service.DeleteDoctorAsync(doctor.DoctorId);

            var doctors = await _service.GetDoctorsAsync();
            Assert.DoesNotContain(doctors, d => d.DoctorId == doctor.DoctorId);

            var visits = await _service.GetVisitsAsync();
            Assert.DoesNotContain(visits, v => v.VisitId == visit.VisitId);
        }
    }
}
