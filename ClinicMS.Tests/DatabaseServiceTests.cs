using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using ClinicMS.Services;
using ClinicMS.Models;
using ClinicMS.Data;
using System.Linq;

namespace ClinicMS.Tests
{
    public class DatabaseServiceTests : IDisposable
    {
        private readonly DatabaseService _service;
        private readonly string _testDbPath;

        public DatabaseServiceTests()
        {
            _testDbPath = Path.Combine(Path.GetTempPath(), $"test_db_{Guid.NewGuid()}.db");
            ClinicDbContext.OverrideDbPath = _testDbPath;
            
            // Create a fresh test DB
            DatabaseService.Initialize();
            _service = new DatabaseService();
        }

        public void Dispose()
        {
            if (File.Exists(_testDbPath))
            {
                // Delay deletion to avoid locking
                GC.Collect();
                GC.WaitForPendingFinalizers();
                try { File.Delete(_testDbPath); } catch { }
            }
        }

        [Fact]
        public async Task AddUpdateDeletePatient_MaintainsContactIntegrity()
        {
            // 1. Arrange & Add
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

            // Verify Contact was created
            var (fetched, contact) = await _service.GetPatientWithContactAsync(added.PatientId);
            Assert.NotNull(fetched);
            Assert.NotNull(contact);
            Assert.Equal("0987654321", fetched.Phone);

            // 2. Update
            fetched.Phone = "0111222333";
            await _service.UpdatePatientAsync(fetched);

            var (updatedFetched, updatedContact) = await _service.GetPatientWithContactAsync(fetched.PatientId);
            Assert.NotNull(updatedFetched);
            Assert.NotNull(updatedContact);
            Assert.Equal("0111222333", updatedFetched.Phone);
            Assert.Equal("0111222333", updatedContact.Phone);

            // 3. Delete
            await _service.DeletePatientAsync(fetched.PatientId);

            var deletedFetched = (await _service.GetPatientsAsync()).FirstOrDefault(p => p.PatientId == fetched.PatientId);
            Assert.Null(deletedFetched);

            // Direct check the real DB to ensure Contact is also removed
            using var db = new ClinicDbContext();
            var contactExists = db.Contacts.Any(c => c.ContactId == contact.ContactId);
            Assert.False(contactExists, "Contact should be deleted when Patient is deleted.");
        }

        [Fact]
        public async Task DeleteDoctor_RemovesAssociatedVisits()
        {
            // 1. Arrange
            var doctor = await _service.AddDoctorAsync(new Doctor { FullName = "Dr. Test" });
            var patient = await _service.AddPatientAsync(new Patient { FullName = "Patient Test" });
            
            var visit = await _service.AddVisitAsync(new Visit 
            { 
                DoctorId = doctor.DoctorId, 
                PatientId = patient.PatientId,
                VisitTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                Status = "Completed"
            });

            // 2. Act
            await _service.DeleteDoctorAsync(doctor.DoctorId);

            // 3. Assert
            var doctors = await _service.GetDoctorsAsync();
            Assert.DoesNotContain(doctors, d => d.DoctorId == doctor.DoctorId);

            var visits = await _service.GetVisitsAsync();
            Assert.DoesNotContain(visits, v => v.VisitId == visit.VisitId);
        }
    }
}
