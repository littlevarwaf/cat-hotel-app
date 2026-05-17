using CatHotel.Models;
using CatHotel.Services;
using CatHotel.Views;
using CatHotel.ViewModels;

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
    }

    private void OnPageLoaded(object? sender, EventArgs e)
    {
        if (this.FindByName("RoomDetailPageInstance") is RoomDetailPage roomDetailPage)
        {
            roomDetailPage.BindingContext = _viewModel;
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