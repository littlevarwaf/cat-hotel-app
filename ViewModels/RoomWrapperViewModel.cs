using CatHotel.Models;
using CatHotel.Services;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace CatHotel.ViewModels;

public class RoomWrapperViewModel : INotifyPropertyChanged, INavigationAware
{
    private readonly IRoomRepository _roomRepo;
    private readonly CartService _cart = CartService.Instance;
    private int _selectedTabIndex = 0;
    private Room? _room;
    private bool _isLoading = false;

    public RoomWrapperViewModel() : this(
        IPlatformApplication.Current!.Services.GetRequiredService<IRoomRepository>())
    { }

    public RoomWrapperViewModel(IRoomRepository roomRepo)
    {
        _roomRepo = roomRepo;
        GoToDetailCommand = new Command(() => SelectedTabIndex = 0);
        GoToShopCommand = new Command(() => SelectedTabIndex = 1);
        GoToCartCommand = new Command(async () =>
        {
            // Pass room data to CartPage
            var parameters = new Dictionary<string, object>
            {
                ["room"] = Room!
            };
            await NavigationService.GoToAsync(NavigationService.CartPage, parameters);
        });
        GoBackCommand = new Command(async () => await NavigationService.GoBackAsync());
        
        // Subscribe to cart changes
        if (_cart is INotifyPropertyChanged notifyPropertyChanged)
        {
            notifyPropertyChanged.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(_cart.Count))
                {
                    CartItemCount = _cart.Count;
                }
            };
        }
        
        // Initialize cart count
        CartItemCount = _cart.Count;
    }

    public Room? Room
    {
        get => _room;
        set
        {
            if (_room != value)
            {
                _room = value;
                OnPropertyChanged();
            }
        }
    }

    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set
        {
            if (_selectedTabIndex != value)
            {
                _selectedTabIndex = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (_isLoading != value)
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }
    }

    private int _cartItemCount;
    public int CartItemCount
    {
        get => _cartItemCount;
        set 
        {
            SetProperty(ref _cartItemCount, value);
            OnPropertyChanged(nameof(ShowCartBadge));
        }
    }

    public bool ShowCartBadge => CartItemCount > 0;

    public ICommand GoToDetailCommand { get; }
    public ICommand GoToShopCommand { get; }
    public ICommand GoToCartCommand { get; }
    public ICommand GoBackCommand { get; }

    public async void OnNavigatedTo(IDictionary<string, object> parameters)
    {
        System.Diagnostics.Debug.WriteLine($"[RoomWrapper] OnNavigatedTo called with params: {string.Join(",", parameters.Keys)}");

        if (parameters.TryGetValue("roomId", out var idObj) && idObj is int roomId)
        {
            System.Diagnostics.Debug.WriteLine($"[RoomWrapper] Loading room ID: {roomId}");
            IsLoading = true;
            try
            {
                Room = await _roomRepo.GetRoomByIdAsync(roomId);
                if (Room != null)
                {
                    var checkIn = parameters.TryGetValue("checkIn", out var d) && d is DateTime dt
                        ? dt
                        : (DateTime?)null;
                    BookingDraftService.Instance.ResetForRoom(roomId, checkIn);
                }
                System.Diagnostics.Debug.WriteLine($"[RoomWrapper] Room loaded: {Room?.Name ?? "NULL"}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RoomWrapper] Error loading room: {ex}");
                await NavigationService.GoBackAsync();
            }
            finally
            {
                IsLoading = false;
            }
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("[RoomWrapper] No roomId parameter found!");
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}