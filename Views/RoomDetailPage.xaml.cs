using CatHotel.Models;
using CatHotel.Services;

namespace CatHotel.Views;

[QueryProperty(nameof(Room), "Room")]
public partial class RoomDetailPage : ContentPage
{
    private readonly CartService _cart = CartService.Instance;
    private Room? _room;

    public Room? Room
    {
        get => _room;
        set { _room = value; UpdateUI(); }
    }

    public RoomDetailPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        UpdateCartBadge();
    }

    private void UpdateCartBadge()
    {
        var n = _cart.Count;
        CartCountLabel.Text = n > 99 ? "99" : n.ToString();
        CartBadge.IsVisible = n > 0;
    }

    private void UpdateUI()
    {
        if (_room == null) return;

        RoomIdLabel.Text = $"{_room.Name} - Booking Details";

        RoomTypeLabel.Text = _room.RoomType switch
        {
            RoomTypes.Small  => "Small room",
            RoomTypes.Medium => "Mid-sized room",
            RoomTypes.Large  => "Large room",
            _                => "Unknown"
        };
        BasePriceLabel.Text = $"Base Price: ฿{_room.BasePrice:N0} / night";
    }

    private async void OnBackClicked(object sender, EventArgs e)
        => await NavigationService.GoBackAsync();

    private async void OnCheckout(object sender, EventArgs e)
    {
        await DisplayAlertAsync("Checkout", "Processing payment... 🐾\nThank you!", "OK");
        await NavigationService.GoBackAsync();
    }

    private async void OnOpenShopTapped(object? sender, TappedEventArgs e)
        => await NavigationService.GoToAsync(NavigationService.Shop);

    private async void OnOpenCartClicked(object sender, EventArgs e)
    {
        await NavigationService.GoToAsync(NavigationService.Cart);
        UpdateCartBadge();
    }
}
