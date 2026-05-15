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
        BindingContext = this;
        _roomRepo = roomRepo;
        _draft.PropertyChanged += (_, _) => RefreshSummary();
    }

    public async void OnNavigatedTo(IDictionary<string, object> parameters)
    {
        if (!parameters.TryGetValue("roomId", out var idObj) || idObj is not int roomId)
            return;

        DateTime? checkIn = null;
        if (parameters.TryGetValue("checkIn", out var dateObj) && dateObj is DateTime d)
            checkIn = d;

        _draft.ResetForRoom(roomId, checkIn);
        _room = await _roomRepo.GetRoomByIdAsync(roomId);

        if (_room != null)
        {
            RoomTitleLabel.Text = $"จอง {_room.Name}";
            RoomTypeLabel.Text = _room.RoomType.ToString();
        }

        FromDateField.Value = _draft.FromDate;
        ToDateField.Value = _draft.ToDate;
        RefreshSummary();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        FromDateField.Value = _draft.FromDate;
        ToDateField.Value = _draft.ToDate;
        RefreshSummary();
    }

    private void RefreshSummary()
    {
        CustomerSummaryLabel.Text = _draft.SelectedCustomerDisplay;
        CatsSummaryLabel.Text = _draft.SelectedCatsDisplay;
    }

    private async void OnPickCustomerClicked(object? sender, EventArgs e)
    {
        await NavigationService.GoToAsync(NavigationService.CustomerPickerWrapperPage);
    }

    private async void OnPickCatsClicked(object? sender, EventArgs e)
    {
        if (_draft.SelectedCustomer == null)
        {
            await DisplayAlert("เลือกลูกค้าก่อน", "กรุณาเลือกลูกค้าก่อนเลือกแมว", "OK");
            return;
        }

        await NavigationService.GoToAsync(NavigationService.CatPickerWrapperPage);
    }

    private async void OnConfirmBookingClicked(object? sender, EventArgs e)
    {
        if (_room == null)
        {
            await DisplayAlert("ข้อผิดพลาด", "ไม่พบข้อมูลห้อง", "OK");
            return;
        }

        if (_draft.SelectedCustomer == null)
        {
            await DisplayAlert("กรุณาเลือกลูกค้า", "ต้องเลือกลูกค้าก่อนจอง", "OK");
            return;
        }

        _draft.FromDate = FromDateField.Value;
        _draft.ToDate = ToDateField.Value;

        if (_draft.ToDate <= _draft.FromDate)
        {
            await DisplayAlert("วันที่ไม่ถูกต้อง", "ToDate ต้องหลัง FromDate", "OK");
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

            await DisplayAlert("สำเร็จ", $"จองห้อง {_room.Name} เรียบร้อย (#{booking.Id})", "OK");
            await NavigationService.GoBackAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("ข้อผิดพลาด", ex.Message, "OK");
        }
    }

    private async void OnBackClicked(object? sender, EventArgs e)
        => await NavigationService.GoBackAsync();
}
