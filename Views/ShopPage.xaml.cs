using CatHotel.Models;
using CatHotel.Services;

namespace CatHotel.Views;

public partial class ShopPage : ContentPage
{
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

    public ShopPage() => InitializeComponent();

    protected override void OnAppearing()
    {
        base.OnAppearing();
        ShopCollection.ItemsSource = GetMockItems();
        UpdateCartBadge();
    }

    private static List<ShopItem> GetMockItems() => new()
    {
        new ShopItem("Me-O Tuna",      "Wet food for cats",   35,  ItemType.Food,       ""),
        new ShopItem("Royal Canin",    "Dry food premium",   399,  ItemType.Food,       ""),
        new ShopItem("Whiskas Sachet", "Pouch meal",          25,  ItemType.Food,       ""),
        new ShopItem("Cat Treat",      "Snack & reward",      89,  ItemType.Food,       ""),
        new ShopItem("Kitty Litter",   "Clumping sand",      199,  ItemType.Necessity,  ""),
        new ShopItem("Cat Shampoo",    "Gentle formula",     149,  ItemType.Accessory,  ""),
    };

    private void UpdateCartBadge()
    {
        var count = _cart.Count;
        CartCountLabel.Text = count.ToString();
        CartBadge.IsVisible = count > 0;
    }

    private async void OnCartClicked(object sender, EventArgs e)
        => await NavigationService.GoToAsync(NavigationService.Cart);

    private async void OnItemTapped(object sender, TappedEventArgs e)
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

    private async void OnClosePopup(object sender, EventArgs e) => await ClosePopup();
    private async void OnDimTapped(object sender, TappedEventArgs e) => await ClosePopup();

    private void OnIncrement(object sender, EventArgs e) { _qty++; PopupQty.Text = _qty.ToString(); }
    private void OnDecrement(object sender, EventArgs e) { if (_qty > 1) _qty--; PopupQty.Text = _qty.ToString(); }

    private async void OnAddToCart(object sender, EventArgs e)
    {
        if (_selectedItem == null) return;
        var name = _selectedItem.Name;
        _cart.Add(_selectedItem, _qty);
        UpdateCartBadge();
        await ClosePopup();
        await DisplayAlertAsync("Added! 🛒", $"{_qty}× {name} added to cart.", "OK");
    }
}
