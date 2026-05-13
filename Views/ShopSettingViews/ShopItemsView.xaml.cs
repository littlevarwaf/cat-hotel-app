using CatHotel.Models;
using CatHotel.Services;

namespace CatHotel.Views.ShopSettingViews;

public partial class ShopItemsView : ContentView
{
    private bool _isInitialized = false;
    private readonly DatabaseService _db;
    private List<ShopItem> _allItems = new();

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

    private async Task LoadItemsAsync()
    {
        _allItems = await _db.Db.Table<ShopItem>().ToListAsync();
        ItemsCollectionView.ItemsSource = _allItems;
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        var query = e.NewTextValue?.ToLower() ?? string.Empty;
        var filtered = string.IsNullOrWhiteSpace(query)
            ? _allItems
            : _allItems.Where(i => i.Name.ToLower().Contains(query)
                               || i.Description.ToLower().Contains(query)).ToList();
        ItemsCollectionView.ItemsSource = filtered;
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