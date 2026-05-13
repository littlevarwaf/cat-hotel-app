using CatHotel.Models;
using CatHotel.Services;

namespace CatHotel.Views.RoomSettingViews;

public partial class RoomsView : ContentView
{
    private bool _isInitialized = false;
    private readonly DatabaseService _db;
    private List<Room> _allRooms = new();
    private CancellationTokenSource _searchCancellationTokenSource;

    public RoomsView()
    {
        InitializeComponent();
        _db = App.Database;

        // Subscribe to room events
        RoomService.RoomAdded += async (s, e) => await RefreshAsync();
        RoomService.RoomUpdated += async (s, e) => await RefreshAsync();
        RoomService.RoomDeleted += async (s, e) => await RefreshAsync();

        // Call initialization when the view is loaded
        this.Loaded += OnViewLoaded;
    }

    private async void OnViewLoaded(object sender, EventArgs e)
    {
        if (_isInitialized) return;
        _isInitialized = true;

        try
        {
            await LoadRoomsAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("[ROOMS][FATAL] " + ex);
            throw;
        }
    }

    // Public method to refresh data
    public async Task RefreshAsync()
    {
        if (_isInitialized)
        {
            await LoadRoomsAsync();
        }
    }

    private async Task LoadRoomsAsync()
    {
        _allRooms = await _db.Db.Table<Room>().ToListAsync();
        RoomsCollectionView.ItemsSource = new List<Room>(_allRooms);
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
                ? _allRooms
                : _allRooms.Where(r => r.Name.ToLower().Contains(query)).ToList();

            RoomsCollectionView.ItemsSource = filtered;
        }
        catch (OperationCanceledException)
        {
            // Search was cancelled, ignore
        }
    }

    private async void OnEditTabTapped(object sender, TappedEventArgs e) { }

    private async void OnAddNewTabTapped(object sender, TappedEventArgs e)
    {
        await NavigationService.GoToAsync("RoomAddView");
    }

    private async void OnRoomTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is Room room)
        {
            await NavigationService.GoToAsync("RoomEditPage",
                new Dictionary<string, object> { ["roomId"] = room.Id });
        }
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await NavigationService.GoBackAsync();
    }
}