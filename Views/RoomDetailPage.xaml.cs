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

            // Subscribe to discount changes
            if (this.BindingContext is RoomWrapperViewModel viewModel && viewModel is INotifyPropertyChanged notifyPropertyChanged)
            {
                notifyPropertyChanged.PropertyChanged += ViewModel_PropertyChanged;
            }
        }
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(RoomWrapperViewModel.HasAppliedDiscount))
        {
            if (this.BindingContext is RoomWrapperViewModel viewModel)
            {
                // Toggle visibility of discount entry and discount card
                DiscountEntrySection.IsVisible = !viewModel.HasAppliedDiscount;
                AppliedDiscountCard.IsVisible = viewModel.HasAppliedDiscount;

                // Toggle visibility of subtotal and discount rows
                SubtotalRow.IsVisible = viewModel.HasAppliedDiscount;
                DiscountRow.IsVisible = viewModel.HasAppliedDiscount;

                System.Diagnostics.Debug.WriteLine($"[RoomDetailPage] Discount visibility updated: HasAppliedDiscount={viewModel.HasAppliedDiscount}");
            }
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