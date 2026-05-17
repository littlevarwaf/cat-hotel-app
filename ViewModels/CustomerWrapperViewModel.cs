using CatHotel.Models;
using CatHotel.Services;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace CatHotel.ViewModels;

public class CustomerWrapperViewModel : INotifyCollectionChanged, INotifyPropertyChanged, INavigationAware
{
    private readonly ICustomerRepository _customerRepo;
    private readonly IBookingRepository _bookingRepo;

    private int _selectedTabIndex = 0;
    private int _roomId = 0;
    private int _bookingId = 0;
    private int _mode = 0; // 0 = RoomDetailPage, 1 = BookingPage
    private bool _isLoading = false;
    private ObservableCollection<Customer> _customers = new();
    private Customer? _selectedCustomer;

    public CustomerWrapperViewModel() : this(
        IPlatformApplication.Current!.Services.GetRequiredService<ICustomerRepository>(),
        IPlatformApplication.Current!.Services.GetRequiredService<IBookingRepository>())
    { }

    public CustomerWrapperViewModel(
        ICustomerRepository customerRepo,
        IBookingRepository bookingRepo)
    {
        _customerRepo = customerRepo;
        _bookingRepo = bookingRepo;

        GoBackCommand = new Command(async () => await NavigationService.GoBackAsync());
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

    public int RoomId
    {
        get => _roomId;
        set
        {
            if (_roomId != value)
            {
                _roomId = value;
                OnPropertyChanged();
            }
        }
    }

    public int BookingId
    {
        get => _bookingId;
        set
        {
            if (_bookingId != value)
            {
                _bookingId = value;
                OnPropertyChanged();
            }
        }
    }

    public int Mode
    {
        get => _mode;
        set
        {
            if (_mode != value)
            {
                _mode = value;
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

    public ObservableCollection<Customer> Customers
    {
        get => _customers;
        set
        {
            if (_customers != value)
            {
                _customers = value;
                OnPropertyChanged();
            }
        }
    }

    public Customer? SelectedCustomer
    {
        get => _selectedCustomer;
        set
        {
            if (_selectedCustomer != value)
            {
                _selectedCustomer = value;
                OnPropertyChanged();
            }
        }
    }

    public ICommand GoBackCommand { get; }

    public async void OnNavigatedTo(IDictionary<string, object> parameters)
    {
        System.Diagnostics.Debug.WriteLine($"[CustomerWrapper] OnNavigatedTo called with params: {string.Join(",", parameters.Keys)}");

        // Get mode (0 = RoomDetailPage, 1 = BookingPage)
        if (parameters.TryGetValue("mode", out var modeObj) && modeObj is int mode)
        {
            Mode = mode;
            System.Diagnostics.Debug.WriteLine($"[CustomerWrapper] Mode set to: {mode}");
        }

        // For mode 0 (RoomDetailPage), get bookingId directly
        if (Mode == 0 && parameters.TryGetValue("bookingId", out var bookingIdObj) && bookingIdObj is int bookingId)
        {
            BookingId = bookingId;
            System.Diagnostics.Debug.WriteLine($"[CustomerWrapper] BookingId set to: {bookingId}");
        }
        // For mode 1 (BookingPage), derive bookingId from roomId if needed
        else if (Mode == 1 && parameters.TryGetValue("roomId", out var roomIdObj) && roomIdObj is int roomId)
        {
            System.Diagnostics.Debug.WriteLine($"[CustomerWrapper] Loading with roomId: {roomId}");
            RoomId = roomId;
            await LoadBookingIdForRoomAsync(roomId);
        }

        // Load all customers for selection
        await LoadCustomersAsync();
    }

    private async Task LoadBookingIdForRoomAsync(int roomId)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"[CustomerWrapper] Fetching active booking for roomId: {roomId}");
            var bookings = await _bookingRepo.GetBookingsByRoomIdAsync(roomId);

            var activeBooking = bookings.FirstOrDefault(b =>
                b.StartDate <= DateTime.Now && b.EndDate >= DateTime.Now);

            if (activeBooking != null)
            {
                BookingId = activeBooking.Id;
                System.Diagnostics.Debug.WriteLine($"[CustomerWrapper] Found active booking: {activeBooking.Id}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CustomerWrapper] Error loading booking: {ex}");
        }
    }

    private async Task LoadCustomersAsync()
    {
        IsLoading = true;
        try
        {
            System.Diagnostics.Debug.WriteLine($"[CustomerWrapper] Loading all customers");
            var customers = await _customerRepo.GetAllCustomersAsync();
            Customers = new ObservableCollection<Customer>(customers);
            System.Diagnostics.Debug.WriteLine($"[CustomerWrapper] Loaded {customers.Count} customers");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CustomerWrapper] Error loading customers: {ex}");
            await Application.Current!.MainPage!.DisplayAlertAsync("Error",
                $"Failed to load customers: {ex.Message}", "OK");
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task RefreshCustomersAsync()
    {
        await LoadCustomersAsync();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public event NotifyCollectionChangedEventHandler CollectionChanged;

    protected void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        CollectionChanged?.Invoke(this, e);
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