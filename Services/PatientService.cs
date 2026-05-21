using Microsoft.EntityFrameworkCore;
using ClinicMS.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ClinicMS.Services;

public class PatientService
{
    private readonly CoreDatabaseService _core;

    public PatientService(CoreDatabaseService core)
    {
        _core = core;
    }

    public async Task<int> GetPatientsCountAsync()
    {
        await using var db = _core.Ctx();
        return await db.Patients.CountAsync();
    }

    public async Task<List<string>> GetAllPatientNamesAsync()
    {
        await using var db = _core.Ctx();
        return await db.Patients.Select(p => p.FullName ?? "").Where(n => n != "").OrderBy(n => n).ToListAsync();
    }

    public async Task<Dictionary<string, int>> GetPatientsLookupAsync()
    {
        await using var db = _core.Ctx();
        var list = await db.Patients
            .Select(p => new { p.PatientId, p.FullName })
            .ToListAsync();

        var dict = new Dictionary<string, int>();
        foreach (var p in list)
        {
            var key = $"{p.FullName} (ID: {p.PatientId})";
            dict[key] = p.PatientId;
        }
        return dict;
    }

    public async Task<List<Patient>> GetPatientsWithContactsAsync(int offset = 0, int limit = 50)
    {
        await using var db = _core.Ctx();
        var patients = await db.Patients
            .Include(p => p.PatientContacts.Where(pc => pc.IsPrimary == 1))
                .ThenInclude(pc => pc.Contact)
            .OrderByDescending(p => p.PatientId)
            .Skip(offset)
            .Take(limit)
            .ToListAsync();

        foreach (var p in patients)
        {
            var pc = p.PatientContacts.FirstOrDefault();
            if (pc?.Contact != null)
            {
                p.Phone = pc.Contact.Phone;
                p.Email = pc.Contact.Email;
                p.Address = pc.Contact.Address;
            }
        }
        return patients;
    }

    public async Task<List<Patient>> SearchPatientsAsync(string searchTerm, int offset = 0, int limit = 50)
    {
        await using var db = _core.Ctx();
        var term = searchTerm.ToLower();
        
        var query = db.Patients
            .Include(p => p.PatientContacts.Where(pc => pc.IsPrimary == 1))
                .ThenInclude(pc => pc.Contact)
            .Where(p => p.FullName.ToLower().Contains(term) || 
                        p.PatientContacts.Any(pc => pc.IsPrimary == 1 && pc.Contact != null && pc.Contact.Phone != null && pc.Contact.Phone.Contains(term)))
            .OrderByDescending(p => p.PatientId);

        var patients = await query.Skip(offset).Take(limit).ToListAsync();

        foreach (var p in patients)
        {
            var pc = p.PatientContacts.FirstOrDefault();
            if (pc?.Contact != null)
            {
                p.Phone = pc.Contact.Phone;
                p.Email = pc.Contact.Email;
                p.Address = pc.Contact.Address;
            }
        }
        return patients;
    }

    public async Task<int> GetPatientsSearchCountAsync(string searchTerm)
    {
        await using var db = _core.Ctx();
        var term = searchTerm.ToLower();
        return await db.Patients
            .Where(p => p.FullName.ToLower().Contains(term) || 
                        p.PatientContacts.Any(pc => pc.IsPrimary == 1 && pc.Contact != null && pc.Contact.Phone != null && pc.Contact.Phone.Contains(term)))
            .CountAsync();
    }

    public async Task<Patient?> GetPatientByNameAsync(string name)
    {
        await using var db = _core.Ctx();
        return await db.Patients.FirstOrDefaultAsync(p => p.FullName == name);
    }

    public async Task<List<Patient>> GetPatientsAsync()
    {
        await using var db = _core.Ctx();
        return await db.Patients.OrderBy(p => p.FullName).ToListAsync();
    }

    public async Task<Patient> AddPatientAsync(Patient p)
    {
        await using var db = _core.Ctx();
        db.Patients.Add(p);
        await db.SaveChangesAsync();

        var contact = new Contact { Phone = p.Phone, Email = p.Email, Address = p.Address };
        db.Contacts.Add(contact);
        await db.SaveChangesAsync();

        db.PatientContacts.Add(new PatientContact
        {
            PatientId = p.PatientId,
            ContactId = contact.ContactId,
            IsPrimary = 1
        });
        await db.SaveChangesAsync();
        return p;
    }

    public async Task UpdatePatientAsync(Patient p)
    {
        await using var db = _core.Ctx();
        db.Patients.Update(p);

        var pc = await db.PatientContacts
                         .Include(x => x.Contact)
                         .FirstOrDefaultAsync(x => x.PatientId == p.PatientId && x.IsPrimary == 1);
        
        if (pc?.Contact != null)
        {
            pc.Contact.Phone = p.Phone;
            pc.Contact.Email = p.Email;
            pc.Contact.Address = p.Address;
            db.Contacts.Update(pc.Contact);
        }
        else
        {
            var contact = new Contact { Phone = p.Phone, Email = p.Email, Address = p.Address };
            db.Contacts.Add(contact);
            await db.SaveChangesAsync();

            db.PatientContacts.Add(new PatientContact
            {
                PatientId = p.PatientId,
                ContactId = contact.ContactId,
                IsPrimary = 1
            });
        }
        await db.SaveChangesAsync();
    }

    public async Task DeletePatientAsync(int id)
    {
        await using var db = _core.Ctx();
        var p = await db.Patients.FindAsync(id);
        if (p != null)
        {
            var pcs = db.PatientContacts.Where(x => x.PatientId == id);
            db.PatientContacts.RemoveRange(pcs);

            var visits = db.Visits.Where(v => v.PatientId == id);
            db.Visits.RemoveRange(visits);

            db.Patients.Remove(p);
            await db.SaveChangesAsync();
        }
    }

    public async Task<(Patient? patient, Contact? contact)> GetPatientWithContactAsync(int id)
    {
        await using var db = _core.Ctx();
        var p = await db.Patients.FindAsync(id);
        var pc = await db.PatientContacts
                         .Include(x => x.Contact)
                         .FirstOrDefaultAsync(x => x.PatientId == id && x.IsPrimary == 1);

        Contact? c = pc?.Contact;
        return (p, c);
    }

    public async Task<List<Visit>> GetPatientVisitsAsync(int patientId)
    {
        await using var db = _core.Ctx();
        return await db.Visits
                       .Include(v => v.Doctor)
                       .Include(v => v.PrescriptionDetails)
                           .ThenInclude(pd => pd.Medicine)
                       .Include(v => v.ServiceOrders)
                           .ThenInclude(so => so.Service)
                       .Where(v => v.PatientId == patientId)
                       .OrderByDescending(v => v.VisitTime)
                       .ToListAsync();
    }
}
