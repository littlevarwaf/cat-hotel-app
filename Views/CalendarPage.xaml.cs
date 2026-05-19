using CatHotel.Models;
using CatHotel.Services;

namespace CatHotel.Views;

public enum BookingDisplayState
{
    Available,
    PartiallyBooked,
    FullyBooked
}

public class CalendarDay
{
    public string DayText { get; set; } = "";
    public bool IsCurrentMonth { get; set; }
    public BookingDisplayState DisplayState { get; set; } = BookingDisplayState.Available;
    public bool IsSelected { get; set; }
    public bool IsToday { get; set; }
    public DateTime Date { get; set; }

    public Color BgColor
    {
        get
        {
            if (!IsCurrentMonth) return Colors.Transparent;
            if (IsSelected) return Color.FromArgb("#F5A1A1");

            return DisplayState switch
            {
                BookingDisplayState.FullyBooked => Color.FromArgb("#F42E55"),
                BookingDisplayState.PartiallyBooked => Color.FromArgb("#FFCC90"),
                _ => IsToday ? Color.FromArgb("#FCE4EC") : Colors.Transparent
            };
        }
    }

    public Color TextColor
    {
        get
        {
            if (!IsCurrentMonth) return Color.FromArgb("#BDBDBD");
            if (IsSelected) return Colors.White;
            if (DisplayState == BookingDisplayState.FullyBooked) return Colors.White;
            return Color.FromArgb("#212121");
        }
    }
}

public partial class CalendarPage : ContentView
{
    private bool _isInitialized = false;
    private readonly IRoomRepository _roomRepo;
    private readonly IBookingRepository _bookingRepo;
    private DateTime _currentMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
    private DateTime? _selectedDate;
    private List<CalendarDay> _calendarDays = new();
    private List<Room> _allRooms = new();
    private List<Booking> _monthBookings = new();

    public CalendarPage() : this(
        IPlatformApplication.Current!.Services.GetRequiredService<IRoomRepository>(),
        IPlatformApplication.Current!.Services.GetRequiredService<IBookingRepository>())
    { }

    public CalendarPage(IRoomRepository roomRepo, IBookingRepository bookingRepo)
    {
        InitializeComponent();
        _roomRepo = roomRepo;
        _bookingRepo = bookingRepo;
        BuildCalendarGrid();

        RoomService.RoomAdded += OnRoomsChanged;
        RoomService.RoomUpdated += OnRoomsChanged;
        RoomService.RoomDeleted += OnRoomsChanged;

        Loaded += OnViewLoaded;
    }

    private void OnRoomsChanged(object? sender, RoomEventArgs e) =>
        _ = MainThread.InvokeOnMainThreadAsync(RefreshCalendarAndRoomsAsync);

    private async void OnViewLoaded(object? sender, EventArgs e)
    {
        if (_isInitialized) return;
        _isInitialized = true;

        try
        {
            await RefreshCalendarAndRoomsAsync();
            // Select today's date on page load
            _selectedDate = DateTime.Today;
            await UpdateDayCells();
            await LoadAvailableRoomsAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("[CALENDAR][FATAL] " + ex);
            throw;
        }
    }

    public async Task RefreshCalendarAndRoomsAsync()
    {
        await RefreshCalendar();
        await LoadAvailableRoomsAsync();
    }

