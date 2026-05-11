using CatHotel.Services;

namespace CatHotel.Views;

public partial class CartPage : ContentPage
{
    private readonly CartService _cart = CartService.Instance;

    public CartPage() => InitializeComponent();

    protected override void OnAppearing()
    {
        base.OnAppearing();
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
            await DisplayAlertAsync("Cart Empty", "Please add items first.", "OK");
            return;
        }
        await DisplayAlertAsync("Order Placed! 🐾", $"Total: ฿{_cart.Total:N0}", "OK");
        _cart.Clear();
        Refresh();
        await NavigationService.GoBackAsync();
    }

    private async void OnBackClicked(object sender, EventArgs e)
        => await NavigationService.GoBackAsync();
}
