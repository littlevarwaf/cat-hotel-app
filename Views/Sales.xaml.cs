using CatHotel.Models;
using Microcharts;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

namespace CatHotel.Views;

public partial class Sales : ContentView
{
    // --- Fields & State ---
    private bool _isInitialized = false;
    private List<(DateTime Month, double Income, double Expense)> _allMonths;
    private int _currentStartIndex = 0;
    private const int _monthsPerPage = 6;

    // --- ObservableCollection for Month Label (for XAML binding) ---
    public ObservableCollection<string> MonthLabels { get; set; } = new();

    // --- Helper property: รับปีจาก MainViewModel (Context แม่) ---
    private int SelectedYear => (BindingContext as MainViewModel)?.SelectedYear ?? DateTime.Now.Year;

    public Sales()
    {
        InitializeComponent();

        // ----- BindingContextChanged จะถูกเรียกทุกครั้งที่ฝังใน Tab หรือ ViewHost -----
        this.BindingContextChanged += (s, e) =>
        {
            SubscribeToVm();
            SyncGraphToSelectedYear(); // โหลดข้อมูลกราฟ+เดือนทันทีที่ปีใน VM เปลี่ยน
        };

        // --- Setup MonthPicker (ตัวเลือกเดือน) ---
        MonthPicker.ItemsSource = new List<string>
        {
            "January","February","March","April","May","June",
            "July","August","September","October","November","December"
        };
        MonthPicker.SelectedIndex = DateTime.Now.Month - 1;
        UpdateMonthDisplay();

        // --- RoomMonthPicker (อีกกราฟ) ---
        RoomMonthPicker.ItemsSource = new List<string>
        {
            "January","February","March","April","May","June",
            "July","August","September","October","November","December"
        };
        RoomMonthPicker.SelectedIndex = DateTime.Now.Month - 1;
        UpdateRoomMonthDisplay();

        this.Loaded += OnViewLoaded;
    }

