using CatHotel.Models;
using CatHotel.Services;

namespace CatHotel.Views.OutcomeSettingViews;

// ViewModel wrapper สำหรับแสดงในรายการ
public class OutcomeRecordViewModel
{
    public int Id { get; set; }
    public double Amount { get; set; }
    public string Note { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public string AmountDisplay => $"฿{Amount:N2}";
    public string NoteDisplay => string.IsNullOrWhiteSpace(Note) ? "(ไม่มีหมายเหตุ)" : Note;
    public string DateDisplay => CreatedAt.ToString("dd/MM/yyyy HH:mm");
}

public partial class OutcomeHistoryView : ContentView
{
    private bool _isInitialized = false;
    private readonly DatabaseService _db;

    public OutcomeHistoryView()
    {
        InitializeComponent();
        _db = App.Database;

        // เติมค่าลงใน Pickers
        var months = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat
                     .MonthNames
                     .Where(m => !string.IsNullOrEmpty(m)).ToList();
        FilterMonthPicker.ItemsSource = months;
        FilterMonthPicker.SelectedIndex = DateTime.Now.Month - 1;

        FilterDayPicker.ItemsSource = Enumerable.Range(1, 31).Select(d => d.ToString("D2")).ToList();
        FilterDayPicker.SelectedIndex = DateTime.Now.Day - 1;

        int current = DateTime.Now.Year;
        var years = Enumerable.Range(current - 10, 11).Reverse().Select(y => y.ToString()).ToList();
        FilterYearPicker.ItemsSource = years;
        FilterYearPicker.SelectedIndex = 0;

        // สมัคร event handler เพิ่มเติม (ป้องกันกรณีไม่ได้ผูกใน XAML)
        FilterMonthPicker.SelectedIndexChanged += OnMonthOrYearChanged;
        FilterYearPicker.SelectedIndexChanged += OnMonthOrYearChanged;

        // ปรับวันให้สอดคล้องกับเดือน/ปีเริ่มต้น
        UpdateDayPicker();

        // Subscribe to outcome events
        OutcomeService.OutcomeAdded += async (s, e) => await RefreshAsync();
        OutcomeService.OutcomeUpdated += async (s, e) => await RefreshAsync();
        OutcomeService.OutcomeDeleted += async (s, e) => await RefreshAsync();

        this.Loaded += OnViewLoaded;
    }

    private async void OnViewLoaded(object sender, EventArgs e)
    {
        if (_isInitialized) return;
        _isInitialized = true;

        try
        {
            await LoadHistoryAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("[OUTCOME_HISTORY][FATAL] " + ex);
            throw;
        }
    }

    // Public method to refresh data
    public async Task RefreshAsync()
    {
        if (_isInitialized)
        {
            await LoadHistoryAsync();
        }
    }

    private async Task LoadHistoryAsync(int? year = null, int? month = null, int? day = null)
    {
        LoadingIndicator.IsVisible = true;
        LoadingIndicator.IsRunning = true;

        try
        {
            List<OutcomeRecord> records;

            if (year == null && month == null && day == null)
                records = await _db.GetAllOutcomeRecordsAsync();
            else
                records = await _db.GetOutcomeRecordsByFilterAsync(year, month, day);

            var vms = records.Select(r => new OutcomeRecordViewModel
            {
                Id = r.Id,
                Amount = r.Amount,
                Note = r.Note,
                CreatedAt = r.CreatedAt
            }).ToList();

            HistoryList.ItemsSource = vms;
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
        }
    }

    // เมื่อ Month หรือ Year เปลี่ยน ให้อัพเดตจำนวนวันให้ถูกต้อง
    private void OnMonthOrYearChanged(object sender, EventArgs e)
    {
        UpdateDayPicker();
    }

    private void UpdateDayPicker()
    {
        // ค่าเริ่มต้น
        int year = DateTime.Now.Year;
        int month = DateTime.Now.Month;

        if (FilterYearPicker.SelectedIndex >= 0 && int.TryParse((string)FilterYearPicker.SelectedItem, out int y))
            year = y;

        if (FilterMonthPicker.SelectedIndex >= 0)
            month = FilterMonthPicker.SelectedIndex + 1;

        int daysInMonth = 1;
        try
        {
            daysInMonth = DateTime.DaysInMonth(year, month);
        }
        catch
        {
            daysInMonth = 31;
        }

        // เก็บค่าที่เลือกเดิม (ถ้ามี)
        string? prevSelected = null;
        if (FilterDayPicker.SelectedIndex >= 0 && FilterDayPicker.SelectedItem is string s)
            prevSelected = s;

        var dayList = Enumerable.Range(1, daysInMonth).Select(d => d.ToString("D2")).ToList();
        FilterDayPicker.ItemsSource = dayList;

        if (prevSelected != null && int.TryParse(prevSelected, out int prevDay))
        {
            if (prevDay <= daysInMonth)
                FilterDayPicker.SelectedIndex = prevDay - 1;
            else
                FilterDayPicker.SelectedIndex = daysInMonth - 1;
        }
        else
        {
            // เลือกวันที่ปัจจุบันถ้าเป็นไปได้
            int today = DateTime.Now.Day;
            FilterDayPicker.SelectedIndex = Math.Min(today - 1, daysInMonth - 1);
        }
    }

    private async void OnFilterTapped(object sender, TappedEventArgs e)
    {
        int? year = null;
        int? month = null;
        int? day = null;

        if (FilterYearPicker.SelectedIndex >= 0 && int.TryParse((string)FilterYearPicker.SelectedItem, out int y))
            year = y;
        if (FilterMonthPicker.SelectedIndex >= 0)
            month = FilterMonthPicker.SelectedIndex + 1;
        if (FilterDayPicker.SelectedIndex >= 0 && int.TryParse((string)FilterDayPicker.SelectedItem, out int d))
            day = d;

        // Validate ranges (extra safety)
        if (month.HasValue && (month < 1 || month > 12))
        {
            await Application.Current!.MainPage!.DisplayAlertAsync("Error", "เดือนต้องอยู่ระหว่าง 1-12", "OK");
            return;
        }
        if (day.HasValue && (day < 1 || day > 31))
        {
            await Application.Current!.MainPage!.DisplayAlertAsync("Error", "วันต้องอยู่ระหว่าง 1-31", "OK");
            return;
        }

        await LoadHistoryAsync(year, month, day);
    }
}