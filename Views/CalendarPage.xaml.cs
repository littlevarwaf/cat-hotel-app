using CatHotel.Models;
using CatHotel.Services;

namespace CatHotel.Views;

public class CalendarDay
{
    public string DayText { get; set; } = "";
    public bool IsCurrentMonth { get; set; }
    public bool IsBooked { get; set; }
    public bool IsSelected { get; set; }
    public bool IsToday { get; set; }
    public DateTime Date { get; set; }

    public Color BgColor
    {
        get
        {
            if (!IsCurrentMonth) return Colors.Transparent;
            if (IsSelected) return Color.FromArgb("#E91E63");
            if (IsBooked) return Color.FromArgb("#FFC107");
            if (IsToday) return Color.FromArgb("#FCE4EC");
            return Colors.Transparent;
        }
    }

    public Color TextColor
    {
        get
        {
            if (!IsCurrentMonth) return Color.FromArgb("#BDBDBD");
            if (IsSelected) return Colors.White;
            return Color.FromArgb("#212121");
        }
    }
}

public partial class CalendarPage : ContentPage
{
    private readonly IRoomRepository _roomRepo;
    private DateTime _currentMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
    private DateTime? _selectedDate;
    private readonly List<int> _bookedDays = new() { 11, 12 };
    private List<CalendarDay> _calendarDays = new();

    public CalendarPage(IRoomRepository roomRepo)
    {
        InitializeComponent();
        _roomRepo = roomRepo;
        BuildCalendarGrid();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        RefreshCalendar();
        await LoadAvailableRoomsAsync();
    }

    private void RefreshCalendar()
    {
        MonthYearLabel.Text  = _currentMonth.ToString("MMMM yyyy");
        MonthFilterLabel.Text = $"Month: {_currentMonth:MMMM}";
        UpdateDayCells();
    }

    private void BuildCalendarGrid()
    {
        CalendarDaysGrid.ColumnDefinitions.Clear();
        CalendarDaysGrid.RowDefinitions.Clear();
        CalendarDaysGrid.Children.Clear();

        for (int c = 0; c < 7; c++)
            CalendarDaysGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        for (int r = 0; r < 6; r++)
            CalendarDaysGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

        for (int row = 0; row < 6; row++)
        {
            for (int col = 0; col < 7; col++)
            {
                int index = row * 7 + col;
                var frame = new Border
                {
                    Padding           = 2,
                    Margin            = new Thickness(1),
                    BackgroundColor   = Colors.Transparent,
                    StrokeThickness   = 0,
                    HeightRequest     = 36,
                    WidthRequest      = 36,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions   = LayoutOptions.Center,
                    StrokeShape       = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(18) }
                };
                var label = new Label
                {
                    FontSize              = 13,
                    HorizontalOptions     = LayoutOptions.Center,
                    VerticalOptions       = LayoutOptions.Center,
                    HorizontalTextAlignment = TextAlignment.Center,
                };
                frame.Content = label;
                var tap = new TapGestureRecognizer { CommandParameter = index };
                tap.Tapped += OnDayCellTapped;
                frame.GestureRecognizers.Add(tap);
                Grid.SetRow(frame, row);
                Grid.SetColumn(frame, col);
                CalendarDaysGrid.Children.Add(frame);
            }
        }
        UpdateDayCells();
    }

    private void UpdateDayCells()
    {
        _calendarDays = BuildDayList();
        var children = CalendarDaysGrid.Children.ToList();
        for (int i = 0; i < children.Count && i < _calendarDays.Count; i++)
        {
            if (children[i] is Border frame && frame.Content is Label lbl)
            {
                var day = _calendarDays[i];
                lbl.Text       = day.DayText;
                lbl.TextColor  = day.TextColor;
                frame.BackgroundColor = day.BgColor;
            }
        }
    }

    private List<CalendarDay> BuildDayList()
    {
        var days = new List<CalendarDay>();
        var firstDay = new DateTime(_currentMonth.Year, _currentMonth.Month, 1);
        int dow    = (int)firstDay.DayOfWeek;
        int offset = dow == 0 ? 6 : dow - 1;

        var prevMonth = _currentMonth.AddMonths(-1);
        int prevTotal = DateTime.DaysInMonth(prevMonth.Year, prevMonth.Month);
        for (int i = offset - 1; i >= 0; i--)
            days.Add(new CalendarDay { DayText = (prevTotal - i).ToString(), IsCurrentMonth = false });

        int daysInMonth = DateTime.DaysInMonth(_currentMonth.Year, _currentMonth.Month);
        var today = DateTime.Today;
        for (int d = 1; d <= daysInMonth; d++)
        {
            var date = new DateTime(_currentMonth.Year, _currentMonth.Month, d);
            days.Add(new CalendarDay
            {
                DayText        = d.ToString(),
                IsCurrentMonth = true,
                Date           = date,
                IsBooked       = _bookedDays.Contains(d),
                IsToday        = date == today,
                IsSelected     = _selectedDate.HasValue && date == _selectedDate.Value,
            });
        }

        int rem = 42 - days.Count;
        for (int i = 1; i <= rem; i++)
            days.Add(new CalendarDay { DayText = i.ToString(), IsCurrentMonth = false });

        return days;
    }

    private async void OnDayCellTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is not int index) return;
        if (index < 0 || index >= _calendarDays.Count) return;
        var day = _calendarDays[index];
        if (!day.IsCurrentMonth || day.IsBooked) return;

        _selectedDate = day.Date;
        UpdateDayCells();
        await LoadAvailableRoomsAsync();
    }

    private async Task LoadAvailableRoomsAsync()
    {
        _ = _selectedDate ?? DateTime.Today;
        SelectedDateLabel.Text = _selectedDate.HasValue
            ? $"วันที่เลือก: {_selectedDate.Value:dd MMM yyyy}"
            : "เลือกวันเพื่อดูห้องว่าง";

        var date = _selectedDate ?? DateTime.Today;
        var rooms = await _roomRepo.GetAvailableRoomsForDateAsync(date);
        AvailableRoomsCollection.ItemsSource = rooms
            .Select(r => new RoomViewModel(r))
            .ToList();
    }

    private void OnPrevMonth(object sender, EventArgs e)
    {
        _currentMonth = _currentMonth.AddMonths(-1);
        _selectedDate = null;
        RefreshCalendar();
        _ = LoadAvailableRoomsAsync();
    }

    private void OnNextMonth(object sender, EventArgs e)
    {
        _currentMonth = _currentMonth.AddMonths(1);
        _selectedDate = null;
        RefreshCalendar();
        _ = LoadAvailableRoomsAsync();
    }
}
