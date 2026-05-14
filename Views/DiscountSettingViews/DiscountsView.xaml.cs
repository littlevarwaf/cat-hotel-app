using CatHotel.Models;
using CatHotel.Services;

namespace CatHotel.Views.DiscountSettingViews;

public partial class DiscountsView : ContentView
{
    private bool _isInitialized = false;
    private readonly DatabaseService _db;
    private List<Discount> _allDiscounts = new();
    private CancellationTokenSource _searchCancellationTokenSource;

    public DiscountsView()
    {
        InitializeComponent();
        _db = App.Database;

        // Subscribe to discount events
        DiscountService.DiscountAdded += async (s, e) => await RefreshAsync();
        DiscountService.DiscountUpdated += async (s, e) => await RefreshAsync();
        DiscountService.DiscountDeleted += async (s, e) => await RefreshAsync();

        // Call initialization when the view is loaded
        this.Loaded += OnViewLoaded;
    }

    private async void OnViewLoaded(object sender, EventArgs e)
    {
        if (_isInitialized) return;
        _isInitialized = true;

        try
        {
            await LoadDiscountsAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("[DISCOUNTS][FATAL] " + ex);
            throw;
        }
    }

    // Public method to refresh data
    public async Task RefreshAsync()
    {
        if (_isInitialized)
        {
            await LoadDiscountsAsync();
        }
    }

    private async Task LoadDiscountsAsync()
    {
        _allDiscounts = await _db.Db.Table<Discount>().ToListAsync();
        DiscountsCollectionView.ItemsSource = new List<Discount>(_allDiscounts);
        SearchEntry.Text = string.Empty;
    }

    private async void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        // Cancel previous search task
        _searchCancellationTokenSource?.Cancel();
        _searchCancellationTokenSource = new CancellationTokenSource();

        try
        {
            // Wait 300ms before searching (debounce)
            await Task.Delay(300, _searchCancellationTokenSource.Token);

            var query = e.NewTextValue?.ToLower() ?? string.Empty;
            var filtered = string.IsNullOrWhiteSpace(query)
                ? _allDiscounts
                : _allDiscounts.Where(d =>
                    d.Code.ToLower().Contains(query) ||
                    d.Description.ToLower().Contains(query)).ToList();

            DiscountsCollectionView.ItemsSource = filtered;
        }
        catch (OperationCanceledException)
        {
            // Search was cancelled, ignore
        }
    }

    private async void OnDiscountTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is Discount discount)
        {
            await NavigationService.GoToAsync("DiscountEditPage",
                new Dictionary<string, object> { ["discountId"] = discount.Id });
        }
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await NavigationService.GoBackAsync();
    }
}