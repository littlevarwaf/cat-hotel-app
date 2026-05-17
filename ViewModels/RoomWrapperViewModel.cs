using CatHotel.Models;
using CatHotel.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace CatHotel.ViewModels;

public class RoomWrapperViewModel : INotifyPropertyChanged, INavigationAware
{
    private readonly IRoomRepository _roomRepo;
    private readonly IBookingRepository _bookingRepo;
    private readonly ICustomerRepository _customerRepo;
    private readonly ICatRepository _catRepo;
    private readonly IBookingCatRepository _bookingCatRepo;
    private readonly CartService _cart = CartService.Instance;

    private int _selectedTabIndex = 0;
    private Room? _room;
    private Booking? _booking;
    private Customer? _customer;
    private ObservableCollection<Cat> _cats = new();
    private ObservableCollection<BookingItem> _bookingItems = new();
    private bool _isLoading = false;
    private string _dateRangeDisplay = string.Empty;
    private double _totalPrice = 0;

    public RoomWrapperViewModel() : this(
        IPlatformApplication.Current!.Services.GetRequiredService<IRoomRepository>(),
        IPlatformApplication.Current!.Services.GetRequiredService<IBookingRepository>(),
        IPlatformApplication.Current!.Services.GetRequiredService<ICustomerRepository>(),
        IPlatformApplication.Current!.Services.GetRequiredService<ICatRepository>(),
        IPlatformApplication.Current!.Services.GetRequiredService<IBookingCatRepository>())
    { }

