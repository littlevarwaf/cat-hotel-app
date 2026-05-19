using CatHotel.Models;
using CatHotel.Services;

namespace CatHotel.Views;

public partial class ShopPage : ContentView
{
    private bool _isInitialized = false;
    private readonly CartService _cart = CartService.Instance;
    private ShopItem? _selectedItem;
    private int _qty = 1;
    private bool _isAnimating;

    public ShopPage()
    { 
        InitializeComponent();

        // Call initialization when the view is loaded
        this.Loaded += OnViewLoaded;
    }

    private async void OnViewLoaded(object? sender, EventArgs e)
    {
        if (_isInitialized) return;
        _isInitialized = true;

        await LoadShopItemsAsync();
    }

    private async Task LoadShopItemsAsync()
    {
        try
        {
            await App.Database.InitializeAsync();
            var items = await App.Database.Db.Table<ShopItem>()
                .Where(item => item.ItemStatus == ItemStatus.Available)
                .ToListAsync();

            ShopCollection.ItemsSource = items;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("[SHOP] LoadShopItemsAsync error: " + ex);
        }
    }

    private async void OnItemTapped(object? sender, TappedEventArgs e)
    {
        if (_isAnimating || e.Parameter is not ShopItem item) return;
        _isAnimating = true;

        var args = new PopupEventArgs
        {
            ShopItem = item,
            Name = item.Name,
            Description = item.Description,
            Price = $"฿{item.ItemPrice:N0}",
            ImageUrl = !string.IsNullOrEmpty(item.ImgUrl) ? item.ImgUrl : "app_icon.svg"
        };

        PopupService.Instance.ShowPopup(args);
        _isAnimating = false;
    }

    private async Task ClosePopup()
    {
        if (_isAnimating) return;
        _isAnimating = true;
        await Task.WhenAll(
            PopupCard.TranslateToAsync(0, 700, 250, Easing.CubicIn),
            DimOverlay.FadeToAsync(0, 200));
        PopupCard.IsVisible  = false;
        DimOverlay.IsVisible = false;
        _selectedItem = null;
        _isAnimating  = false;
    }

    private async void OnClosePopup(object? sender, EventArgs e) => await ClosePopup();
    private async void OnDimTapped(object? sender, TappedEventArgs e) => await ClosePopup();

    private void OnIncrement(object? sender, EventArgs e) { _qty++; PopupQty.Text = _qty.ToString(); }
    private void OnDecrement(object? sender, EventArgs e) { if (_qty > 1) _qty--; PopupQty.Text = _qty.ToString(); }

    private async void OnAddToCart(object? sender, EventArgs e)
    {
        if (_selectedItem == null) return;
        var name = _selectedItem.Name;
        _cart.Add(_selectedItem, _qty);
        await ClosePopup();
        //await DisplayAlertAsync("Added! 🛒", $"{_qty}× {name} added to cart.", "OK");
    }
}
