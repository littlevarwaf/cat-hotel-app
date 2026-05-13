using System;
using System.Linq;
using System.Collections.Generic;
using CatHotel.Models;
using CatHotel.Services;

namespace CatHotel.Views;

// ViewModel wrapper สำหรับแสดงในรายการ
public class OutcomeRecordViewModel
{
    public int Id           { get; set; }
    public double Amount    { get; set; }
    public string Note      { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public string AmountDisplay => $"฿{Amount:N2}";
    public string NoteDisplay   => string.IsNullOrWhiteSpace(Note) ? "(ไม่มีหมายเหตุ)" : Note;
    public string DateDisplay   => CreatedAt.ToString("dd/MM/yyyy HH:mm");
}

public partial class Outcomehistory : ContentPage
{
    private readonly DatabaseService _db;

    public Outcomehistory()
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
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadHistoryAsync();
    }

    private async Task LoadHistoryAsync(int? year = null, int? month = null, int? day = null)
    {
        LoadingIndicator.IsVisible  = true;
        LoadingIndicator.IsRunning  = true;

        try
        {
            List<OutcomeRecord> records;

            if (year == null && month == null && day == null)
                records = await _db.GetAllOutcomeRecordsAsync();
            else
                records = await _db.GetOutcomeRecordsByFilterAsync(year, month, day);

            var vms = records.Select(r => new OutcomeRecordViewModel
            {
                Id        = r.Id,
                Amount    = r.Amount,
                Note      = r.Note,
                CreatedAt = r.CreatedAt
            }).ToList();

            HistoryList.ItemsSource = vms;
        }
        finally
        {
            LoadingIndicator.IsRunning  = false;
            LoadingIndicator.IsVisible  = false;
        }
    }

    private async void OnAddIncomeClicked(object sender, EventArgs e)
    {
        await NavigationService.GoToAsync(NavigationService.OutcomePage);
    }

    private async void OnHistoryIncomeClicked(object sender, EventArgs e)
    {
        // อยู่หน้านี้อยู่แล้ว — รีโหลด
        await LoadHistoryAsync();
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

    [Obsolete]
    private async void OnFilterTapped(object sender, TappedEventArgs e)
    {
        int? year  = null;
        int? month = null;
        int? day   = null;

        if (FilterYearPicker.SelectedIndex >= 0 && int.TryParse((string)FilterYearPicker.SelectedItem, out int y)) year  = y;
        if (FilterMonthPicker.SelectedIndex >= 0) month = FilterMonthPicker.SelectedIndex + 1;
        if (FilterDayPicker.SelectedIndex >= 0 && int.TryParse((string)FilterDayPicker.SelectedItem, out int d)) day = d;

        // Validate ranges (extra safety)
        if (month.HasValue && (month < 1 || month > 12))
        {
            await DisplayAlert("Error", "เดือนต้องอยู่ระหว่าง 1-12", "OK");
            return;
        }
        if (day.HasValue && (day < 1 || day > 31))
        {
            await DisplayAlert("Error", "วันต้องอยู่ระหว่าง 1-31", "OK");
            return;
        }

        await LoadHistoryAsync(year, month, day);
    }
}
