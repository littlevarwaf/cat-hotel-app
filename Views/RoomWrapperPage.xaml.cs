using CatHotel.Models;
using CatHotel.Services;
using CatHotel.Views;
using CatHotel.ViewModels;
using System.ComponentModel;

namespace CatHotel.Views;

public partial class RoomWrapperPage : ContentPage, INavigationAware
{
    private RoomWrapperViewModel _viewModel;

    private int _popupQty = 1;
    private ShopItem? _popupItem;

    public RoomWrapperPage()
    {
        InitializeComponent();
        _viewModel = new RoomWrapperViewModel();
        BindingContext = _viewModel;

        // Subscribe to popup service events
        PopupService.Instance.ShowPopupRequested += OnShowPopup;
        PopupService.Instance.HidePopupRequested += OnHidePopup;

        // Subscribe to cart changes to detect when order is placed
        if (CartService.Instance is INotifyPropertyChanged cartNotify)
        {
            cartNotify.PropertyChanged += OnCartPropertyChanged;
        }
    }

    private void OnPageLoaded(object? sender, EventArgs e)
    {
        if (this.FindByName("RoomDetailPageInstance") is RoomDetailPage roomDetailPage)
        {
            roomDetailPage.BindingContext = _viewModel;
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        System.Diagnostics.Debug.WriteLine($"[RoomWrapperPage] OnAppearing - Refreshing cats and booking data");

        // Refresh the cats from database if we have an active booking
        if (_viewModel.Booking != null)
        {
            try
            {
                await _viewModel.RefreshBookingCatsAsync(_viewModel.Booking.Id);
                System.Diagnostics.Debug.WriteLine($"[RoomWrapperPage] ✅ Cats refreshed on page appearing");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RoomWrapperPage] Error refreshing cats: {ex}");
            }
        }
    }

    private void OnCartPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // When cart is cleared (order placed), refresh booking items
        if (e.PropertyName == nameof(CartService.Instance.Count) &&
            CartService.Instance.Count == 0)
        {
            _ = RefreshBookingItemsFromCartAsync();
        }
    }

    private async Task RefreshBookingItemsFromCartAsync()
    {
        if (_viewModel.Booking != null)
        {
            System.Diagnostics.Debug.WriteLine($"[RoomWrapperPage] 🔄 Cart cleared - Refreshing booking items for booking: {_viewModel.Booking.Id}");
            await _viewModel.RefreshBookingItemsAsync(_viewModel.Booking.Id);
            System.Diagnostics.Debug.WriteLine($"[RoomWrapperPage] ✅ Booking items refreshed after cart checkout");
        }
    }

    public async void OnNavigatedTo(IDictionary<string, object> parameters)
    {
        System.Diagnostics.Debug.WriteLine($"[RoomWrapperPage] OnNavigatedTo called with params: {string.Join(",", parameters.Keys)}");

        // Check if we just returned from CartPage with new items
        if (CartPage.ShouldRefreshBookingItems && _viewModel.Booking != null)
        {
            System.Diagnostics.Debug.WriteLine($"[RoomWrapperPage] 🔄 Refreshing booking items for booking: {_viewModel.Booking.Id}");
            CartPage.ShouldRefreshBookingItems = false; // Reset flag
            await _viewModel.RefreshBookingItemsAsync(_viewModel.Booking.Id);
            System.Diagnostics.Debug.WriteLine($"[RoomWrapperPage] ✅ Booking items refreshed");
        }

        // Pass navigation to the ViewModel
        if (_viewModel is INavigationAware navigationAware)
        {
            navigationAware.OnNavigatedTo(parameters);
        }
    }

    // Event handler - async void for events
    private async void OnShowPopup(object? sender, PopupEventArgs args)
    {
        await ShowPopupAsync(args);
    }

    // Actual async logic - Task for awaiting from other code
    private async Task ShowPopupAsync(PopupEventArgs args)
    {
        _popupItem = args.ShopItem;
        _popupQty = 1;

        PopupName.Text = args.Name;
        PopupDesc.Text = args.Description;
        PopupPrice.Text = args.Price;
        PopupQty.Text = "1";
        PopupImage.Source = args.ImageUrl;

        PopupCard.TranslationY = 600;
        DimOverlay.Opacity = 0;
        DimOverlay.IsVisible = true;
        PopupCard.IsVisible = true;

        await Task.WhenAll(
            PopupCard.TranslateToAsync(0, 0, 300, Easing.CubicOut),
            DimOverlay.FadeToAsync(0.5, 250));
    }

    // Event handler - async void for events
    private async void OnHidePopup(object? sender, EventArgs e)
    {
        await HidePopupAsync();
    }

    // Actual async logic - Task for awaiting from other code
    private async Task HidePopupAsync()
    {
        await Task.WhenAll(
            PopupCard.TranslateToAsync(0, 600, 250, Easing.CubicIn),
            DimOverlay.FadeToAsync(0, 200));
        PopupCard.IsVisible = false;
        DimOverlay.IsVisible = false;
        _popupItem = null;
    }

    // Event handler - calls HidePopupAsync
    private async void OnClosePopup(object? sender, EventArgs e)
    {
        await HidePopupAsync();
    }

    // Event handler - calls HidePopupAsync
    private async void OnDimTapped(object? sender, TappedEventArgs e)
    {
        await HidePopupAsync();
    }

    private void OnIncrement(object? sender, EventArgs e)
    {
        _popupQty++;
        PopupQty.Text = _popupQty.ToString();
    }

    private void OnDecrement(object? sender, EventArgs e)
    {
        if (_popupQty > 1) _popupQty--;
        PopupQty.Text = _popupQty.ToString();
    }

    private async void OnAddToCart(object? sender, EventArgs e)
    {
        if (_popupItem is null) return;
        CartService.Instance.Add(_popupItem, _popupQty);
        await HidePopupAsync();
    }
}