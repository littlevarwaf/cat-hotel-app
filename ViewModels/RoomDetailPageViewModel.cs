using CatHotel.Models;
using CatHotel.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace CatHotel.ViewModels;

public class RoomDetailPageViewModel : INotifyPropertyChanged, INavigationAware
{
    private readonly IBookingRepository _bookingRepo;
    private readonly ICustomerRepository _customerRepo;
    private readonly ICatRepository _catRepo;
    private readonly IBookingCatRepository _bookingCatRepo;
    private Booking? _booking;
    private Customer? _customer;
    private ObservableCollection<Cat> _cats = new();
    private ObservableCollection<BookingItem> _bookingItems = new();
    private bool _isLoading = false;
    private string _dateRangeDisplay = string.Empty;
    private double _totalPrice = 0;
    private int _roomId = 0;

    public RoomDetailPageViewModel() : this(
        IPlatformApplication.Current!.Services.GetRequiredService<IBookingRepository>(),
        IPlatformApplication.Current!.Services.GetRequiredService<ICustomerRepository>(),
        IPlatformApplication.Current!.Services.GetRequiredService<ICatRepository>(),
        IPlatformApplication.Current!.Services.GetRequiredService<IBookingCatRepository>())
    { }

    public RoomDetailPageViewModel(
        IBookingRepository bookingRepo,
        ICustomerRepository customerRepo,
        ICatRepository catRepo,
        IBookingCatRepository bookingCatRepo)
    {
        _bookingRepo = bookingRepo;
        _customerRepo = customerRepo;
        _catRepo = catRepo;
        _bookingCatRepo = bookingCatRepo;

        EditCustomerCommand = new Command(async () => await EditCustomerAsync());
        AddCustomerCommand = new Command(async () => await AddCustomerAsync());
        EditCatCommand = new Command<Cat>(async (cat) => await EditCatAsync(cat));
        RemoveCatCommand = new Command<Cat>(async (cat) => await RemoveCatAsync(cat));
        AddMoreCatsCommand = new Command(async () => await AddMoreCatsAsync());
        CheckoutCommand = new Command(async () => await CheckoutAsync());
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

    public bool HasCustomer => Customer != null;

    public bool CanAddMoreCats => Booking != null && Cats.Count < Booking.Room?.MaxOccupants;

    public ICommand EditCustomerCommand { get; }
    public ICommand AddCustomerCommand { get; }
    public ICommand EditCatCommand { get; }
    public ICommand RemoveCatCommand { get; }
    public ICommand AddMoreCatsCommand { get; }
    public ICommand CheckoutCommand { get; }

    public async void OnNavigatedTo(IDictionary<string, object> parameters)
    {
        System.Diagnostics.Debug.WriteLine($"[RoomDetailPageViewModel] OnNavigatedTo called with params: {string.Join(",", parameters.Keys)}");

        // Try bookingId first
        if (parameters.TryGetValue("bookingId", out var bookingIdObj) && bookingIdObj is int bookingId)
        {
            System.Diagnostics.Debug.WriteLine($"[RoomDetailPageViewModel] Loading with bookingId: {bookingId}");
            await LoadBookingDataAsync(bookingId);
        }
        // Fallback to roomId - fetch active booking for that room
        else if (parameters.TryGetValue("roomId", out var roomIdObj) && roomIdObj is int roomId)
        {
            System.Diagnostics.Debug.WriteLine($"[RoomDetailPageViewModel] Loading with roomId: {roomId}");
            _roomId = roomId;
            await LoadActiveBookingForRoomAsync(roomId);
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("[RoomDetailPageViewModel] No bookingId or roomId parameter found!");
        }
    }

    private async Task LoadActiveBookingForRoomAsync(int roomId)
    {
        IsLoading = true;
        try
        {
            System.Diagnostics.Debug.WriteLine($"[RoomDetailPageViewModel] Fetching bookings for roomId: {roomId}");

            // Get all bookings for this room
            var bookings = await _bookingRepo.GetBookingsByRoomIdAsync(roomId);
            System.Diagnostics.Debug.WriteLine($"[RoomDetailPageViewModel] Found {bookings.Count} bookings for room {roomId}");

            // Find active booking (overlapping with today)
            var activeBooking = bookings.FirstOrDefault(b =>
                b.StartDate <= DateTime.Now && b.EndDate >= DateTime.Now);

            if (activeBooking != null)
            {
                System.Diagnostics.Debug.WriteLine($"[RoomDetailPageViewModel] Found active booking: {activeBooking.Id}");
                await LoadBookingDataAsync(activeBooking.Id);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[RoomDetailPageViewModel] No active booking found for room {roomId}");
                await Application.Current!.MainPage!.DisplayAlertAsync("No Booking",
                    "No active booking found for this room.", "OK");
                await NavigationService.GoBackAsync();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[RoomDetailPageViewModel] Error loading active booking: {ex}");
            await Application.Current!.MainPage!.DisplayAlertAsync("Error",
                $"Failed to load booking: {ex.Message}", "OK");
            await NavigationService.GoBackAsync();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadBookingDataAsync(int bookingId)
    {
        IsLoading = true;
        try
        {
            System.Diagnostics.Debug.WriteLine($"[RoomDetailPageViewModel] Loading booking data for bookingId: {bookingId}");

            // Load booking with related data
            Booking = await _bookingRepo.GetBookingByIdAsync(bookingId);

            if (Booking != null)
            {
                System.Diagnostics.Debug.WriteLine($"[RoomDetailPageViewModel] Booking loaded: {Booking.Id}");

                // Load customer
                if (Booking.CustomerId > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[RoomDetailPageViewModel] Loading customer: {Booking.CustomerId}");
                    Customer = await _customerRepo.GetCustomerByIdAsync(Booking.CustomerId);
                }

                // Load cats
                System.Diagnostics.Debug.WriteLine($"[RoomDetailPageViewModel] Loading cats for booking: {bookingId}");
                var cats = await _catRepo.GetCatsByBookingIdAsync(bookingId);
                System.Diagnostics.Debug.WriteLine($"[RoomDetailPageViewModel] Found {cats.Count} cats");
                Cats = new ObservableCollection<Cat>(cats);

                // Load booking items
                System.Diagnostics.Debug.WriteLine($"[RoomDetailPageViewModel] Loading booking items for booking: {bookingId}");
                await RefreshBookingItemsAsync(bookingId);

                // Calculate date range display
                UpdateDateRangeDisplay();

                // Calculate total price
                CalculateTotalPrice();

                System.Diagnostics.Debug.WriteLine($"[RoomDetailPageViewModel] Booking data loaded successfully");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[RoomDetailPageViewModel] Booking not found: {bookingId}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[RoomDetailPageViewModel] Error loading booking: {ex}");
            await Application.Current!.MainPage!.DisplayAlertAsync("Error",
                $"Failed to load booking: {ex.Message}", "OK");
            await NavigationService.GoBackAsync();
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task RefreshBookingItemsAsync(int bookingId)
    {
        try
        {
            var items = await _bookingRepo.GetBookingItemsByBookingIdAsync(bookingId);
            BookingItems = new ObservableCollection<BookingItem>(items);
            CalculateTotalPrice();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[RoomDetailPageViewModel] Error refreshing booking items: {ex}");
        }
    }

    private void UpdateDateRangeDisplay()
    {
        if (Booking == null) return;

        var startDate = Booking.StartDate;
        var endDate = Booking.EndDate;
        var days = (int)(endDate - startDate).TotalDays;

        // Format: DD.MM.YY - DD.MM.YY (X Days)
        DateRangeDisplay = $"{startDate:dd.MM.yy} - {endDate:dd.MM.yy} ({days} Days)";
    }

    private void CalculateTotalPrice()
    {
        if (Booking == null) return;

        // Room charge: BasePrice * Number of nights
        var startDate = Booking.StartDate;
        var endDate = Booking.EndDate;
        var nights = (int)(endDate - startDate).TotalDays;
        var roomCharge = Booking.Room?.BasePrice * nights ?? 0;

        // Shop items charge
        var shopCharge = BookingItems.Sum(item => item.UnitPrice * item.Quantity);

        // Total
        TotalPrice = roomCharge + shopCharge;

        // Update booking total price
        if (Booking != null)
        {
            Booking.TotalPrice = TotalPrice;
        }
    }

    private async Task EditCustomerAsync()
    {
        if (Customer == null) return;

        var parameters = new Dictionary<string, object>
        {
            ["customerId"] = Customer.Id
        };
        await NavigationService.GoToAsync(NavigationService.CustomerWrapperPage, parameters);
    }

    private async Task AddCustomerAsync()
    {
        await NavigationService.GoToAsync(NavigationService.CustomerWrapperPage);
    }

    private async Task EditCatAsync(Cat cat)
    {
        var parameters = new Dictionary<string, object>
        {
            ["catId"] = cat.Id
        };
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
            System.Diagnostics.Debug.WriteLine($"[RoomDetailPageViewModel] Removing cat {cat.Id} from booking {Booking.Id}");

            // Remove the cat from the booking via BookingCat table
            await _bookingCatRepo.RemoveCatFromBookingAsync(Booking.Id, cat.Id);

            // Remove from UI
            Cats.Remove(cat);

            // Refresh CanAddMoreCats property
            OnPropertyChanged(nameof(CanAddMoreCats));

            System.Diagnostics.Debug.WriteLine($"[RoomDetailPageViewModel] Cat {cat.Id} removed successfully");
            await Application.Current!.MainPage!.DisplayAlertAsync("Success", $"'{cat.Name}' has been removed from this booking.", "OK");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[RoomDetailPageViewModel] Error removing cat: {ex}");
            await Application.Current!.MainPage!.DisplayAlertAsync("Error",
                $"Failed to remove cat: {ex.Message}", "OK");
        }
    }

    private async Task AddMoreCatsAsync()
    {
        // Navigate to CatWrapperPage to add more cats
        await NavigationService.GoToAsync(NavigationService.CatWrapperPage);
    }

    private async Task CheckoutAsync()
    {
        // Placeholder - will handle checkout logic
        await NavigationService.GoBackAsync();
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