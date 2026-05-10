using System;
using System.Collections.Generic;
using Microcharts;
using SkiaSharp;
using System.Globalization;
using System.Linq;
using CatHotel.Models;
using SQLite;

namespace CatHotel;

public partial class Sales : ContentPage
{
    public Sales()
    {
        InitializeComponent();

        MonthPicker.ItemsSource = new List<string>
        {
            "January","February","March","April","May","June",
            "July","August","September","October","November","December"
        };

        MonthPicker.SelectedIndex = DateTime.Now.Month - 1;
        UpdateMonthDisplay();

        RoomMonthPicker.ItemsSource = new List<string>
        {   
            "January","February","March","April","May","June",
            "July","August","September","October","November","December"
        };
        RoomMonthPicker.SelectedIndex = DateTime.Now.Month - 1;
        UpdateRoomMonthDisplay();
    }

    private async Task RenderIncome7MonthsBarChartFromDbAsync()
    {
        var culture = new CultureInfo("en-US");
        var data = await App.Database.GetMonthlySalesLast7MonthsAsync();

        var entries = data.Select((x, idx) =>
        {
            var label = x.Month.ToString("MMM", culture);
            var value = (float)x.Total;
            var color = (idx == data.Count - 1) ? SKColor.Parse("#FF6B6B") : SKColor.Parse("#4ECDC4");

            return new ChartEntry(value)
            {
                Label = label,
                ValueLabel = x.Total.ToString("0"),
                Color = color
            };
        }).ToList();

        Income7MonthsChart.Chart = new BarChart
        {
            Entries = entries,
            LabelTextSize = 28,
            ValueLabelOrientation = Orientation.Horizontal,
            LabelOrientation = Orientation.Horizontal,
            BackgroundColor = SKColors.White,
            Margin = 20
        };
    }

    private void SetupDummyIncome7MonthsBarChart()
    {
        var now = DateTime.Now;
        var culture = new CultureInfo("en-US");

        var entries = new List<ChartEntry>();

        for (int i = 6; i >= 0; i--)
        {
            var d = new DateTime(now.Year, now.Month, 1).AddMonths(-i);
            var monthLabel = d.ToString("MMM", culture);

            float value = 10000 + (6 - i) * 2500;
            var color = (i == 0) ? SKColor.Parse("#FF6B6B") : SKColor.Parse("#4ECDC4");

            entries.Add(new ChartEntry(value)
            {
                Label = monthLabel,
                ValueLabel = value.ToString("0"),
                Color = color
            });
        }

        Income7MonthsChart.Chart = new BarChart
        {
            Entries = entries,
            LabelTextSize = 28,
            ValueLabelOrientation = Orientation.Horizontal,
            LabelOrientation = Orientation.Horizontal,
            BackgroundColor = SKColors.White,
            Margin = 20
        };
    }

    //กราฟ 2 โดนัท
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
                    Label = x.Key, // ItemType string (จาก .ToString())
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

    private void UpdateMonthDisplay()
    {
        var monthName = MonthPicker.SelectedIndex >= 0
            ? MonthPicker.ItemsSource[MonthPicker.SelectedIndex]?.ToString()
            : "-";

        MonthDisplayLabel.Text = $"Month : {monthName}";
    }

    private async void MonthPicker_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (MonthPicker.SelectedIndex < 0) return;
        UpdateMonthDisplay();
        await RenderBestCategoryDonut(MonthPicker.SelectedIndex);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            await App.Database.InitializeAsync();
            await RenderIncome7MonthsBarChartFromDbAsync();
            await RenderBestCategoryDonut(MonthPicker.SelectedIndex);
            await LoadRoomUsageFromDbAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("[SALES][FATAL] " + ex);
            throw;
        }
    }
    private async Task LoadRoomUsageFromDbAsync()
    {
        var (large, medium, small) =
            await App.Database.GetRoomUsageCountByTypeAsync(DateTime.Now.Year, RoomMonthPicker.SelectedIndex + 1);

        SetRoomUsageChart(large, medium, small); 
    }

    //กราฟ 3
    private void UpdateRoomMonthDisplay()
    {
        var monthName = RoomMonthPicker.SelectedIndex >= 0
            ? RoomMonthPicker.ItemsSource[RoomMonthPicker.SelectedIndex]?.ToString()
            : "-";

        RoomMonthDisplayLabel.Text = $"Month : {monthName}";
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

    private async void RoomMonthPicker_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (RoomMonthPicker.SelectedIndex < 0) return;

        UpdateRoomMonthDisplay();
        await LoadRoomUsageFromDbAsync();
    }
}