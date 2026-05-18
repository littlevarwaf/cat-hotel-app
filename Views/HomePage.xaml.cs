using CatHotel.Models;
using CatHotel.Services;

namespace CatHotel.Views;

public partial class HomePage : ContentView
{
    private bool _isInitialized = false;
    private readonly IRoomRepository _roomRepo;

    public HomePage() : this(
        IPlatformApplication.Current!.Services.GetRequiredService<IRoomRepository>())
    { }

    public HomePage(IRoomRepository roomRepo)
    {
        InitializeComponent();
        _roomRepo = roomRepo;
        this.Loaded += OnViewLoaded;

        // ✅ subscribe เหมือน RoomsView เลย
        RoomService.RoomAdded += async (s, e) => await MainThread.InvokeOnMainThreadAsync(LoadRoomsAsync);
        RoomService.RoomUpdated += async (s, e) => await MainThread.InvokeOnMainThreadAsync(LoadRoomsAsync);
        RoomService.RoomDeleted += async (s, e) => await MainThread.InvokeOnMainThreadAsync(LoadRoomsAsync);
    }

    private async void OnViewLoaded(object? sender, EventArgs e)
    {
        if (_isInitialized) return;
        _isInitialized = true;

        try
        {
            DateLabel.Text = DateTime.Now.ToString("dd/MM/yyyy");
            EditButton.IsVisible = true;
            await LoadRoomsAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("[HOME][FATAL] " + ex);
            throw;
        }
    }

    public async Task RefreshRoomsAsync()
    {
        await LoadRoomsAsync();
    }

    private async Task LoadRoomsAsync()
    {
        LoadingIndicator.IsRunning = true;
        LoadingIndicator.IsVisible = true;

        try
        {
            var rooms = await _roomRepo.GetAllRoomsAsync();
            var occupiedRooms = rooms.Where(r => r.Status == RoomStatus.Occupied).ToList();
            var viewModels = occupiedRooms.Select(r => new RoomViewModel(r)).ToList();
            RoomsCollection.ItemsSource = viewModels;

            OccupiedLabel.Text = $"Occupied Rooms: {occupiedRooms.Count}";
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("[HOME] LoadRooms error: " + ex);
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
        }
    }

    private async void OnRoomTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is RoomViewModel vm)
        {
            await NavigationService.GoToAsync(
                "RoomWrapper",
                new Dictionary<string, object> { ["roomId"] = vm.Room.Id });
        }
    }

    private async void OnEditClicked(object? sender, EventArgs e)
    {
        await NavigationService.GoToAsync("SettingsMenuPage");
    }
}

public class RoomViewModel
{
    public Room Room { get; }
    public RoomViewModel(Room room) => Room = room;
    public string Name => Room.Name;
    public string? ImgUrl => Room.ImgUrl;
    public RoomStatus Status => Room.Status;
    public RoomTypes RoomType => Room.RoomType;
    public string RoomTypeDisplay => Room.RoomType.ToString();
    public double BasePrice => Room.BasePrice;
    public int Id => Room.Id;
}