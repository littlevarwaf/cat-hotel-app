using CatHotel.Models;
using CatHotel.Services;

namespace CatHotel.Views;

public partial class HomePage : ContentPage
{
    private readonly IRoomRepository _roomRepo;

    public HomePage(IRoomRepository roomRepo)
    {
        InitializeComponent();
        _roomRepo = roomRepo;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        DateLabel.Text = DateTime.Now.ToString("dd/MM/yyyy");
        EditButton.IsVisible = true;
        await LoadRoomsAsync();
    }

    private async Task LoadRoomsAsync()
    {
        LoadingIndicator.IsRunning = true;
        LoadingIndicator.IsVisible = true;

        try
        {
            var rooms = await _roomRepo.GetAllRoomsAsync();
            var viewModels = rooms.Select(r => new RoomViewModel(r)).ToList();
            RoomsCollection.ItemsSource = viewModels;

            int available = rooms.Count(r => r.Status == RoomStatus.Available);
            AvailableLabel.Text = $"Available Rooms: {available}";
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Cannot load rooms: {ex.Message}", "OK");
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
        }
    }

    private async void OnRoomTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is RoomViewModel vm)
        {
            await NavigationService.GoToAsync(
                NavigationService.RoomDetail,
                new Dictionary<string, object> { ["Room"] = vm.Room });
        }
    }

    private async void OnEditClicked(object sender, EventArgs e)
    {
        await NavigationService.GoToAsync("EditRoomPage");
    }
}

public class RoomViewModel
{
    public Room Room { get; }

    public RoomViewModel(Room room) => Room = room;

    public string Name => Room.Name;
    public string RoomTypeDisplay => Room.RoomType switch
    {
        RoomTypes.Small  => "Small room",
        RoomTypes.Medium => "Mid-sized room",
        RoomTypes.Large  => "Large room",
        _                => "Unknown"
    };
    public string BasePriceDisplay => $"฿{Room.BasePrice:N0} / night";

    public string StatusBadgeText => Room.Status switch
    {
        RoomStatus.Available   => "Available ●",
        RoomStatus.Unavailable => "Not Available ●",
        RoomStatus.Occupied    => "Occupied ●",
        _                      => "Unknown"
    };

    public Color StatusBadgeColor => Room.Status switch
    {
        RoomStatus.Available   => Color.FromArgb("#4CAF50"),
        RoomStatus.Unavailable => Color.FromArgb("#F44336"),
        RoomStatus.Occupied    => Color.FromArgb("#FF9800"),
        _                      => Color.FromArgb("#9E9E9E")
    };
}
