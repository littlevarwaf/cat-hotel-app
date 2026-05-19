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
        }

        if (parameters.TryGetValue("booking", out var bookingObj) && bookingObj is Booking booking)
        {
            _booking = booking;
            System.Diagnostics.Debug.WriteLine($"[CartPage] Booking loaded: {_booking.Id}");
        }
    }

    private void OnViewLoaded(object? sender, EventArgs e)
    {
        if (_isInitialized) return;
        _isInitialized = true;
        Refresh();
    }

    private void Refresh()
    {
        CartCollection.ItemsSource = null;
        CartCollection.ItemsSource = _cart.Items;
        OrderSummaryCollection.ItemsSource = null;
        OrderSummaryCollection.ItemsSource = _cart.Items;
        TotalItemsLabel.Text = $"Total Items: {_cart.Count}";
        TotalLabel.Text = $"฿{_cart.Total:N0}";
    }

    private void OnRemoveClicked(object? sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is CartEntry entry)
        {
            _cart.Remove(entry);
            Refresh();
        }
    }

    private async void OnCartItemIncrement(object? sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is CartEntry entry)
        {
            entry.Quantity++;
            _cart.OnPropertyChanged(nameof(_cart.Count));
            _cart.OnPropertyChanged(nameof(_cart.Total));
            Refresh();
        }
    }

    private async void OnCartItemDecrement(object? sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is CartEntry entry)
        {
            if (entry.Quantity > 1)
            {
                entry.Quantity--;
                _cart.OnPropertyChanged(nameof(_cart.Count));
                _cart.OnPropertyChanged(nameof(_cart.Total));
            }
            else
            {
                // Quantity is at 1, ask if user wants to remove
                bool result = await DisplayAlertAsync(
                    "Remove Item",
                    $"Remove {entry.Item.Name} from cart?",
                    "Yes",
                    "No");

                if (result)
                {
                    _cart.Remove(entry);
                    Refresh();
                }
            }
        }
    }

    private async void OnBackClicked(object? sender, EventArgs e)
        => await NavigationService.GoBackAsync();

    private async void OnPlaceOrder(object? sender, EventArgs e)
    {
        if (!_cart.Items.Any())
            return;

        if (_booking == null)
        {
            await DisplayAlertAsync("Error", "No active booking found", "OK");
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

            await DisplayAlertAsync("สำเร็จ ✅", $"สั่งซื้อเรียบร้อย (Booking #{_booking.Id})", "OK");

            // 🔔 Signal that booking items need to be refreshed
            ShouldRefreshBookingItems = true;
            await NavigationService.GoBackAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("[CART] OnPlaceOrder error: " + ex);
            await DisplayAlertAsync("Error", "บันทึกคำสั่งซื้อไม่สำเร็จ: " + ex.Message, "OK");
        }
    }
}