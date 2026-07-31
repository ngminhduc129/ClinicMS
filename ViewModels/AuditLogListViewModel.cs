using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ClinicMS.Models;
using ClinicMS.Services;

namespace ClinicMS.ViewModels;

public class AuditLogListViewModel : BaseViewModel
{
    private readonly CoreDatabaseService _core;
    private readonly ToastService _toast;

    public ObservableCollection<AuditLog> Logs { get; } = new();

    private bool _loading;
    public bool Loading { get => _loading; set => SetProperty(ref _loading, value); }

    private string _searchTerm = "";
    public string SearchTerm { get => _searchTerm; set => SetProperty(ref _searchTerm, value); }

    private string _entityFilter = "";
    public string EntityFilter { get => _entityFilter; set => SetProperty(ref _entityFilter, value); }

    private string _actionFilter = "";
    public string ActionFilter { get => _actionFilter; set => SetProperty(ref _actionFilter, value); }

    private DateTime? _dateFrom;
    public DateTime? DateFrom { get => _dateFrom; set => SetProperty(ref _dateFrom, value); }

    private DateTime? _dateTo;
    public DateTime? DateTo { get => _dateTo; set => SetProperty(ref _dateTo, value); }

    private int _totalCount;
    public int TotalCount { get => _totalCount; set => SetProperty(ref _totalCount, value); }

    public ObservableCollection<string> EntityNames { get; } = new();
    public ObservableCollection<string> ActionNames { get; } = new();

    private int _offset = 0;
    private const int PageSize = 50;

    public AuditLogListViewModel(CoreDatabaseService core, ToastService toast)
    {
        _core = core;
        _toast = toast;
    }

    public async Task LoadAsync()
    {
        if (Loading) return;
        Loading = true;
        try
        {
            _offset = 0;
            Logs.Clear();
            await LoadFiltersAsync();

            await using var db = _core.Ctx();
            var query = BuildQuery(db);
            TotalCount = await query.CountAsync();
            OnPropertyChanged(nameof(TotalCount));

            var results = await query.OrderByDescending(l => l.LogId)
                                      .Skip(_offset).Take(PageSize)
                                      .ToListAsync();
            foreach (var l in results) Logs.Add(l);
            _offset += results.Count;
        }
        catch (Exception ex)
        {
            _toast.ShowError($"Lỗi khi tải nhật ký: {ex.Message}");
        }
        finally { Loading = false; }
    }

    public async Task LoadMoreAsync()
    {
        if (Loading) return;
        Loading = true;
        try
        {
            await using var db = _core.Ctx();
            var query = BuildQuery(db);
            var results = await query.OrderByDescending(l => l.LogId)
                                      .Skip(_offset).Take(PageSize)
                                      .ToListAsync();
            foreach (var l in results) Logs.Add(l);
            _offset += results.Count;
        }
        catch { }
        finally { Loading = false; }
    }

    public async Task SearchAsync()
    {
        await LoadAsync();
    }

    private async Task LoadFiltersAsync()
    {
        try
        {
            await using var db = _core.Ctx();
            var entities = await db.AuditLogs.Select(l => l.EntityName).Distinct().OrderBy(n => n).ToListAsync();
            var actions = await db.AuditLogs.Select(l => l.Action).Distinct().OrderBy(a => a).ToListAsync();

            EntityNames.Clear();
            EntityNames.Add("");
            foreach (var e in entities) EntityNames.Add(e);

            ActionNames.Clear();
            ActionNames.Add("");
            foreach (var a in actions) ActionNames.Add(a);
        }
        catch { }
    }

    private IQueryable<AuditLog> BuildQuery(Data.ClinicDbContext db)
    {
        var query = db.AuditLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(EntityFilter))
            query = query.Where(l => l.EntityName == EntityFilter);

        if (!string.IsNullOrWhiteSpace(ActionFilter))
            query = query.Where(l => l.Action == ActionFilter);

        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            var term = SearchTerm.ToLower();
            query = query.Where(l =>
                (l.Details != null && l.Details.ToLower().Contains(term)) ||
                (l.EntityName != null && l.EntityName.ToLower().Contains(term)) ||
                (l.Action != null && l.Action.ToLower().Contains(term)));
        }

        if (DateFrom.HasValue)
        {
            var from = DateFrom.Value.ToString("yyyy-MM-dd");
            query = query.Where(l => l.Timestamp != null && l.Timestamp.CompareTo(from) >= 0);
        }

        if (DateTo.HasValue)
        {
            var to = DateTo.Value.AddDays(1).ToString("yyyy-MM-dd");
            query = query.Where(l => l.Timestamp != null && l.Timestamp.CompareTo(to) < 0);
        }

        return query;
    }
}
