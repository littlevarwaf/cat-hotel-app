using CatHotel.Services;

namespace CatHotel.Views;

public partial class RoomDetailPage : ContentView
{
    private bool _isInitialized = false;

    public RoomDetailPage()
    {
        InitializeComponent();
        this.Loaded += OnViewLoaded;
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

    private async void OnBackClicked(object? sender, EventArgs e)
        => await NavigationService.GoBackAsync();
}