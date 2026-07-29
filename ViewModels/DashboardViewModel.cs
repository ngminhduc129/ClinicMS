using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Linq;
using ClinicMS.Models;
using ClinicMS.Services;

namespace ClinicMS.ViewModels;

public class DashboardViewModel : BaseViewModel
{
    private readonly DashboardService _dashboardService;
    private readonly PatientService _patientService;
    private readonly DoctorService _doctorService;
    private readonly VisitService _visitService;
    private readonly InvoiceService _invoiceService;
    private readonly ToastService _toast;

    public ObservableCollection<Visit> TodayVisits { get; } = new();
    public ObservableCollection<Appointment> UpcomingAppointments { get; } = new();
    public ObservableCollection<ChartItem> WeeklyStats { get; } = new();

    private int _totalPatients; public int TotalPatients { get => _totalPatients; set => SetProperty(ref _totalPatients, value); }
    private int _totalDoctors;  public int TotalDoctors  { get => _totalDoctors;  set => SetProperty(ref _totalDoctors,  value); }        
    private int _totalVisits;   public int TotalVisits   { get => _totalVisits;   set => SetProperty(ref _totalVisits,   value); }        
    private int _visitsToday;   public int VisitsToday   { get => _visitsToday;   set => SetProperty(ref _visitsToday,   value); }        
    private int _todayInvoices; public int TodayInvoices { get => _todayInvoices; set => SetProperty(ref _todayInvoices, value); }
    private double _todayRevenue;
    public string TodayRevenueDisplay => _todayRevenue.ToString("N0") + " ₫";

    public DashboardViewModel(
        DashboardService dashboardService, 
        PatientService patientService, 
        DoctorService doctorService, 
        VisitService visitService, 
        InvoiceService invoiceService,
        ToastService toast)
    {
        _dashboardService = dashboardService;
        _patientService = patientService;
        _doctorService = doctorService;
        _visitService = visitService;
        _invoiceService = invoiceService;
        _toast = toast;
    }

    public async Task LoadAsync()
    {
        try {
            TotalPatients = await _patientService.GetPatientsCountAsync();
            TotalDoctors = await _doctorService.GetDoctorsCountAsync();
            TotalVisits = await _visitService.GetVisitsCountAsync();
            VisitsToday = await _visitService.GetTodayVisitsCountAsync();

            var (invCount, invTotal) = await _invoiceService.GetTodayInvoiceStatsAsync();
            TodayInvoices = invCount;
            _todayRevenue = invTotal;
            OnPropertyChanged(nameof(TodayRevenueDisplay));

            var appointments = await _dashboardService.GetUpcomingAppointmentsAsync(5);
            UpcomingAppointments.Clear();
            foreach (var a in appointments) UpcomingAppointments.Add(a);

            await LoadWeeklyStatsAsync();

            var todayList = await _visitService.GetTodayVisitsAsync();
            TodayVisits.Clear();
            foreach (var v in todayList) TodayVisits.Add(v);
        } 
        catch (Exception ex)
        {
            _toast.ShowError($"Lỗi khi tải dữ liệu tổng quan: {ex.Message}");
        }
    }

    private async Task LoadWeeklyStatsAsync()
    {
        var data = await _dashboardService.GetVisitCountByDayAsync(7);
        WeeklyStats.Clear();
        int max = data.Values.Any() ? data.Values.Max() : 1;
        if (max < 5) max = 5;

        for (int i = 6; i >= 0; i--)
        {
            var date = DateTime.Today.AddDays(-i);
            var dateStr = date.ToString("yyyy-MM-dd");
            int count = data.ContainsKey(dateStr) ? data[dateStr] : 0;
            WeeklyStats.Add(new ChartItem { Label = i == 0 ? "Hôm nay" : date.ToString("dd/MM"), Value = count, Percentage = (double)count / max * 100 });
        }
    }
}
