using CatHotel.Models;
using CatHotel.Services;
using System.ComponentModel;

namespace CatHotel.Views;

public partial class CartPage : ContentPage, INavigationAware
{
    private bool _isInitialized = false;
    private readonly CartService _cart = CartService.Instance;
    private Room? _room;
    private Booking? _booking;

    // Static flag to signal refresh needed
    public static bool ShouldRefreshBookingItems { get; set; } = false;

    public Room? Room
    {
        get => _room;
        set
        {
            if (_room != value)
            {
                _room = value;
                OnPropertyChanged();
            }
        }
    }

    public CartPage()
    {
        InitializeComponent();
        BindingContext = this;
        this.Loaded += OnViewLoaded;

        if (_cart is INotifyPropertyChanged notifyPropertyChanged)
        {
            notifyPropertyChanged.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(_cart.Count) || e.PropertyName == nameof(_cart.Total))
                {
                    Refresh();
                }
            };
        }
    }

    public void OnNavigatedTo(IDictionary<string, object> parameters)
    {
        if (parameters.TryGetValue("room", out var roomObj) && roomObj is Room room)
        {
            Room = room;
            System.Diagnostics.Debug.WriteLine($"[CART] Received Room: {room.Name} (ID: {room.Id})");
        }

        if (parameters.TryGetValue("booking", out var bookingObj) && bookingObj is Booking booking)
        {
            _booking = booking;
            System.Diagnostics.Debug.WriteLine($"[CART] Received Booking: {booking.Id}");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("[CART] No booking data received");
        }
    }

    private async void OnViewLoaded(object? sender, EventArgs e)
    {
        if (_isInitialized) return;
        _isInitialized = true;

        Refresh();
    }

    private void Refresh()
    {
        CartCollection.ItemsSource = null;
        CartCollection.ItemsSource = _cart.Items;
        TotalItemsLabel.Text = $"Total Items: {_cart.Count}";
        TotalLabel.Text = $"฿{_cart.Total:N0}";
        SummaryLabel.Text = string.Join(", ",
            _cart.Items.Select(c => $"{c.Item.Name} (x{c.Quantity})"));
    }

    private void OnRemoveClicked(object? sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is CartEntry entry)
        {
            _cart.Remove(entry);
            Refresh();
        }
    }

    private async void OnPlaceOrder(object? sender, EventArgs e)
    {
        if (!_cart.Items.Any())
            return;

        if (_booking == null)
        {
            await DisplayAlert("Error", "No active booking found", "OK");
            return;
        }

        try
        {
            await App.Database.InitializeAsync();

            // ✨ Add items to EXISTING booking instead of creating new one
            foreach (var entry in _cart.Items)
            {
                var bi = new BookingItem(_booking.Id, entry.Item.Id, entry.Quantity)
                {
                    UnitPrice = entry.Item.ItemPrice
                };
                await App.Database.Db.InsertAsync(bi);
                System.Diagnostics.Debug.WriteLine($"[CART] BookingItem created: BookingId={_booking.Id}, ItemId={entry.Item.Id}, Qty={entry.Quantity}");
            }

            // Recalculate total price with new items
            await App.Database.RecalculateBookingTotalPriceAsync(_booking.Id);
            System.Diagnostics.Debug.WriteLine($"[CART] Booking {_booking.Id} total price recalculated");

            _cart.Clear();
            Refresh();

            await DisplayAlert("สำเร็จ ✅", $"สั่งซื้อเรียบร้อย (Booking #{_booking.Id})", "OK");

            // 🔔 Signal that booking items need to be refreshed
            ShouldRefreshBookingItems = true;
            await NavigationService.GoBackAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("[CART] OnPlaceOrder error: " + ex);
            await DisplayAlert("Error", "บันทึกคำสั่งซื้อไม่สำเร็จ: " + ex.Message, "OK");
        }
    }

    private async void OnBackClicked(object? sender, EventArgs e)
        => await NavigationService.GoBackAsync();
}