    private async Task RefreshCalendar()
    {
        MonthYearLabel.Text = _currentMonth.ToString("MMMM yyyy");

        _allRooms = await _roomRepo.GetAllRoomsAsync();
        var monthStart = _currentMonth;
        var monthEnd = _currentMonth.AddMonths(1);
        _monthBookings = await _bookingRepo.GetBookingsForDateRangeWithRoomsAsync(monthStart, monthEnd);

        // Update room counts
        int totalRooms = _allRooms.Count;
        int availableRooms = _allRooms.Count(r => r.Status != RoomStatus.Unavailable);
        
        TotalRoomsLabel.Text = $"Total Rooms: {totalRooms}";
        AvailableRoomsCountLabel.Text = $"Available Rooms: {availableRooms}";

        await UpdateDayCells();
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
                    Padding = 2,
                    Margin = new Thickness(1),
                    BackgroundColor = Colors.Transparent,
                    StrokeThickness = 0,
                    HeightRequest = 36,
                    WidthRequest = 36,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                    StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(18) }
                };
                var label = new Label
                {
                    FontSize = 13,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
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
    }

    private async Task UpdateDayCells()
    {
        _calendarDays = await BuildDayListAsync();
        var children = CalendarDaysGrid.Children.ToList();
        for (int i = 0; i < children.Count && i < _calendarDays.Count; i++)
        {
            if (children[i] is Border frame && frame.Content is Label lbl)
            {
                var day = _calendarDays[i];
                lbl.Text = day.DayText;
                lbl.TextColor = day.TextColor;
                frame.BackgroundColor = day.BgColor;
            }
        }
    }

    private Task<List<CalendarDay>> BuildDayListAsync()
    {
        var days = new List<CalendarDay>();
        var firstDay = new DateTime(_currentMonth.Year, _currentMonth.Month, 1);
        int dow = (int)firstDay.DayOfWeek;
        int offset = dow == 0 ? 6 : dow - 1;

        var prevMonth = _currentMonth.AddMonths(-1);
        int prevTotal = DateTime.DaysInMonth(prevMonth.Year, prevMonth.Month);
        for (int i = offset - 1; i >= 0; i--)
            days.Add(new CalendarDay { DayText = (prevTotal - i).ToString(), IsCurrentMonth = false });

        int daysInMonth = DateTime.DaysInMonth(_currentMonth.Year, _currentMonth.Month);
        var today = DateTime.Today;
        int totalAvailableRooms = _allRooms.Count(r => r.Status != RoomStatus.Unavailable);

        for (int d = 1; d <= daysInMonth; d++)
        {
            var date = new DateTime(_currentMonth.Year, _currentMonth.Month, d);

            var bookingsForDate = _monthBookings
                .Where(b => BookingDateHelper.IsBookingActiveOnDate(b.StartDate, b.EndDate, date))
                .ToList();

            int bookedRoomsCount = bookingsForDate
                .Select(b => b.RoomId)
                .Distinct()
                .Count();

            var displayState = BookingDisplayState.Available;
            if (bookedRoomsCount > 0)
            {
                displayState = bookedRoomsCount >= totalAvailableRooms
                    ? BookingDisplayState.FullyBooked
                    : BookingDisplayState.PartiallyBooked;
            }

            days.Add(new CalendarDay
            {
                DayText = d.ToString(),
                IsCurrentMonth = true,
                Date = date,
                DisplayState = displayState,
                IsToday = date == today,
                IsSelected = _selectedDate.HasValue && date == _selectedDate.Value,
            });
        }

        int rem = 42 - days.Count;
        for (int i = 1; i <= rem; i++)
            days.Add(new CalendarDay { DayText = i.ToString(), IsCurrentMonth = false });

        return Task.FromResult(days);
    }

    private async void OnDayCellTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is not int index) return;
        if (index < 0 || index >= _calendarDays.Count) return;
        var day = _calendarDays[index];

        // Only allow clicking on dates in the current month
        if (!day.IsCurrentMonth) return;

        _selectedDate = day.Date;
        await UpdateDayCells();
        await LoadAvailableRoomsAsync();
    }

    private async Task LoadAvailableRoomsAsync()
    {
        if (_selectedDate.HasValue)
        {
            var date = _selectedDate.Value;
            string formattedDate = date.ToString("dd MMM yyyy");

            // Update all section date labels
            AvailableDateLabel.Text = formattedDate;
            OccupiedDateLabel.Text = formattedDate;
            UnavailableDateLabel.Text = formattedDate;

            // Load available rooms - exclude rooms with Unavailable status
            var availableRooms = await _roomRepo.GetAvailableRoomsForDateAsync(date);
            var filteredAvailableRooms = availableRooms
                .Where(r => r.Status != RoomStatus.Unavailable)
                .ToList();

            AvailableRoomsCollection.ItemsSource = filteredAvailableRooms
                .Select(r => new RoomViewModel(r))
                .ToList();

            // Update available rooms count for the selected date
            AvailableRoomsCountLabel.Text = $"Available Rooms: {filteredAvailableRooms.Count}";

            // Load occupied bookings (currently active)
            var occupiedBookings = GetBookingsForDate(date)
                .Where(b => BookingDateHelper.IsBookingCurrentlyActive(b.StartDate, b.EndDate))
                .ToList();

            OccupiedRoomsCollection.ItemsSource = occupiedBookings
                .Select(b => new RoomViewModel(b.Room))
                .DistinctBy(r => r.Id)
                .ToList();

            OccupiedRoomsSection.IsVisible = occupiedBookings.Count > 0;

            // Load unavailable rooms - ALWAYS show rooms with Unavailable status + future bookings
            var unavailableRoomsByStatus = _allRooms
                .Where(r => r.Status == RoomStatus.Unavailable)
                .ToList();

            var unavailableBookings = GetBookingsForDate(date)
                .Where(b => BookingDateHelper.IsBookingFuture(b.StartDate))
                .ToList();

            // Combine both sources and remove duplicates
            var allUnavailableRooms = new List<Room>();

            // Add rooms with Unavailable status
            allUnavailableRooms.AddRange(unavailableRoomsByStatus);

            // Add rooms from future bookings (not already in unavailable list)
            foreach (var booking in unavailableBookings)
            {
                if (!allUnavailableRooms.Any(r => r.Id == booking.Room.Id))
                {
                    allUnavailableRooms.Add(booking.Room);
                }
            }

            UnavailableRoomsCollection.ItemsSource = allUnavailableRooms
                .Select(r => new RoomViewModel(r))
                .ToList();

            UnavailableRoomsSection.IsVisible = allUnavailableRooms.Count > 0;
        }
        else
        {
            AvailableRoomsCollection.ItemsSource = null;
            OccupiedRoomsCollection.ItemsSource = null;
            UnavailableRoomsCollection.ItemsSource = null;
            OccupiedRoomsSection.IsVisible = false;
            UnavailableRoomsSection.IsVisible = false;
            AvailableRoomsCountLabel.Text = "Available Rooms: -";
        }
    }

    private async void OnPrevMonth(object? sender, EventArgs e)
    {
        _currentMonth = _currentMonth.AddMonths(-1);
        _selectedDate = null;
        await RefreshCalendarAndRoomsAsync();
    }

    private async void OnNextMonth(object? sender, EventArgs e)
    {
        _currentMonth = _currentMonth.AddMonths(1);
        _selectedDate = null;
        await RefreshCalendarAndRoomsAsync();
    }

    private async void OnRoomTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is not RoomViewModel vm)
            return;

        var checkIn = _selectedDate ?? DateTime.Today;
        await NavigationService.GoToAsync(
            NavigationService.BookingPage,
            new Dictionary<string, object>
            {
                ["roomId"] = vm.Room.Id,
                ["checkIn"] = checkIn
            });
    }

    public List<Booking> GetBookingsForDate(DateTime date)
    {
        return _monthBookings
            .Where(b => BookingDateHelper.IsBookingActiveOnDate(b.StartDate, b.EndDate, date))
            .ToList();
    }
}