using Microsoft.EntityFrameworkCore;
using ClinicMS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ClinicMS.Services;

public class DashboardService
{
    private readonly CoreDatabaseService _core;

    public DashboardService(CoreDatabaseService core)
    {
        _core = core;
    }

    public async Task<List<Appointment>> GetUpcomingAppointmentsAsync(int limit = 10)
    {
        await using var db = _core.Ctx();
        var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        return await db.Appointments
                       .Include(a => a.Patient)
                       .Include(a => a.Doctor)
                       .Where(a => a.AppointmentTime.CompareTo(now) >= 0 && a.Status == "Scheduled")
                       .OrderBy(a => a.AppointmentTime)
                       .Take(limit)
                       .ToListAsync();
    }

    public async Task<Dictionary<string, int>> GetVisitCountByDayAsync(int daysBack = 7)
    {
        await using var db = _core.Ctx();
        var startDate = DateTime.Today.AddDays(-daysBack + 1).ToString("yyyy-MM-dd");
        
        var list = await db.Visits
                           .Where(v => v.PatientId > 0 && v.VisitTime != null && v.VisitTime.CompareTo(startDate) >= 0)
                           .ToListAsync();

        return list.GroupBy(v => v.VisitTime?.Length >= 10 ? v.VisitTime.Substring(0, 10) : "")
                   .Where(g => !string.IsNullOrEmpty(g.Key))
                   .ToDictionary(g => g.Key, g => g.Count());
    }
}
