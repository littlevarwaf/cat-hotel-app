using CatHotel.Services;
using System.ComponentModel;

namespace CatHotel.Views;

public partial class RoomDetailPage : ContentView
{
    private bool _isInitialized = false;
    private readonly CartService _cart = CartService.Instance;

    public RoomDetailPage()
    {
        InitializeComponent();
        this.Loaded += OnViewLoaded;
    }

    private void OnViewLoaded(object? sender, EventArgs e)
    {
        if (_isInitialized) return;
        _isInitialized = true;
    }

    private async void OnBackClicked(object? sender, EventArgs e)
        => await NavigationService.GoBackAsync();

    private async void OnCheckout(object? sender, EventArgs e)
    {
        await NavigationService.GoBackAsync();
    }
}