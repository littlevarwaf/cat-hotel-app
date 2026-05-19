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
    private Discount? _appliedDiscount;
    private double _discountAmount = 0;
    private double _finalTotalPrice = 0;
    private bool _hasAppliedDiscount = false;

    private int _plannedNights = 0;
    private int _actualNights = 0;
    private double _earlyCheckOutAdjustment = 0;
    private string _roomChargeDescription = string.Empty;

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
        ApplyDiscountCommand = new Command(async () => await ApplyDiscountAsync());
        RemoveDiscountCommand = new Command(async () => await RemoveDiscountAsync());

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

        // Subscribe to customer changes
        CustomerService.CustomerUpdated += OnCustomerUpdated;
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
                RecalculateFinalTotal();
            }
        }
    }

    public Discount? AppliedDiscount
    {
        get => _appliedDiscount;
        set
        {
            if (_appliedDiscount != value)
            {
                _appliedDiscount = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasAppliedDiscount));
            }
        }
    }

    public double DiscountAmount
    {
        get => _discountAmount;
        set
        {
            if (_discountAmount != value)
            {
                _discountAmount = value;
                OnPropertyChanged();
            }
        }
    }

    public double FinalTotalPrice
    {
        get => _finalTotalPrice;
        set
        {
            if (_finalTotalPrice != value)
            {
                _finalTotalPrice = value;
                OnPropertyChanged();
            }
        }
    }

    public bool HasAppliedDiscount => _appliedDiscount != null;

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
    public ICommand ApplyDiscountCommand { get; }
    public ICommand RemoveDiscountCommand { get; }

    public string? DiscountCodeInput { get; set; }

    public int PlannedNights
    {
        get => _plannedNights;
        set
        {
            if (_plannedNights != value)
            {
                _plannedNights = value;
                OnPropertyChanged();
            }
        }
    }

    public int ActualNights
    {
        get => _actualNights;
        set
        {
            if (_actualNights != value)
            {
                _actualNights = value;
                OnPropertyChanged();
            }
        }
    }

    public double EarlyCheckOutAdjustment
    {
        get => _earlyCheckOutAdjustment;
        set
        {
            if (_earlyCheckOutAdjustment != value)
            {
                _earlyCheckOutAdjustment = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasEarlyCheckOut));
                RecalculateFinalTotal();
            }
        }
    }

    public string RoomChargeDescription
    {
        get => _roomChargeDescription;
        set
        {
            if (_roomChargeDescription != value)
            {
                _roomChargeDescription = value;
                OnPropertyChanged();
            }
        }
    }

    public bool HasEarlyCheckOut => Math.Abs(_earlyCheckOutAdjustment) > 0.001;

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

                // Calculate nights and adjustment
                CalculateNightsAndAdjustment();

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

    public async Task RefreshBookingCatsAsync(int bookingId)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"[RoomWrapper] Refreshing cats for booking: {bookingId}");
            var cats = await _catRepo.GetCatsByBookingIdAsync(bookingId);
            Cats = new ObservableCollection<Cat>(cats);
            OnPropertyChanged(nameof(CanAddMoreCats));
            System.Diagnostics.Debug.WriteLine($"[RoomWrapper] ✅ Cats refreshed: {cats.Count} cats loaded");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[RoomWrapper] Error refreshing cats: {ex}");
        }
    }

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
        var days = (endDate.Date - startDate.Date).Days;

        DateRangeDisplay = $"{startDate:dd.MM.yy} - {endDate:dd.MM.yy} ({days} Days)";
        System.Diagnostics.Debug.WriteLine($"[RoomWrapper] DateRangeDisplay: {DateRangeDisplay}");
    }

    private void CalculateTotalPrice()
    {
        if (Booking == null) return;

        var startDate = Booking.StartDate;
        var endDate = Booking.EndDate;
        var nights = (endDate.Date - startDate.Date).Days;
        var roomCharge = Booking.Room?.BasePrice * nights ?? 0;

        var shopCharge = BookingItems.Sum(item => item.UnitPrice * item.Quantity);

        TotalPrice = roomCharge + shopCharge;

        if (Booking != null)
        {
            Booking.TotalPrice = TotalPrice;
        }

        System.Diagnostics.Debug.WriteLine($"[RoomWrapper] TotalPrice calculated: {TotalPrice}");
    }

    private void RecalculateFinalTotal()
    {
        FinalTotalPrice = TotalPrice - DiscountAmount - Math.Abs(EarlyCheckOutAdjustment);
        System.Diagnostics.Debug.WriteLine($"[RoomWrapper] FinalTotalPrice: {FinalTotalPrice} (Total: {TotalPrice}, Discount: {DiscountAmount}, Early Checkout: {EarlyCheckOutAdjustment})");
    }

    private void CalculateNightsAndAdjustment()
    {
        if (Booking == null || Booking.Room == null) return;

        var startDate = Booking.StartDate;
        var endDate = Booking.EndDate;
        var plannedNights = (endDate.Date - startDate.Date).Days;
        var actualNights = (DateTime.Now.Date - startDate.Date).Days;

        PlannedNights = plannedNights;
        ActualNights = actualNights;

        // Calculate the room charge for actual nights
        var roomChargeForActualNights = Booking.Room.BasePrice * actualNights;

        // Calculate the adjustment (negative if checked out early)
        var earlyCheckOutNights = plannedNights - actualNights;
        EarlyCheckOutAdjustment = earlyCheckOutNights > 0 ? -(Booking.Room.BasePrice * earlyCheckOutNights) : 0;

        // Create a descriptive string for the room charge
        RoomChargeDescription = $"{Booking.Room.Name} - {plannedNights} Days";

        System.Diagnostics.Debug.WriteLine($"[RoomWrapper] Nights Calculation:");
        System.Diagnostics.Debug.WriteLine($"  Planned: {plannedNights}, Actual: {actualNights}");
        System.Diagnostics.Debug.WriteLine($"  Early Checkout Nights: {earlyCheckOutNights}");
        System.Diagnostics.Debug.WriteLine($"  Early Checkout Adjustment: {EarlyCheckOutAdjustment}");
    }

    private async Task ApplyDiscountAsync()
    {
        if (string.IsNullOrWhiteSpace(DiscountCodeInput))
        {
            await Application.Current!.MainPage!.DisplayAlertAsync("Error", "Please enter a discount code.", "OK");
            return;
        }

        try
        {
            System.Diagnostics.Debug.WriteLine($"[RoomWrapper] Applying discount code: {DiscountCodeInput}");

            // Search for discount by code
            var discount = await App.Database.Db.Table<Discount>()
                .Where(d => d.Code.ToUpper() == DiscountCodeInput.ToUpper())
                .FirstOrDefaultAsync();

            if (discount == null)
            {
                await Application.Current!.MainPage!.DisplayAlertAsync("Error", "Discount code not found.", "OK");
                return;
            }

            // Check if discount is expired
            if (discount.ExpirationDate < DateTime.Now)
            {
                await Application.Current!.MainPage!.DisplayAlertAsync("Error", "This discount code has expired.", "OK");
                return;
            }

            // Check if discount has remaining uses
            if (discount.UsedCount >= discount.Quantity)
            {
                await Application.Current!.MainPage!.DisplayAlertAsync("Error", "This discount code has no remaining uses.", "OK");
                return;
            }

            // Apply discount
            AppliedDiscount = discount;
            DiscountAmount = discount.Amount;
            RecalculateFinalTotal();
            DiscountCodeInput = string.Empty;

            System.Diagnostics.Debug.WriteLine($"[RoomWrapper] ✅ Discount applied: {discount.Code} - Amount: {discount.Amount}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[RoomWrapper] Error applying discount: {ex}");
            await Application.Current!.MainPage!.DisplayAlertAsync("Error", $"Error applying discount: {ex.Message}", "OK");
        }
    }

    private async Task RemoveDiscountAsync()
    {
        AppliedDiscount = null;
        DiscountAmount = 0;
        RecalculateFinalTotal();
        System.Diagnostics.Debug.WriteLine($"[RoomWrapper] Discount removed");
        await Task.CompletedTask;
    }

    private async void OnCustomerUpdated(object? sender, CustomerEventArgs e)
    {
        if (Booking != null && e.Customer != null)
        {
            System.Diagnostics.Debug.WriteLine($"[RoomWrapper] 🔄 Customer updated event received for customer {e.Customer.Id}");

            // Update the in-memory booking's CustomerId to match the database
            Booking.CustomerId = e.Customer.Id;
            System.Diagnostics.Debug.WriteLine($"[RoomWrapper] Updated Booking.CustomerId to {e.Customer.Id}");

            // Refresh the customer display
            await RefreshCustomerAsync(e.Customer.Id);
        }
    }

    private async Task RefreshCustomerAsync(int customerId)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"[RoomWrapper] Refreshing customer {customerId}");
            var customer = await _customerRepo.GetCustomerByIdAsync(customerId);
            Customer = customer;
            System.Diagnostics.Debug.WriteLine($"[RoomWrapper] ✅ Customer refreshed: {customer?.Name ?? "NULL"}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[RoomWrapper] Error refreshing customer: {ex}");
        }
    }

    private async Task EditCustomerAsync()
    {
        if (Booking == null) return;

        var parameters = new Dictionary<string, object>
        {
            ["mode"] = 0,  // 0 = RoomDetailPage (existing booking)
            ["bookingId"] = Booking.Id
        };

        await NavigationService.GoToAsync(NavigationService.CustomerWrapperPage, parameters);
    }

    private async Task AddCustomerAsync()
    {
        if (Booking == null)
        {
            await Application.Current!.MainPage!.DisplayAlertAsync("Error",
                "No active booking found.", "OK");
            return;
        }

        var parameters = new Dictionary<string, object>
        {
            ["mode"] = 0,  // 0 = RoomDetailPage (existing booking)
            ["bookingId"] = Booking.Id
        };

        await NavigationService.GoToAsync(NavigationService.CustomerWrapperPage, parameters);
    }

    private async Task EditCatAsync(Cat cat)
    {
        if (Booking == null) return;

        // Get the BookingCat record for this cat
        var bookingCat = await App.Database.Db.Table<BookingCat>()
            .Where(bc => bc.BookingId == Booking.Id && bc.CatId == cat.Id)
            .FirstOrDefaultAsync();

        if (bookingCat == null)
        {
            System.Diagnostics.Debug.WriteLine($"[RoomWrapper] Error: BookingCat not found for booking {Booking.Id} and cat {cat.Id}");
            await Application.Current!.MainPage!.DisplayAlertAsync("Error", "Could not find cat record in booking.", "OK");
            return;
        }

        System.Diagnostics.Debug.WriteLine($"[RoomWrapper] Editing cat: {cat.Name} (BookingCatId: {bookingCat.Id})");

        // Navigate with the BookingCat ID
        var parameters = new Dictionary<string, object>
        {
            ["mode"] = 0,  // RoomDetailPage mode
            ["bookingId"] = Booking.Id,
            ["editingBookingCatId"] = bookingCat.Id
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
            System.Diagnostics.Debug.WriteLine($"[RoomWrapper] Applied Discount: {AppliedDiscount?.Code ?? "NONE"}");
            System.Diagnostics.Debug.WriteLine($"[RoomWrapper] Discount Amount: {DiscountAmount}");
            System.Diagnostics.Debug.WriteLine($"[RoomWrapper] Total Price Before Discount: {TotalPrice}");
            System.Diagnostics.Debug.WriteLine($"[RoomWrapper] Final Total Price: {FinalTotalPrice}");

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

            // 4. Calculate discount amount at checkout
            var discountAmountAtCheckout = (int)DiscountAmount;
            var finalTotal = roomRevenue + shopRevenue - discountAmountAtCheckout;

            System.Diagnostics.Debug.WriteLine($"[RoomWrapper] Room Revenue: {roomRevenue}");
            System.Diagnostics.Debug.WriteLine($"[RoomWrapper] Shop Revenue: {shopRevenue}");
            System.Diagnostics.Debug.WriteLine($"[RoomWrapper] Discount Amount at Checkout: {discountAmountAtCheckout}");
            System.Diagnostics.Debug.WriteLine($"[RoomWrapper] Final Total Revenue: {finalTotal}");

            // 5. Create and insert Sale entry with discount info
            var sale = new Sale
            {
                BookingId = Booking.Id,
                RoomId = Booking.RoomId,
                RoomRevenue = roomRevenue,
                ShopRevenue = shopRevenue,
                TotalRevenue = finalTotal,
                DiscountId = AppliedDiscount?.Id,  // NULL if no discount applied
                CompletedAt = checkoutTime
            };

            await App.Database.Db.InsertAsync(sale);
            System.Diagnostics.Debug.WriteLine($"[RoomWrapper] ✅ Created sale entry:");
            System.Diagnostics.Debug.WriteLine($"    - SaleId: {sale.Id}");
            System.Diagnostics.Debug.WriteLine($"    - BookingId: {sale.BookingId}");
            System.Diagnostics.Debug.WriteLine($"    - DiscountId: {sale.DiscountId}");
            System.Diagnostics.Debug.WriteLine($"    - RoomRevenue: {sale.RoomRevenue}");
            System.Diagnostics.Debug.WriteLine($"    - ShopRevenue: {sale.ShopRevenue}");
            System.Diagnostics.Debug.WriteLine($"    - TotalRevenue: {sale.TotalRevenue}");

            // 6. If discount was applied, increment UsedCount
            if (AppliedDiscount != null)
            {
                AppliedDiscount.UsedCount++;
                await App.Database.Db.UpdateAsync(AppliedDiscount);
                System.Diagnostics.Debug.WriteLine($"[RoomWrapper] ✅ Discount {AppliedDiscount.Code} UsedCount incremented to {AppliedDiscount.UsedCount}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[RoomWrapper] No discount applied");
            }

            // 7. Update Room status to Available
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

    /// <summary>
    /// Helper method to get discount details from database using DiscountId
    /// </summary>
    public async Task<Discount?> GetDiscountByIdAsync(int discountId)
    {
        try
        {
            return await App.Database.Db.Table<Discount>()
                .Where(d => d.Id == discountId)
                .FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[RoomWrapper] Error getting discount {discountId}: {ex}");
            return null;
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