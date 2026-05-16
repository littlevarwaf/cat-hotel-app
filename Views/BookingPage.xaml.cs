using CatHotel.Models;
using CatHotel.Services;

namespace CatHotel.Views;

public partial class BookingPage : ContentPage, INavigationAware
{
    private readonly IRoomRepository _roomRepo;
    private readonly BookingDraftService _draft = BookingDraftService.Instance;
    private Room? _room;

    public BookingPage() : this(
        IPlatformApplication.Current!.Services.GetRequiredService<IRoomRepository>())
    { }

    public BookingPage(IRoomRepository roomRepo)
    {
        InitializeComponent();
        _roomRepo = roomRepo;
        BindingContext = _draft;
    }

    public async void OnNavigatedTo(IDictionary<string, object> parameters)
    {
        System.Diagnostics.Debug.WriteLine($"[BookingPage] OnNavigatedTo called with {parameters.Count} parameters");
        foreach (var param in parameters)
        {
            System.Diagnostics.Debug.WriteLine($"[BookingPage] Parameter: {param.Key} = {param.Value}");
        }

        if (!parameters.TryGetValue("roomId", out var idObj) || idObj is not int roomId)
        {
            System.Diagnostics.Debug.WriteLine("[BookingPage] ERROR: roomId parameter not found or invalid");
            return;
        }

        System.Diagnostics.Debug.WriteLine($"[BookingPage] Got roomId: {roomId}");

        DateTime? checkIn = null;
        if (parameters.TryGetValue("checkIn", out var dateObj) && dateObj is DateTime d)
            checkIn = d;

        _draft.ResetForRoom(roomId, checkIn);
        _room = await _roomRepo.GetRoomByIdAsync(roomId);

        System.Diagnostics.Debug.WriteLine($"[BookingPage] Loaded room: {_room?.Name ?? "NULL"}");

        if (_room != null)
        {
            _draft.Room = _room;
            System.Diagnostics.Debug.WriteLine($"[BookingPage] Set _draft.Room to: {_draft.Room.Name}");
        }

        FromDateField.Value = _draft.FromDate;
        ToDateField.Value = _draft.ToDate;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        FromDateField.Value = _draft.FromDate;
        ToDateField.Value = _draft.ToDate;
    }

    private async void OnPickCustomerClicked(object? sender, EventArgs e)
    {
        await NavigationService.GoToAsync(NavigationService.CustomerWrapperPage,
            new Dictionary<string, object> { ["mode"] = 1 });
    }

    private async void OnPickCatsClicked(object? sender, EventArgs e)
    {
        if (_draft.SelectedCustomer == null)
        {
            await DisplayAlertAsync("เลือกลูกค้าก่อน", "กรุณาเลือกลูกค้าก่อนเลือกแมว", "OK");
            return;
        }

        await NavigationService.GoToAsync(NavigationService.CatWrapperPage,
            new Dictionary<string, object> { ["mode"] = 1 });
    }

    private async void OnRemoveCatClicked(object? sender, EventArgs e)
    {
        if (sender is not Button button) return;

        var cat = button.BindingContext as Cat;
        if (cat == null) return;

        bool confirm = await DisplayAlertAsync("Remove Cat",
            $"Remove '{cat.Name}' from the selected cats?", "Remove", "Cancel");

        if (!confirm) return;

        _draft.SelectedCats.Remove(cat);
    }

    private async void OnConfirmBookingClicked(object? sender, EventArgs e)
    {
        if (_room == null)
        {
            await DisplayAlertAsync("ข้อผิดพลาด", "ไม่พบข้อมูลห้อง", "OK");
            return;
        }

        if (_draft.SelectedCustomer == null)
        {
            await DisplayAlertAsync("กรุณาเลือกลูกค้า", "ต้องเลือกลูกค้าก่อนจอง", "OK");
            return;
        }

        _draft.FromDate = FromDateField.Value;
        _draft.ToDate = ToDateField.Value;

        if (_draft.ToDate <= _draft.FromDate)
        {
            await DisplayAlertAsync("วันที่ไม่ถูกต้อง", "ToDate ต้องหลัง FromDate", "OK");
            return;
        }

        try
        {
            await App.Database.InitializeAsync();

            var nights = Math.Max(1, (_draft.ToDate.Date - _draft.FromDate.Date).Days);
            var total = _room.BasePrice * nights;

            var booking = new Booking
            {
                RoomId = _room.Id,
                CustomerId = _draft.SelectedCustomer.Id,
                StartDate = _draft.FromDate,
                EndDate = _draft.ToDate,
                TotalPrice = total,
                CreatedAt = DateTime.Now
            };

            await App.Database.Db.InsertAsync(booking);

            foreach (var cat in _draft.SelectedCats)
            {
                await App.Database.Db.InsertAsync(new BookingCat
                {
                    BookingId = booking.Id,
                    CatId = cat.Id
                });
            }

            // Update room status to Occupied
            _room.Status = RoomStatus.Occupied;
            await _roomRepo.UpdateRoomAsync(_room);

            System.Diagnostics.Debug.WriteLine($"[BookingPage] Room {_room.Id} status updated to Occupied");

            await DisplayAlertAsync("สำเร็จ", $"จองห้อง {_room.Name} เรียบร้อย (#{booking.Id})", "OK");
            await NavigationService.GoBackAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("ข้อผิดพลาด", ex.Message, "OK");
        }
    }

    private async void OnBackClicked(object? sender, EventArgs e)
        => await NavigationService.GoBackAsync();
}