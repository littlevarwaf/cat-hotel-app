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

    private static string GetEmoji(ShopItem item) => item.ItemType switch
    {
        ItemType.Food       => "🐟",
        ItemType.Necessity  => "🪨",
        ItemType.Toy        => "🧶",
        ItemType.Accessory  => "🧴",
        _                   => "🐾"
    };

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
            var items = await App.Database.Db.Table<ShopItem>().ToListAsync();

            // ถ้า DB ว่างให้ seed ข้อมูลตั้งต้นไว้ก่อน
            if (items.Count == 0)
            {
                items = GetDefaultItems();
                foreach (var item in items)
                    await App.Database.Db.InsertAsync(item);

                // reload เพื่อให้ได้ Id จริงจาก DB
                items = await App.Database.Db.Table<ShopItem>().ToListAsync();
            }

            ShopCollection.ItemsSource = items;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("[SHOP] LoadShopItemsAsync error: " + ex);
            // fallback ใช้ default list ถ้า DB ยังไม่พร้อม
            ShopCollection.ItemsSource = GetDefaultItems();
        }
    }

    private static List<ShopItem> GetDefaultItems() => new()
    {
        new ShopItem("Me-O Tuna",      "Wet food for cats",   35,  ItemType.Food,       ""),
        new ShopItem("Royal Canin",    "Dry food premium",   399,  ItemType.Food,       ""),
        new ShopItem("Whiskas Sachet", "Pouch meal",          25,  ItemType.Food,       ""),
        new ShopItem("Cat Treat",      "Snack & reward",      89,  ItemType.Food,       ""),
        new ShopItem("Kitty Litter",   "Clumping sand",      199,  ItemType.Necessity,  ""),
        new ShopItem("Cat Shampoo",    "Gentle formula",     149,  ItemType.Accessory,  ""),
    };

    private async void OnItemTapped(object? sender, TappedEventArgs e)
    {
        if (_isAnimating || e.Parameter is not ShopItem item) return;
        _selectedItem = item;
        _qty = 1;

        PopupName.Text  = item.Name;
        PopupDesc.Text  = item.Description;
        PopupPrice.Text = $"฿{item.ItemPrice:N0}";
        PopupQty.Text   = "1";
        PopupEmoji.Text = GetEmoji(item);

        PopupCard.TranslationY = 600;
        DimOverlay.Opacity     = 0;
        DimOverlay.IsVisible   = true;
        PopupCard.IsVisible    = true;

        _isAnimating = true;
        await Task.WhenAll(
            PopupCard.TranslateToAsync(0, 0, 300, Easing.CubicOut),
            DimOverlay.FadeToAsync(1, 250));
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
