using CatHotel.Services;
using CatHotel.ViewModels;
using System.ComponentModel;

namespace CatHotel.Views;

public partial class RoomDetailPage : ContentView
{
    private bool _isInitialized = false;

    public RoomDetailPage()
    {
        InitializeComponent();
        this.Loaded += OnViewLoaded;

        // Subscribe to cart service to detect when items are added
        if (CartService.Instance is INotifyPropertyChanged cartNotify)
        {
            cartNotify.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(CartService.Instance.Count) &&
                    CartService.Instance.Count == 0) // Cart was just cleared (order placed)
                {
                    RefreshBookingDataAsync();
                }
            };
        }
    }

    private void OnViewLoaded(object? sender, EventArgs e)
    {
        if (_isInitialized) return;
        _isInitialized = true;

        System.Diagnostics.Debug.WriteLine($"[RoomDetailPage] Loaded. BindingContext type: {this.BindingContext?.GetType().Name ?? "NULL"}");
        if (this.BindingContext != null)
        {
            System.Diagnostics.Debug.WriteLine($"[RoomDetailPage] Booking: {((dynamic)this.BindingContext).Booking}");
            System.Diagnostics.Debug.WriteLine($"[RoomDetailPage] Room: {((dynamic)this.BindingContext).Room}");
            System.Diagnostics.Debug.WriteLine($"[RoomDetailPage] DateRangeDisplay: {((dynamic)this.BindingContext).DateRangeDisplay}");
        }
    }

    private async Task RefreshBookingDataAsync()
    {
        try
        {
            if (this.BindingContext is RoomWrapperViewModel viewModel && viewModel.Booking != null)
            {
                System.Diagnostics.Debug.WriteLine($"[RoomDetailPage] Refreshing booking data for booking: {viewModel.Booking.Id}");
                await viewModel.RefreshBookingItemsAsync(viewModel.Booking.Id);
                System.Diagnostics.Debug.WriteLine($"[RoomDetailPage] Booking data refreshed");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[RoomDetailPage] Error refreshing booking data: {ex}");
        }
    }

    private async void OnBackClicked(object? sender, EventArgs e)
        => await NavigationService.GoBackAsync();
}