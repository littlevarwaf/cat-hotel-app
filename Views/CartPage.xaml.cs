using CatHotel.Models;
using CatHotel.Services;
using System.ComponentModel;

namespace CatHotel.Views;

public partial class CartPage : ContentPage, INavigationAware
{
    private bool _isInitialized = false;
    private readonly CartService _cart = CartService.Instance;
    private Room? _room;

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
        
        // Subscribe to cart changes to update in real-time
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
        // Extract room data if passed
        if (parameters.TryGetValue("room", out var roomObj) && roomObj is Room room)
        {
            Room = room;
            System.Diagnostics.Debug.WriteLine($"[CART] Received Room: {room.Name} (ID: {room.Id})");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("[CART] No room data received");
        }
    }

    private async void OnViewLoaded(object sender, EventArgs e)
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

    private void OnRemoveClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is CartEntry entry)
        {
            _cart.Remove(entry);
            Refresh();
        }
    }

    private async void OnPlaceOrder(object sender, EventArgs e)
    {
        if (!_cart.Items.Any())
        {
            return;
        }
        _cart.Clear();
        Refresh();
        await NavigationService.GoBackAsync();
    }

    private async void OnBackClicked(object sender, EventArgs e)
        => await NavigationService.GoBackAsync();
}