    // -- Subscribe VM เพื่อรับ Event เมื่อปีเปลี่ยน --
    private void SubscribeToVm()
    {
        if (BindingContext is MainViewModel vm)
        {
            vm.PropertyChanged -= MainVm_PropertyChanged;
            vm.PropertyChanged += MainVm_PropertyChanged;
        }
    }
    private void MainVm_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.SelectedYear))
            SyncGraphToSelectedYear();
    }
    private async void SyncGraphToSelectedYear()
    {
        await LoadAllYearDataAsync();
    }

    // --- OnTabActivated สำหรับ TabHost ---
    public void OnTabActivated() => RefreshSalesData();

    private async void OnViewLoaded(object? sender, EventArgs e)
    {
        if (_isInitialized) return;
        _isInitialized = true;

        try
        {
            await App.Database.InitializeAsync();
            await LoadAllYearDataAsync();
            await RenderBestCategoryDonut(MonthPicker.SelectedIndex);
            await LoadRoomUsageFromDbAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("[SALES][FATAL] " + ex);
            throw;
        }
    }

    // ----- รีเฟรชทั้งหน้า (เช่นถูก Tab ปลุก) -----
    public async void RefreshSalesData()
    {
        try
        {
            await App.Database.InitializeAsync();
            await LoadAllYearDataAsync();
            await RenderBestCategoryDonut(MonthPicker.SelectedIndex);
            await LoadRoomUsageFromDbAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("[SALES][REFRESH][FATAL] " + ex);
        }
    }

    // --- ดึงข้อมูลกราฟ (รายปี) แล้ว render ชื่อเดือนและกราฟแท่ง (Bar Chart) ---
    public async Task LoadAllYearDataAsync()
    {
        _allMonths = await App.Database.GetMonthlySalesByYearAsync(SelectedYear);
        _currentStartIndex = 0;
        await RenderCurrentBarChartPage();
    }

    private async Task RenderCurrentBarChartPage()
    {
        var culture = new CultureInfo("en-US");
        var entries = new List<ChartEntry>();

        var pageItems = _allMonths
            .Skip(_currentStartIndex)
            .Take(_monthsPerPage)
            .ToList();

        foreach (var item in pageItems)
        {
            entries.Add(new ChartEntry((float)item.Income)
            {
                Label = "",
                ValueLabel = item.Income.ToString("0"),
                Color = SKColor.Parse("#4caf50"),
            });
            entries.Add(new ChartEntry((float)item.Expense)
            {
                Label = "",
                ValueLabel = item.Expense.ToString("0"),
                Color = SKColor.Parse("#f44336"),
            });
        }

        // แก้กราฟว่าง (กัน crash)
        if (entries.Count > 0 && entries.All(e => e.Value == 0))
        {
            entries.Add(new ChartEntry(0.1f)
            {
                Color = SKColor.Parse("#00FFFFFF"),
                Label = "",
                ValueLabel = "",
            });
        }

        // --- set ค่าใหม่ให้ Microcharts View ---
        Income7MonthsChart.Chart = new BarChart
        {
            Entries = entries,
            LabelTextSize = 20,
            ValueLabelOrientation = Orientation.Horizontal,
            LabelOrientation = Orientation.Horizontal,
            BackgroundColor = SKColors.White,
            Margin = 20
        };

        // --- Update ชื่อเดือน ---
        MonthLabels.Clear();
        foreach (var label in pageItems.Select(m => m.Month.ToString("MMM", culture)))
            MonthLabels.Add(label);
    }

    // --- ปุ่มสลับหน้าเดือน (กรณีมี Jan-Jun/Jul-Dec) ---
    public async Task PrevPage()
    {
        if (_currentStartIndex >= _monthsPerPage)
        {
            _currentStartIndex -= _monthsPerPage;
            await RenderCurrentBarChartPage();
        }
    }
    public async Task NextPage()
    {
        if (_currentStartIndex + _monthsPerPage < _allMonths.Count)
        {
            _currentStartIndex += _monthsPerPage;
            await RenderCurrentBarChartPage();
        }
    }
    private async void PrevButton_Clicked(object sender, EventArgs e) => await PrevPage();
    private async void NextButton_Clicked(object sender, EventArgs e) => await NextPage();

    // --- ปุ่มเปลี่ยนปี (binding กับ MainViewModel) ---
    private void PrevYearButton_Clicked(object sender, EventArgs e)
    {
        if (BindingContext is MainViewModel vm && vm.SelectedYear > 2000)
            vm.SelectedYear--;
    }
    private void NextYearButton_Clicked(object sender, EventArgs e)
    {
        if (BindingContext is MainViewModel vm && vm.SelectedYear < DateTime.Now.Year)
            vm.SelectedYear++;
    }

    // ---- เดือน/Picker/Chart อื่น ตามเดิม ----
    private void UpdateMonthDisplay()
    {
        var monthName = MonthPicker.SelectedIndex >= 0
            ? MonthPicker.ItemsSource[MonthPicker.SelectedIndex]?.ToString()
            : "-";
        MonthDisplayLabel.Text = $"Month : {monthName}";
    }
    private void UpdateRoomMonthDisplay()
    {
        var monthName = RoomMonthPicker.SelectedIndex >= 0
            ? RoomMonthPicker.ItemsSource[RoomMonthPicker.SelectedIndex]?.ToString()
            : "-";
        RoomMonthDisplayLabel.Text = $"Month : {monthName}";
    }
    private async void MonthPicker_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (MonthPicker.SelectedIndex < 0) return;
        UpdateMonthDisplay();
        await RenderBestCategoryDonut(MonthPicker.SelectedIndex);
    }
    private async void RoomMonthPicker_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (RoomMonthPicker.SelectedIndex < 0) return;
        UpdateRoomMonthDisplay();
        await LoadRoomUsageFromDbAsync();
    }

    private async Task RenderBestCategoryDonut(int monthIndex0Based)
    {
        int year = DateTime.Now.Year;
        int month = monthIndex0Based + 1;

        var byCategory = await App.Database.GetItemTypeQuantityByMonthAsync(year, month);

        if (byCategory.Count == 0 || byCategory.Sum(x => x.Value) <= 0)
        {
            BestCategoryDonutChart.Chart = new DonutChart
            {
                Entries = new[]
                {
                    new ChartEntry(1)
                    {
                        Label = "No Data",
                        ValueLabel = "",
                        Color = SKColor.Parse("#CCCCCC")
                    }
                },
                HoleRadius = 0.6f,
                BackgroundColor = SKColors.White,
                Margin = 0
            };
            return;
        }

        var colors = new[]
        {
            SKColor.Parse("#FF6B6B"),
            SKColor.Parse("#4ECDC4"),
            SKColor.Parse("#45B7D1"),
            SKColor.Parse("#FFA07A"),
            SKColor.Parse("#98D8C8")
        };

        float total = byCategory.Sum(x => x.Value);
        int i = 0;

        var entries = byCategory
            .OrderByDescending(x => x.Value)
            .Select(x =>
            {
                var percent = total > 0 ? (x.Value / total) * 100f : 0f;
                return new ChartEntry(x.Value)
                {
                    Label = x.Key,
                    ValueLabel = $"{x.Value:0} ({percent:0.#}%)",
                    Color = colors[i++ % colors.Length],
                };
            })
            .ToList();

        BestCategoryDonutChart.Chart = new DonutChart
        {
            Entries = entries,
            HoleRadius = 0.6f,
            BackgroundColor = SKColors.White,
            Margin = 0
        };
    }

    private async Task LoadRoomUsageFromDbAsync()
    {
        var (large, medium, small) =
            await App.Database.GetRoomUsageCountByTypeAsync(DateTime.Now.Year, RoomMonthPicker.SelectedIndex + 1);

        SetRoomUsageChart(large, medium, small);
    }

    private void SetRoomUsageChart(int large, int medium, int small)
    {
        var entries = new List<ChartEntry>
        {
            new ChartEntry(large)  { Label="Big",    ValueLabel=large.ToString(),  Color=SKColor.Parse("#FF6B6B") },
            new ChartEntry(medium) { Label="Medium", ValueLabel=medium.ToString(), Color=SKColor.Parse("#4ECDC4") },
            new ChartEntry(small)  { Label="Small",  ValueLabel=small.ToString(),  Color=SKColor.Parse("#45B7D1") },
        };

        RoomUsageBarChart.Chart = new BarChart
        {
            Entries = entries,
            LabelTextSize = 30,
            ValueLabelOrientation = Orientation.Horizontal,
            LabelOrientation = Orientation.Horizontal,
            BackgroundColor = SKColors.White,
            Margin = 30
        };
    }
}