    public RoomWrapperViewModel(
        IRoomRepository roomRepo,
        IBookingRepository bookingRepo,
        ICustomerRepository customerRepo,
        ICatRepository catRepo,
        IBookingCatRepository bookingCatRepo)
    {
        _roomRepo = roomRepo;
        _bookingRepo = bookingRepo;
        _customerRepo = customerRepo;
        _catRepo = catRepo;
        _bookingCatRepo = bookingCatRepo;

        GoToDetailCommand = new Command(() => SelectedTabIndex = 0);
        GoToShopCommand = new Command(() => SelectedTabIndex = 1);
        GoToCartCommand = new Command(async () =>
        {
            if (Booking == null)
            {
                await Application.Current!.MainPage!.DisplayAlertAsync("Error",
                    "No active booking found for this room.", "OK");
                return;
            }

            var parameters = new Dictionary<string, object>
            {
                ["room"] = Room!,
                ["booking"] = Booking
            };
            await NavigationService.GoToAsync(NavigationService.CartPage, parameters);
        });
        GoBackCommand = new Command(async () => await NavigationService.GoBackAsync());
        EditCustomerCommand = new Command(async () => await EditCustomerAsync());
        AddCustomerCommand = new Command(async () => await AddCustomerAsync());
        EditCatCommand = new Command<Cat>(async (cat) => await EditCatAsync(cat));
        RemoveCatCommand = new Command<Cat>(async (cat) => await RemoveCatAsync(cat));
        AddMoreCatsCommand = new Command(async () => await AddMoreCatsAsync());
        CheckoutCommand = new Command(async () => await CheckoutAsync());

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

    public Booking? Booking
    {
        get => _booking;
        set
        {
            if (_booking != value)
            {
                _booking = value;
                OnPropertyChanged();
            }
        }
    }

    public Customer? Customer
    {
        get => _customer;
        set
        {
            if (_customer != value)
            {
                _customer = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasCustomer));
            }
        }
    }

    public ObservableCollection<Cat> Cats
    {
        get => _cats;
        set
        {
            if (_cats != value)
            {
                _cats = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanAddMoreCats));
            }
        }
    }

    public ObservableCollection<BookingItem> BookingItems
    {
        get => _bookingItems;
        set
        {
            if (_bookingItems != value)
            {
                _bookingItems = value;
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

    public string DateRangeDisplay
    {
        get => _dateRangeDisplay;
        set
        {
            if (_dateRangeDisplay != value)
            {
                _dateRangeDisplay = value;
                OnPropertyChanged();
            }
        }
    }

    public double TotalPrice
    {
        get => _totalPrice;
        set
        {
            if (_totalPrice != value)
            {
                _totalPrice = value;
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
    public bool HasCustomer => Customer != null;
    public bool CanAddMoreCats => Booking != null && Cats.Count < Booking.Room?.MaxOccupants;

    public ICommand GoToDetailCommand { get; }
    public ICommand GoToShopCommand { get; }
    public ICommand GoToCartCommand { get; }
    public ICommand GoBackCommand { get; }
    public ICommand EditCustomerCommand { get; }
    public ICommand AddCustomerCommand { get; }
    public ICommand EditCatCommand { get; }
    public ICommand RemoveCatCommand { get; }
    public ICommand AddMoreCatsCommand { get; }
    public ICommand CheckoutCommand { get; }

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

                // Load active booking for this room
                if (Room != null)
                {
                    var bookings = await _bookingRepo.GetBookingsByRoomIdAsync(roomId);
                    System.Diagnostics.Debug.WriteLine($"[RoomWrapper] Found {bookings.Count} bookings for room {roomId}");

                    var activeBooking = bookings.FirstOrDefault(b =>
                        b.StartDate <= DateTime.Now && b.EndDate >= DateTime.Now);

                    if (activeBooking != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[RoomWrapper] Found active booking: {activeBooking.Id}");
                        await LoadBookingDataAsync(activeBooking.Id);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[RoomWrapper] No active booking found for room {roomId}");
                    }
                }
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

    private async Task LoadBookingDataAsync(int bookingId)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"[RoomWrapper] Loading booking data for bookingId: {bookingId}");

            Booking = await _bookingRepo.GetBookingByIdAsync(bookingId);

            if (Booking != null)
            {
                System.Diagnostics.Debug.WriteLine($"[RoomWrapper] Booking loaded: {Booking.Id}");

                // Load customer
                if (Booking.CustomerId > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[RoomWrapper] Loading customer: {Booking.CustomerId}");
                    Customer = await _customerRepo.GetCustomerByIdAsync(Booking.CustomerId);
                    System.Diagnostics.Debug.WriteLine($"[RoomWrapper] Customer loaded: {Customer?.Name ?? "NULL"}");
                }

                // Load cats
                System.Diagnostics.Debug.WriteLine($"[RoomWrapper] Loading cats for booking: {bookingId}");
                var cats = await _catRepo.GetCatsByBookingIdAsync(bookingId);
                System.Diagnostics.Debug.WriteLine($"[RoomWrapper] Found {cats.Count} cats");
                Cats = new ObservableCollection<Cat>(cats);

                // Load booking items
                System.Diagnostics.Debug.WriteLine($"[RoomWrapper] Loading booking items for booking: {bookingId}");
                var items = await _bookingRepo.GetBookingItemsByBookingIdAsync(bookingId);
                System.Diagnostics.Debug.WriteLine($"[RoomWrapper] Found {items.Count} booking items");
                await LoadBookingItemsWithDetailsAsync(items);
                BookingItems = new ObservableCollection<BookingItem>(items);

                // Calculate date range display
                UpdateDateRangeDisplay();

                // Calculate total price
                CalculateTotalPrice();

                System.Diagnostics.Debug.WriteLine($"[RoomWrapper] Booking data loaded successfully");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[RoomWrapper] Error loading booking data: {ex}");
        }
    }

    private async Task LoadBookingItemsWithDetailsAsync(IEnumerable<BookingItem> bookingItems)
    {
        try
        {
            // For each booking item, load the associated ShopItem
            foreach (var item in bookingItems)
            {
                if (item.ItemId > 0)
                {
                    // Get ShopItem from database
                    var shopItem = await App.Database.Db.Table<ShopItem>()
                        .Where(si => si.Id == item.ItemId)
                        .FirstOrDefaultAsync();

                    if (shopItem != null)
                    {
                        item.ShopItem = shopItem;
                        System.Diagnostics.Debug.WriteLine($"[RoomWrapper] Loaded ShopItem for BookingItem {item.Id}: {shopItem.Name}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[RoomWrapper] Error loading ShopItem details: {ex}");
        }
    }

    // Update the RefreshBookingItemsAsync method to include ShopItem loading:
    public async Task RefreshBookingItemsAsync(int bookingId)
    {
        try
        {
            var items = await _bookingRepo.GetBookingItemsByBookingIdAsync(bookingId);
            await LoadBookingItemsWithDetailsAsync(items);
            BookingItems = new ObservableCollection<BookingItem>(items);
            CalculateTotalPrice();
            System.Diagnostics.Debug.WriteLine($"[RoomWrapper] BookingItems refreshed with details: {items.Count} items");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[RoomWrapper] Error refreshing booking items: {ex}");
        }
    }

    private void UpdateDateRangeDisplay()
    {
        if (Booking == null) return;

        var startDate = Booking.StartDate;
        var endDate = Booking.EndDate;
        var days = (int)(endDate - startDate).TotalDays;

        DateRangeDisplay = $"{startDate:dd.MM.yy} - {endDate:dd.MM.yy} ({days} Days)";
        System.Diagnostics.Debug.WriteLine($"[RoomWrapper] DateRangeDisplay: {DateRangeDisplay}");
    }

    private void CalculateTotalPrice()
    {
        if (Booking == null) return;

        var startDate = Booking.StartDate;
        var endDate = Booking.EndDate;
        var nights = (int)(endDate - startDate).TotalDays;
        var roomCharge = Booking.Room?.BasePrice * nights ?? 0;

        var shopCharge = BookingItems.Sum(item => item.UnitPrice * item.Quantity);

        TotalPrice = roomCharge + shopCharge;

        if (Booking != null)
        {
            Booking.TotalPrice = TotalPrice;
        }

        System.Diagnostics.Debug.WriteLine($"[RoomWrapper] TotalPrice calculated: {TotalPrice}");
    }

    private async Task EditCustomerAsync()
    {
        if (Customer == null) return;
        var parameters = new Dictionary<string, object> { ["customerId"] = Customer.Id };
        await NavigationService.GoToAsync(NavigationService.CustomerWrapperPage, parameters);
    }

    private async Task AddCustomerAsync()
    {
        await NavigationService.GoToAsync(NavigationService.CustomerWrapperPage);
    }

    private async Task EditCatAsync(Cat cat)
    {
        var parameters = new Dictionary<string, object> { ["catId"] = cat.Id };
        await NavigationService.GoToAsync(NavigationService.CatWrapperPage, parameters);
    }

    private async Task RemoveCatAsync(Cat cat)
    {
        if (Booking == null) return;

        bool confirm = await Application.Current!.MainPage!.DisplayAlertAsync("Remove Cat",
            $"Remove '{cat.Name}' from this booking?", "Remove", "Cancel");
        if (!confirm) return;

        try
        {
            System.Diagnostics.Debug.WriteLine($"[RoomWrapper] Removing cat {cat.Id} from booking {Booking.Id}");

            // Remove the cat from the booking via BookingCat table
            await _bookingCatRepo.RemoveCatFromBookingAsync(Booking.Id, cat.Id);

            // Remove from UI
            Cats.Remove(cat);

            // Refresh CanAddMoreCats property
            OnPropertyChanged(nameof(CanAddMoreCats));

            System.Diagnostics.Debug.WriteLine($"[RoomWrapper] Cat {cat.Id} removed successfully");
            await Application.Current!.MainPage!.DisplayAlertAsync("Success", $"'{cat.Name}' has been removed from this booking.", "OK");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[RoomWrapper] Error removing cat: {ex}");
            await Application.Current!.MainPage!.DisplayAlertAsync("Error",
                $"Failed to remove cat: {ex.Message}", "OK");
        }
    }

    private async Task AddMoreCatsAsync()
    {
        if (Booking == null)
        {
            await Application.Current!.MainPage!.DisplayAlertAsync("Error",
                "No active booking found.", "OK");
            return;
        }

        System.Diagnostics.Debug.WriteLine($"[RoomWrapper] Adding more cats for booking: {Booking.Id}");

        await NavigationService.GoToAsync(NavigationService.CatWrapperPage,
            new Dictionary<string, object>
            {
                ["mode"] = 0,
                ["bookingId"] = Booking.Id
            });
    }

    private async Task CheckoutAsync()
    {
        if (Booking == null)
        {
            await Application.Current!.MainPage!.DisplayAlertAsync("Error", "No booking found.", "OK");
            return;
        }

        bool confirm = await Application.Current!.MainPage!.DisplayAlertAsync(
            "Check Out",
            $"Check out booking #{Booking.Id}?",
            "Yes",
            "Cancel");

        if (!confirm) return;

        IsLoading = true;
        try
        {
            System.Diagnostics.Debug.WriteLine($"[RoomWrapper] Checking out booking {Booking.Id}");

            // Initialize database
            await App.Database.InitializeAsync();

            // 1. Update Booking's EndDate to current time
            var checkoutTime = DateTime.Now;
            Booking.EndDate = checkoutTime;

            await App.Database.Db.UpdateAsync(Booking);
            System.Diagnostics.Debug.WriteLine($"[RoomWrapper] Updated booking {Booking.Id} EndDate to {checkoutTime}");

            // 2. Calculate RoomRevenue (BasePrice * number of nights)
            var nights = Math.Max(1, (Booking.EndDate.Date - Booking.StartDate.Date).Days);
            var roomRevenue = (int)(Booking.Room?.BasePrice * nights ?? 0);

            // 3. Calculate ShopRevenue from BookingItems
            var shopRevenue = (int)BookingItems.Sum(item => item.UnitPrice * item.Quantity);

            // 4. Create and insert Sale entry
            var sale = new Sale
            {
                BookingId = Booking.Id,
                RoomId = Booking.RoomId,
                RoomRevenue = roomRevenue,
                ShopRevenue = shopRevenue,
                TotalRevenue = roomRevenue + shopRevenue,
                CompletedAt = checkoutTime
            };

            await App.Database.Db.InsertAsync(sale);
            System.Diagnostics.Debug.WriteLine($"[RoomWrapper] Created sale entry: BookingId={sale.BookingId}, RoomRevenue={roomRevenue}, ShopRevenue={shopRevenue}");

            // 5. Update Room status to Available
            if (Booking.Room != null)
            {
                Booking.Room.Status = RoomStatus.Available;
                await App.Database.Db.UpdateAsync(Booking.Room);
                System.Diagnostics.Debug.WriteLine($"[RoomWrapper] Updated room {Booking.Room.Id} status to Available");
            }

            await Application.Current!.MainPage!.DisplayAlertAsync(
                "Success",
                $"Check out successful! Sale #{sale.Id} created.",
                "OK");

            await NavigationService.GoBackAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[RoomWrapper] Error during checkout: {ex}");
            await Application.Current!.MainPage!.DisplayAlertAsync(
                "Error",
                $"Checkout failed: {ex.Message}",
                "OK");
        }
        finally
        {
            IsLoading = false;
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