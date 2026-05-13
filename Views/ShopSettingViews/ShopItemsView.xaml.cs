using CatHotel.Models;
using CatHotel.Services;

namespace CatHotel.Views.ShopSettingViews;

public partial class ShopItemsView : ContentView
{
    private bool _isInitialized = false;
    private readonly DatabaseService _db;
    private List<ShopItem> _allItems = new();
    private CancellationTokenSource _searchCancellationTokenSource;

    public ShopItemsView()
    {
        InitializeComponent();
        _db = App.Database;

        // Call initialization when the view is loaded
        this.Loaded += OnViewLoaded;
    }

    private async void OnViewLoaded(object sender, EventArgs e)
    {
        if (_isInitialized) return;
        _isInitialized = true;

        try
        {
            await LoadItemsAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("[SHOP ITEMS][FATAL] " + ex);
            throw;
        }
    }

    // Public method to refresh data
    public async Task RefreshAsync()
    {
        if (_isInitialized)
        {
            await LoadItemsAsync();
        }
    }

    private async Task LoadItemsAsync()
    {
        _allItems = await _db.Db.Table<ShopItem>().ToListAsync();
        ItemsCollectionView.ItemsSource = new List<ShopItem>(_allItems);
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
                ? _allItems
                : _allItems.Where(i => i.Name.ToLower().Contains(query)
                                   || i.Description.ToLower().Contains(query)).ToList();

            ItemsCollectionView.ItemsSource = filtered;
        }
        catch (OperationCanceledException)
        {
            // Search was cancelled, ignore
        }
    }

    private void OnEditTabTapped(object sender, TappedEventArgs e) { }

    private async void OnAddNewTabTapped(object sender, TappedEventArgs e)
    {
        await NavigationService.GoToAsync("ShopItemAddPage");
    }

    private async void OnItemTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is ShopItem item)
        {
            await NavigationService.GoToAsync("ShopItemEditPage",
                new Dictionary<string, object> { ["itemId"] = item.Id });
        }
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await NavigationService.GoBackAsync();
    }
}