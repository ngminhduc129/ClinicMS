using System;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using Xunit;
using ClinicMS.Services;
using ClinicMS.Models;
using ClinicMS.Data;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace ClinicMS.Tests
{
    public abstract class ServiceTestBase : IDisposable
    {
        protected readonly string _testDbPath;
        protected readonly CoreDatabaseService _core;

        protected ServiceTestBase()
        {
            _testDbPath = Path.Combine(Path.GetTempPath(), $"test_db_{Guid.NewGuid()}.db");
            ClinicDbContext.OverrideDbPath = _testDbPath;
            CoreDatabaseService.Initialize();
            _core = new CoreDatabaseService();
        }

        public void Dispose()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            for (int i = 0; i < 3; i++)
            {
                try { if (File.Exists(_testDbPath)) { File.Delete(_testDbPath); } return; }
                catch { System.Threading.Thread.Sleep(200); }
            }
        }
    }

    // ======================== PATIENT SERVICE ========================

    public class PatientServiceTests : ServiceTestBase
    {
        [Fact]
        public async Task AddPatient_CreatesContactAndPatientContact()
        {
            var svc = new PatientService(_core);
            var p = new Patient { FullName = "Test Patient", Phone = "0900000001", Email = "a@b.com", Address = "Addr" };
            var added = await svc.AddPatientAsync(p);
            Assert.True(added.PatientId > 0);
            Assert.Equal("Test Patient", added.FullName);
            var (fetched, contact) = await svc.GetPatientWithContactAsync(added.PatientId);
            Assert.NotNull(fetched);
            Assert.NotNull(contact);
            Assert.Equal("0900000001", contact.Phone);
        }

        [Fact]
        public async Task UpdatePatient_ModifiesPrimaryContact()
        {
            var svc = new PatientService(_core);
            var p = await svc.AddPatientAsync(new Patient { FullName = "Update Me", Phone = "0900000002" });
            p.Phone = "0999999999";
            await svc.UpdatePatientAsync(p);
            var (_, contact) = await svc.GetPatientWithContactAsync(p.PatientId);
            Assert.NotNull(contact);
            Assert.Equal("0999999999", contact.Phone);
        }

        [Fact]
        public async Task DeletePatient_RemovesPatientContactAndVisits()
        {
            var svcP = new PatientService(_core);
            var svcD = new DoctorService(_core);
            var svcV = new VisitService(_core);

            var p = await svcP.AddPatientAsync(new Patient { FullName = "Del Patient", Phone = "0900000003" });
            var doc = await svcD.AddDoctorAsync(new Doctor { FullName = "Dr. Del" });
            var visit = await svcV.AddVisitAsync(new Visit { PatientId = p.PatientId, DoctorId = doc.DoctorId, VisitTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), Status = "Completed" });

            await svcP.DeletePatientAsync(p.PatientId);

            var patients = await svcP.GetPatientsAsync();
            Assert.DoesNotContain(patients, x => x.PatientId == p.PatientId);

            var visits = await svcV.GetVisitsAsync();
            Assert.DoesNotContain(visits, x => x.VisitId == visit.VisitId);

            using var db = new ClinicDbContext();
            var pcExists = db.PatientContacts.Any(x => x.PatientId == p.PatientId);
            Assert.False(pcExists);
        }

        [Fact]
        public async Task SearchPatients_ByName_ReturnsMatches()
        {
            var svc = new PatientService(_core);
            await svc.AddPatientAsync(new Patient { FullName = "Nguyen Van A", Phone = "0900000010" });
            await svc.AddPatientAsync(new Patient { FullName = "Nguyen Van B", Phone = "0900000011" });
            await svc.AddPatientAsync(new Patient { FullName = "Le Thi C", Phone = "0900000012" });

            var results = await svc.SearchPatientsAsync("Nguyen");
            Assert.Equal(2, results.Count);
            Assert.All(results, r => Assert.Contains("Nguyen", r.FullName));
        }

        [Fact]
        public async Task SearchPatients_ByPhone_ReturnsMatches()
        {
            var svc = new PatientService(_core);
            await svc.AddPatientAsync(new Patient { FullName = "Phone Test 1", Phone = "0988888888" });
            await svc.AddPatientAsync(new Patient { FullName = "Phone Test 2", Phone = "0977777777" });

            var results = await svc.SearchPatientsAsync("8888");
            Assert.Single(results);
            Assert.Equal("0988888888", results[0].Phone);
        }

        [Fact]
        public async Task Pagination_ReturnsCorrectSlice()
        {
            var svc = new PatientService(_core);
            for (int i = 0; i < 10; i++)
                await svc.AddPatientAsync(new Patient { FullName = $"Pagination Test {i}", Phone = $"09000001{i:D2}" });

            var page1 = await svc.GetPatientsWithContactsAsync(0, 4);
            Assert.Equal(4, page1.Count);

            var page2 = await svc.GetPatientsWithContactsAsync(4, 4);
            Assert.Equal(4, page2.Count);

            var page3 = await svc.GetPatientsWithContactsAsync(8, 4);
            Assert.Equal(2, page3.Count);
        }

        [Fact]
        public async Task Counts_AreAccurate()
        {
            var svc = new PatientService(_core);
            Assert.Equal(0, await svc.GetPatientsCountAsync());
            await svc.AddPatientAsync(new Patient { FullName = "Count 1", Phone = "0900000090" });
            await svc.AddPatientAsync(new Patient { FullName = "Count 2", Phone = "0900000091" });
            Assert.Equal(2, await svc.GetPatientsCountAsync());
            Assert.Equal(2, await svc.GetPatientsSearchCountAsync("Count"));
            Assert.Equal(1, await svc.GetPatientsSearchCountAsync("Count 1"));
            Assert.Equal(0, await svc.GetPatientsSearchCountAsync("NonExistent"));
        }
    }

    // ======================== DOCTOR SERVICE ========================

    public class DoctorServiceTests : ServiceTestBase
    {
        [Fact]
        public async Task AddDoctor_SetsId()
        {
            var svc = new DoctorService(_core);
            var d = await svc.AddDoctorAsync(new Doctor { FullName = "Dr. Smith", Specialty = "Tim mach", Phone = "0900000020" });
            Assert.True(d.DoctorId > 0);
            Assert.Equal("Dr. Smith", d.FullName);
        }

        [Fact]
        public async Task UpdateDoctor_ModifiesFields()
        {
            var svc = new DoctorService(_core);
            var d = await svc.AddDoctorAsync(new Doctor { FullName = "Dr. Old", Specialty = "Da lieu" });
            d.FullName = "Dr. New";
            d.Specialty = "Than kinh";
            await svc.UpdateDoctorAsync(d);
            var all = await svc.GetDoctorsAsync();
            var updated = all.First(x => x.DoctorId == d.DoctorId);
            Assert.Equal("Dr. New", updated.FullName);
            Assert.Equal("Than kinh", updated.Specialty);
        }

        [Fact]
        public async Task DeleteDoctor_RemovesAssociatedVisits()
        {
            var svcD = new DoctorService(_core);
            var svcP = new PatientService(_core);
            var svcV = new VisitService(_core);

            var doc = await svcD.AddDoctorAsync(new Doctor { FullName = "Dr. To Delete" });
            var pat = await svcP.AddPatientAsync(new Patient { FullName = "Patient for Doc Del", Phone = "0900000030" });
            var v = await svcV.AddVisitAsync(new Visit { PatientId = pat.PatientId, DoctorId = doc.DoctorId, VisitTime = "2024-01-01 10:00:00", Status = "Completed" });

            await svcD.DeleteDoctorAsync(doc.DoctorId);
            Assert.DoesNotContain(await svcD.GetDoctorsAsync(), x => x.DoctorId == doc.DoctorId);

            var visits = await svcV.GetVisitsAsync();
            Assert.DoesNotContain(visits, x => x.VisitId == v.VisitId);
        }

        [Fact]
        public async Task SearchDoctors_ByNameOrSpecialty()
        {
            var svc = new DoctorService(_core);
            await svc.AddDoctorAsync(new Doctor { FullName = "Nguyen Van A", Specialty = "Noi tong quat" });
            await svc.AddDoctorAsync(new Doctor { FullName = "Tran Van B", Specialty = "Ngoai khoa" });
            await svc.AddDoctorAsync(new Doctor { FullName = "Le Thi C", Specialty = "Noi tong quat" });

            Assert.Equal(1, (await svc.SearchDoctorsAsync("Nguyen")).Count);
            Assert.Equal(2, (await svc.SearchDoctorsAsync("Noi")).Count);
            Assert.Equal(2, (await svc.SearchDoctorsAsync("Van")).Count);
            Assert.Empty(await svc.SearchDoctorsAsync("Khong co"));
        }

        [Fact]
        public async Task Lookups_AreCorrect()
        {
            var svc = new DoctorService(_core);
            await svc.AddDoctorAsync(new Doctor { FullName = "Dr. A" });
            await svc.AddDoctorAsync(new Doctor { FullName = "Dr. B" });
            var names = await svc.GetAllDoctorNamesAsync();
            Assert.Contains("Dr. A", names);
            Assert.Contains("Dr. B", names);
            var lookup = await svc.GetDoctorsLookupAsync();
            Assert.True(lookup.Count >= 2);
        }
    }

    // ======================== MEDICINE SERVICE ========================

    public class MedicineServiceTests : ServiceTestBase
    {
        [Fact]
        public async Task CRUD_Medicine()
        {
            var svc = new MedicineService(_core);

            var m = await svc.AddMedicineAsync(new Medicine { MedicineName = "Paracetamol", Unit = "Vien", Price = 5000, StockQuantity = 100 });
            Assert.True(m.MedicineId > 0);

            m.Price = 6000;
            await svc.UpdateMedicineAsync(m);
            var all = await svc.GetMedicinesAsync();
            var updated = all.First(x => x.MedicineId == m.MedicineId);
            Assert.Equal(6000, updated.Price);

            await svc.DeleteMedicineAsync(m.MedicineId);
            all = await svc.GetMedicinesAsync();
            Assert.DoesNotContain(all, x => x.MedicineId == m.MedicineId);
        }

        [Fact]
        public async Task StockQuantities_ArePersisted()
        {
            var svc = new MedicineService(_core);
            var m = await svc.AddMedicineAsync(new Medicine { MedicineName = "Vitamin C", StockQuantity = 50, ReservedQuantity = 5, Price = 2000 });
            var fetched = (await svc.GetMedicinesAsync()).First(x => x.MedicineId == m.MedicineId);
            Assert.Equal(50, fetched.StockQuantity);
            Assert.Equal(5, fetched.ReservedQuantity);
            Assert.Equal(45, fetched.AvailableQuantity);
        }
    }

    // ======================== SERVICE CATALOG ========================

    public class ServiceCatalogServiceTests : ServiceTestBase
    {
        [Fact]
        public async Task CRUD_Service()
        {
            var svc = new ServiceCatalogService(_core);

            var s = await svc.AddServiceAsync(new Service { ServiceName = "Xet nghiem mau", Price = 150000 });
            Assert.True(s.ServiceId > 0);

            s.Price = 200000;
            await svc.UpdateServiceAsync(s);
            var all = await svc.GetServicesAsync();
            var updated = all.First(x => x.ServiceId == s.ServiceId);
            Assert.Equal(200000, updated.Price);

            await svc.DeleteServiceAsync(s.ServiceId);
            all = await svc.GetServicesAsync();
            Assert.DoesNotContain(all, x => x.ServiceId == s.ServiceId);
        }
    }

    // ======================== VISIT SERVICE ========================

    public class VisitServiceTests : ServiceTestBase
    {
        [Fact]
        public async Task AddVisit_CreatesVisitWithDetails()
        {
            var svcP = new PatientService(_core);
            var svcD = new DoctorService(_core);
            var svcV = new VisitService(_core);

            var p = await svcP.AddPatientAsync(new Patient { FullName = "Visit Patient", Phone = "0900000040" });
            var doc = await svcD.AddDoctorAsync(new Doctor { FullName = "Dr. Visit" });

            var v = new Visit
            {
                PatientId = p.PatientId,
                DoctorId = doc.DoctorId,
                VisitTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                Status = "Waiting",
                Symptoms = "Dau bung",
                Diagnosis = "Viem da day"
            };
            var added = await svcV.AddVisitAsync(v);
            Assert.True(added.VisitId > 0);

            var fetched = await svcV.GetVisitDetailsAsync(added.VisitId);
            Assert.NotNull(fetched);
            Assert.Equal("Dau bung", fetched.Symptoms);
            Assert.Equal("Viem da day", fetched.Diagnosis);
        }

        [Fact]
        public async Task AddVisit_WithPrescriptionAndServices()
        {
            var svcP = new PatientService(_core);
            var svcD = new DoctorService(_core);
            var svcM = new MedicineService(_core);
            var svcS = new ServiceCatalogService(_core);
            var svcV = new VisitService(_core);

            var p = await svcP.AddPatientAsync(new Patient { FullName = "Rx Patient", Phone = "0900000050" });
            var doc = await svcD.AddDoctorAsync(new Doctor { FullName = "Dr. Rx" });
            var med = await svcM.AddMedicineAsync(new Medicine { MedicineName = "Amoxicillin", StockQuantity = 100, Price = 10000 });
            var svc = await svcS.AddServiceAsync(new Service { ServiceName = "X-Quang", Price = 200000 });

            var v = new Visit
            {
                PatientId = p.PatientId,
                DoctorId = doc.DoctorId,
                VisitTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                Status = "Completed",
                PrescriptionDetails = new System.Collections.Generic.List<PrescriptionDetail>
                {
                    new PrescriptionDetail { MedicineId = med.MedicineId, Quantity = 10, Dosage = "1v x 2/ngay" }
                },
                ServiceOrders = new System.Collections.Generic.List<ServiceOrder>
                {
                    new ServiceOrder { ServiceId = svc.ServiceId }
                }
            };
            var added = await svcV.AddVisitAsync(v);
            var details = await svcV.GetPrescriptionDetailsAsync(added.VisitId);
            Assert.Single(details);
            Assert.Equal("Amoxicillin", details[0].Medicine?.MedicineName);

            var orders = await svcV.GetServiceOrdersAsync(added.VisitId);
            Assert.Single(orders);
            Assert.Equal("X-Quang", orders[0].Service?.ServiceName);
        }

        [Fact]
        public async Task SavePrescriptionDetails_UpdatesReservedQuantity()
        {
            var svcP = new PatientService(_core);
            var svcD = new DoctorService(_core);
            var svcM = new MedicineService(_core);
            var svcV = new VisitService(_core);

            var p = await svcP.AddPatientAsync(new Patient { FullName = "Stock Patient", Phone = "0900000060" });
            var doc = await svcD.AddDoctorAsync(new Doctor { FullName = "Dr. Stock" });
            var med = await svcM.AddMedicineAsync(new Medicine { MedicineName = "Stock Test Med", StockQuantity = 100, Price = 5000 });

            var v = await svcV.AddVisitAsync(new Visit { PatientId = p.PatientId, DoctorId = doc.DoctorId, VisitTime = "2024-06-01 10:00:00", Status = "In Progress" });

            var items = new System.Collections.Generic.List<PrescriptionDetail>
            {
                new PrescriptionDetail { MedicineId = med.MedicineId, Quantity = 20, Dosage = "1v/ngay" }
            };
            await svcV.SavePrescriptionDetailsAsync(v.VisitId, items);

            var updatedMed = (await svcM.GetMedicinesAsync()).First(x => x.MedicineId == med.MedicineId);
            Assert.Equal(20, updatedMed.ReservedQuantity);

            var lessItems = new System.Collections.Generic.List<PrescriptionDetail>
            {
                new PrescriptionDetail { MedicineId = med.MedicineId, Quantity = 5, Dosage = "1v/ngay" }
            };
            await svcV.SavePrescriptionDetailsAsync(v.VisitId, lessItems);
            updatedMed = (await svcM.GetMedicinesAsync()).First(x => x.MedicineId == med.MedicineId);
            Assert.Equal(5, updatedMed.ReservedQuantity);
        }

        [Fact]
        public async Task DeleteVisit_RemovesRelatedData()
        {
            var svcP = new PatientService(_core);
            var svcD = new DoctorService(_core);
            var svcM = new MedicineService(_core);
            var svcV = new VisitService(_core);
            var svcI = new InvoiceService(_core);

            var p = await svcP.AddPatientAsync(new Patient { FullName = "Del Visit Patient", Phone = "0900000070" });
            var doc = await svcD.AddDoctorAsync(new Doctor { FullName = "Dr. Del Visit" });
            var med = await svcM.AddMedicineAsync(new Medicine { MedicineName = "Del Med", StockQuantity = 50, Price = 3000 });

            var v = await svcV.AddVisitAsync(new Visit { PatientId = p.PatientId, DoctorId = doc.DoctorId, VisitTime = "2024-06-01 10:00:00", Status = "Completed" });
            await svcV.SavePrescriptionDetailsAsync(v.VisitId, new System.Collections.Generic.List<PrescriptionDetail>
            {
                new PrescriptionDetail { MedicineId = med.MedicineId, Quantity = 10, Dosage = "1v/ngay" }
            });
            await svcI.GenerateOrUpdateInvoiceAsync(v.VisitId);

            await svcV.DeleteVisitAsync(v.VisitId);
            var visits = await svcV.GetVisitsAsync();
            Assert.DoesNotContain(visits, x => x.VisitId == v.VisitId);

            var details = await svcV.GetPrescriptionDetailsAsync(v.VisitId);
            Assert.Empty(details);
        }

        [Fact]
        public async Task DateFilter_ReturnsCorrectVisits()
        {
            var svcP = new PatientService(_core);
            var svcD = new DoctorService(_core);
            var svcV = new VisitService(_core);

            var p = await svcP.AddPatientAsync(new Patient { FullName = "Date Patient", Phone = "0900000080" });
            var doc = await svcD.AddDoctorAsync(new Doctor { FullName = "Dr. Date" });

            await svcV.AddVisitAsync(new Visit { PatientId = p.PatientId, DoctorId = doc.DoctorId, VisitTime = "2024-06-01 10:00:00", Status = "Completed" });
            await svcV.AddVisitAsync(new Visit { PatientId = p.PatientId, DoctorId = doc.DoctorId, VisitTime = "2024-06-02 11:00:00", Status = "Completed" });
            await svcV.AddVisitAsync(new Visit { PatientId = p.PatientId, DoctorId = doc.DoctorId, VisitTime = "2024-06-15 09:00:00", Status = "Waiting" });

            var june1 = await svcV.GetVisitsAsync(0, 50, "2024-06-01");
            Assert.Single(june1);

            var allJune = await svcV.GetVisitsAsync(0, 50, "2024-06");
            Assert.Equal(3, allJune.Count);
        }

        [Fact]
        public async Task GetTodayVisits_ReturnsCorrectCount()
        {
            var svcP = new PatientService(_core);
            var svcD = new DoctorService(_core);
            var svcV = new VisitService(_core);

            var p = await svcP.AddPatientAsync(new Patient { FullName = "Today Patient", Phone = "0900000095" });
            var doc = await svcD.AddDoctorAsync(new Doctor { FullName = "Dr. Today" });

            await svcV.AddVisitAsync(new Visit { PatientId = p.PatientId, DoctorId = doc.DoctorId, VisitTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), Status = "Waiting" });
            await svcV.AddVisitAsync(new Visit { PatientId = p.PatientId, DoctorId = doc.DoctorId, VisitTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), Status = "In Progress" });

            var count = await svcV.GetTodayVisitsCountAsync();
            Assert.Equal(2, count);
        }
    }

    // ======================== INVOICE SERVICE ========================

    public class InvoiceServiceTests : ServiceTestBase
    {
        [Fact]
        public async Task GenerateInvoice_CreatesItemsFromPrescriptionAndServices()
        {
            var svcP = new PatientService(_core);
            var svcD = new DoctorService(_core);
            var svcM = new MedicineService(_core);
            var svcS = new ServiceCatalogService(_core);
            var svcV = new VisitService(_core);
            var svcI = new InvoiceService(_core);

            var p = await svcP.AddPatientAsync(new Patient { FullName = "Invoice Patient", Phone = "0900000100" });
            var doc = await svcD.AddDoctorAsync(new Doctor { FullName = "Dr. Invoice" });
            var med = await svcM.AddMedicineAsync(new Medicine { MedicineName = "Invoice Med", Price = 10000, StockQuantity = 50 });
            var svc = await svcS.AddServiceAsync(new Service { ServiceName = "Invoice Service", Price = 300000 });

            var v = await svcV.AddVisitAsync(new Visit { PatientId = p.PatientId, DoctorId = doc.DoctorId, VisitTime = "2024-07-01 10:00:00", Status = "Completed" });
            await svcV.SavePrescriptionDetailsAsync(v.VisitId, new System.Collections.Generic.List<PrescriptionDetail>
            {
                new PrescriptionDetail { MedicineId = med.MedicineId, Quantity = 10, Dosage = "1v/ngay" }
            });
            await svcV.SaveServiceOrdersAsync(v.VisitId, new System.Collections.Generic.List<ServiceOrder>
            {
                new ServiceOrder { ServiceId = svc.ServiceId }
            });

            await svcI.GenerateOrUpdateInvoiceAsync(v.VisitId);
            var invoice = await svcI.GetInvoiceByVisitIdAsync(v.VisitId);

            Assert.NotNull(invoice);
            Assert.Equal("Unpaid", invoice.Status);
            Assert.NotEmpty(invoice.Items);

            var medItem = invoice.Items.FirstOrDefault(i => i.ItemType == "Medicine");
            Assert.NotNull(medItem);
            Assert.Equal(10000, medItem.UnitPrice);
            Assert.Equal(10, medItem.Quantity);
            Assert.Equal(100000, medItem.Subtotal);

            var svcItem = invoice.Items.FirstOrDefault(i => i.ItemType == "Service");
            Assert.NotNull(svcItem);
            Assert.Equal(300000, svcItem.UnitPrice);
        }

        [Fact]
        public async Task AddPayment_DeductsStockAndUpdatesStatus()
        {
            var svcP = new PatientService(_core);
            var svcD = new DoctorService(_core);
            var svcM = new MedicineService(_core);
            var svcV = new VisitService(_core);
            var svcI = new InvoiceService(_core);

            var p = await svcP.AddPatientAsync(new Patient { FullName = "Pay Patient", Phone = "0900000110" });
            var doc = await svcD.AddDoctorAsync(new Doctor { FullName = "Dr. Pay" });
            var med = await svcM.AddMedicineAsync(new Medicine { MedicineName = "Pay Med", Price = 5000, StockQuantity = 100 });

            var v = await svcV.AddVisitAsync(new Visit { PatientId = p.PatientId, DoctorId = doc.DoctorId, VisitTime = "2024-08-01 10:00:00", Status = "Completed" });
            await svcV.SavePrescriptionDetailsAsync(v.VisitId, new System.Collections.Generic.List<PrescriptionDetail>
            {
                new PrescriptionDetail { MedicineId = med.MedicineId, Quantity = 30, Dosage = "1v/ngay" }
            });
            await svcI.GenerateOrUpdateInvoiceAsync(v.VisitId);

            await svcI.AddPaymentAsync(new Payment { VisitId = v.VisitId, Amount = 150000, PaymentMethod = "Cash", PaymentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") });

            var invoice = await svcI.GetInvoiceByVisitIdAsync(v.VisitId);
            Assert.Equal("Paid", invoice.Status);

            var updatedMed = (await svcM.GetMedicinesAsync()).First(x => x.MedicineId == med.MedicineId);
            Assert.Equal(70, updatedMed.StockQuantity);
            Assert.Equal(0, updatedMed.ReservedQuantity);
        }

        [Fact]
        public async Task AddPayment_OnlyDeductsStockOnce()
        {
            var svcP = new PatientService(_core);
            var svcD = new DoctorService(_core);
            var svcM = new MedicineService(_core);
            var svcV = new VisitService(_core);
            var svcI = new InvoiceService(_core);

            var p = await svcP.AddPatientAsync(new Patient { FullName = "Dedup Patient", Phone = "0900000120" });
            var doc = await svcD.AddDoctorAsync(new Doctor { FullName = "Dr. Dedup" });
            var med = await svcM.AddMedicineAsync(new Medicine { MedicineName = "Dedup Med", Price = 1000, StockQuantity = 50 });

            var v = await svcV.AddVisitAsync(new Visit { PatientId = p.PatientId, DoctorId = doc.DoctorId, VisitTime = "2024-09-01 10:00:00", Status = "Completed" });
            await svcV.SavePrescriptionDetailsAsync(v.VisitId, new System.Collections.Generic.List<PrescriptionDetail>
            {
                new PrescriptionDetail { MedicineId = med.MedicineId, Quantity = 5, Dosage = "1v/ngay" }
            });
            await svcI.GenerateOrUpdateInvoiceAsync(v.VisitId);

            await svcI.AddPaymentAsync(new Payment { VisitId = v.VisitId, Amount = 5000, PaymentMethod = "Cash", PaymentTime = "2024-09-01 12:00:00" });
            var stockAfterFirst = (await svcM.GetMedicinesAsync()).First(x => x.MedicineId == med.MedicineId).StockQuantity;

            await svcI.AddPaymentAsync(new Payment { VisitId = v.VisitId, Amount = 5000, PaymentMethod = "Transfer", PaymentTime = "2024-09-01 14:00:00" });
            var stockAfterSecond = (await svcM.GetMedicinesAsync()).First(x => x.MedicineId == med.MedicineId).StockQuantity;

            Assert.Equal(45, stockAfterFirst);
            Assert.Equal(stockAfterFirst, stockAfterSecond);
        }

        [Fact]
        public async Task PartialPayment_SetsStatusToPartial()
        {
            var svcP = new PatientService(_core);
            var svcD = new DoctorService(_core);
            var svcM = new MedicineService(_core);
            var svcV = new VisitService(_core);
            var svcI = new InvoiceService(_core);

            var p = await svcP.AddPatientAsync(new Patient { FullName = "Partial Patient", Phone = "0900000130" });
            var doc = await svcD.AddDoctorAsync(new Doctor { FullName = "Dr. Partial" });
            var med = await svcM.AddMedicineAsync(new Medicine { MedicineName = "Partial Med", Price = 10000, StockQuantity = 10 });

            var v = await svcV.AddVisitAsync(new Visit { PatientId = p.PatientId, DoctorId = doc.DoctorId, VisitTime = "2024-10-01 10:00:00", Status = "Completed" });
            await svcV.SavePrescriptionDetailsAsync(v.VisitId, new System.Collections.Generic.List<PrescriptionDetail>
            {
                new PrescriptionDetail { MedicineId = med.MedicineId, Quantity = 2, Dosage = "1v/ngay" }
            });
            await svcI.GenerateOrUpdateInvoiceAsync(v.VisitId);

            await svcI.AddPaymentAsync(new Payment { VisitId = v.VisitId, Amount = 10000, PaymentMethod = "Cash", PaymentTime = "2024-10-01 12:00:00" });
            var invoice = await svcI.GetInvoiceByVisitIdAsync(v.VisitId);
            Assert.Equal("Partial", invoice.Status);
        }
    }

    // ======================== DASHBOARD SERVICE ========================

    public class DashboardServiceTests : ServiceTestBase
    {
        [Fact]
        public async Task CreateAndGetAppointments()
        {
            var svcP = new PatientService(_core);
            var svcD = new DoctorService(_core);
            var svcDb = new DashboardService(_core);

            var p = await svcP.AddPatientAsync(new Patient { FullName = "Appt Patient", Phone = "0900000140" });
            var doc = await svcD.AddDoctorAsync(new Doctor { FullName = "Dr. Appt" });

            var appt = new Appointment
            {
                PatientId = p.PatientId,
                DoctorId = doc.DoctorId,
                AppointmentTime = DateTime.Now.AddDays(30).ToString("yyyy-MM-dd 14:00:00"),
                Status = "Scheduled"
            };
            await svcDb.CreateAppointmentAsync(appt);

            var upcoming = await svcDb.GetUpcomingAppointmentsAsync(10);
            Assert.Contains(upcoming, a => a.PatientId == p.PatientId && a.DoctorId == doc.DoctorId);
        }

        [Fact]
        public async Task GetVisitCountByDay_ReturnsAggregatedData()
        {
            var svcP = new PatientService(_core);
            var svcD = new DoctorService(_core);
            var svcV = new VisitService(_core);
            var svcDb = new DashboardService(_core);

            var p = await svcP.AddPatientAsync(new Patient { FullName = "Chart Patient", Phone = "0900000150" });
            var doc = await svcD.AddDoctorAsync(new Doctor { FullName = "Dr. Chart" });

            var today = DateTime.Today;
            await svcV.AddVisitAsync(new Visit { PatientId = p.PatientId, DoctorId = doc.DoctorId, VisitTime = today.AddDays(-2).ToString("yyyy-MM-dd 09:00:00"), Status = "Completed" });
            await svcV.AddVisitAsync(new Visit { PatientId = p.PatientId, DoctorId = doc.DoctorId, VisitTime = today.AddDays(-2).ToString("yyyy-MM-dd 10:00:00"), Status = "Completed" });
            await svcV.AddVisitAsync(new Visit { PatientId = p.PatientId, DoctorId = doc.DoctorId, VisitTime = today.AddDays(-1).ToString("yyyy-MM-dd 09:00:00"), Status = "Completed" });

            var counts = await svcDb.GetVisitCountByDayAsync(30);
            var day1 = today.AddDays(-2).ToString("yyyy-MM-dd");
            var day2 = today.AddDays(-1).ToString("yyyy-MM-dd");
            Assert.True(counts.ContainsKey(day1));
            Assert.Equal(2, counts[day1]);
            Assert.Equal(1, counts[day2]);
        }
    }

    // ======================== INTEGRATION: FULL FLOW ========================

    public class FullFlowTests : ServiceTestBase
    {
        [Fact]
        public async Task CompleteExaminationToPaymentFlow()
        {
            // Simulate: Add patient + doctor -> Create visit -> Add prescription + services -> Complete -> Generate invoice -> Pay -> Verify stock

            var svcP = new PatientService(_core);
            var svcD = new DoctorService(_core);
            var svcM = new MedicineService(_core);
            var svcS = new ServiceCatalogService(_core);
            var svcV = new VisitService(_core);
            var svcI = new InvoiceService(_core);

            var p = await svcP.AddPatientAsync(new Patient { FullName = "Full Flow Patient", Phone = "0999999999" });
            var doc = await svcD.AddDoctorAsync(new Doctor { FullName = "Dr. Full Flow", Specialty = "Da khoa" });
            var med = await svcM.AddMedicineAsync(new Medicine { MedicineName = "Full Flow Med", Price = 15000, StockQuantity = 200 });
            var svc = await svcS.AddServiceAsync(new Service { ServiceName = "Full Flow Service", Price = 500000 });

            // Step 1: Create visit (basic, no prescription - will add via SavePrescriptionDetailsAsync)
            var v = await svcV.AddVisitAsync(new Visit
            {
                PatientId = p.PatientId,
                DoctorId = doc.DoctorId,
                VisitTime = "2025-06-01 08:00:00",
                Status = "In Progress",
                Symptoms = "Sot, ho"
            });
            Assert.True(v.VisitId > 0);

            // Step 2: Add prescription (this triggers Stock Reservation: ReservedQuantity += 30)
            await svcV.SavePrescriptionDetailsAsync(v.VisitId, new System.Collections.Generic.List<PrescriptionDetail>
            {
                new PrescriptionDetail { MedicineId = med.MedicineId, Quantity = 30, Dosage = "2v x 3/ngay" }
            });

            // Step 3: Complete visit and re-add prescription (UpdateVisitAsync deletes+re-inserts)
            v.Status = "Completed";
            v.PrescriptionDetails = new System.Collections.Generic.List<PrescriptionDetail>
            {
                new PrescriptionDetail { MedicineId = med.MedicineId, Quantity = 30, Dosage = "2v x 3/ngay" }
            };
            v.ServiceOrders = new System.Collections.Generic.List<ServiceOrder>
            {
                new ServiceOrder { ServiceId = svc.ServiceId }
            };
            await svcV.UpdateVisitAsync(v);

            // Step 4: Re-reserve stock since UpdateVisitAsync doesn't manage stock (delta = 30-30 = 0, no change)
            await svcV.SavePrescriptionDetailsAsync(v.VisitId, new System.Collections.Generic.List<PrescriptionDetail>
            {
                new PrescriptionDetail { MedicineId = med.MedicineId, Quantity = 30, Dosage = "2v x 3/ngay" }
            });

            // Step 5: Generate invoice
            await svcI.GenerateOrUpdateInvoiceAsync(v.VisitId);
            var invoice = await svcI.GetInvoiceByVisitIdAsync(v.VisitId);
            Assert.NotNull(invoice);
            Assert.Equal("Unpaid", invoice.Status);
            Assert.Equal(950000, invoice.TotalAmount); // 30*15000 + 500000
            Assert.Equal(2, invoice.Items.Count);

            // Step 6: Pay in full
            await svcI.AddPaymentAsync(new Payment { VisitId = v.VisitId, Amount = 950000, PaymentMethod = "Cash", PaymentTime = "2025-06-01 17:00:00" });
            invoice = await svcI.GetInvoiceByVisitIdAsync(v.VisitId);
            Assert.Equal("Paid", invoice.Status);
            Assert.Single(invoice.Payments);

            // Step 7: Verify stock deduction
            var finalMed = (await svcM.GetMedicinesAsync()).First(x => x.MedicineId == med.MedicineId);
            Assert.Equal(170, finalMed.StockQuantity); // 200 - 30
            Assert.Equal(0, finalMed.ReservedQuantity);
        }
    }
}
