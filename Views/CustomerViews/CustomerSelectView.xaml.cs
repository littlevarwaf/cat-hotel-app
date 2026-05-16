using CatHotel.Models;
using CatHotel.Services;
using CatHotel.ViewModels;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace CatHotel.Views.CustomerViews;

public partial class CustomerSelectView : ContentView
{
    private readonly ICustomerRepository _customerRepo;
    private readonly IBookingRepository _bookingRepo;
    private readonly BookingDraftService _draftService = BookingDraftService.Instance;
    private List<Customer> _allCustomers = new();
    private CustomerWrapperViewModel? _viewModel;
    private CancellationTokenSource? _searchCancellationTokenSource;
    private bool _isInitialized = false;

    public CustomerSelectView()
    {
        InitializeComponent();
        _customerRepo = IPlatformApplication.Current!.Services.GetRequiredService<ICustomerRepository>();
        _bookingRepo = IPlatformApplication.Current!.Services.GetRequiredService<IBookingRepository>();
        this.BindingContextChanged += OnBindingContextChanged;

        // Subscribe to customer events
        CustomerService.CustomerAdded += async (s, e) => await RefreshAsync();
        CustomerService.CustomerUpdated += async (s, e) => await RefreshAsync();
        CustomerService.CustomerDeleted += async (s, e) => await RefreshAsync();

        // Call initialization when the view is loaded
        this.Loaded += OnViewLoaded;
    }

    private async void OnViewLoaded(object sender, EventArgs e)
    {
        if (_isInitialized) return;
        _isInitialized = true;

        try
        {
            await LoadCustomersAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("[CUSTOMERS][FATAL] " + ex);
        }
    }

    // Public method to refresh data
    public async Task RefreshAsync()
    {
        if (_isInitialized)
        {
            await LoadCustomersAsync();
        }
    }

    private void OnBindingContextChanged(object? sender, EventArgs e)
    {
        if (BindingContext is CustomerWrapperViewModel vm)
        {
            _viewModel = vm;
            System.Diagnostics.Debug.WriteLine($"[CustomerSelectView] BindingContext set to CustomerWrapperViewModel");

            // Subscribe to customer collection changes
            if (_viewModel.Customers is INotifyCollectionChanged notifyCollection)
            {
                notifyCollection.CollectionChanged += OnCustomersCollectionChanged;
            }

            System.Diagnostics.Debug.WriteLine($"[CustomerSelectView] Initial _allCustomers count: {_allCustomers.Count}");
        }
    }

    private void OnCustomersCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Update _allCustomers when the ViewModel's collection changes
        if (_viewModel != null)
        {
            UpdateAllCustomers();
            System.Diagnostics.Debug.WriteLine($"[CustomerSelectView] Customers collection updated. Total: {_allCustomers.Count}");
        }
    }

    private async Task LoadCustomersAsync()
    {
        try
        {
            _allCustomers = await _customerRepo.GetAllCustomersAsync();
            if (_viewModel != null)
            {
                _viewModel.Customers = new ObservableCollection<Customer>(_allCustomers);
            }
            SearchEntry.Text = string.Empty;
            System.Diagnostics.Debug.WriteLine($"[CustomerSelectView] Loaded {_allCustomers.Count} customers");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CustomerSelectView] Error loading customers: {ex}");
        }
    }

    private void UpdateAllCustomers()
    {
        if (_viewModel != null)
        {
            _allCustomers = new List<Customer>(_viewModel.Customers);
            System.Diagnostics.Debug.WriteLine($"[CustomerSelectView] Updated _allCustomers with {_allCustomers.Count} customers");
        }
    }

    private async void OnEditButtonClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.BindingContext is Customer customer)
        {
            await NavigationService.GoToAsync("CustomerEditPage",
                new Dictionary<string, object> { ["customerId"] = customer.Id });
        }
    }

    private async void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        _searchCancellationTokenSource?.Cancel();
        _searchCancellationTokenSource = new CancellationTokenSource();

        try
        {
            await Task.Delay(300, _searchCancellationTokenSource.Token);

            var query = e.NewTextValue?.ToLower() ?? string.Empty;

            if (_viewModel == null)
            {
                System.Diagnostics.Debug.WriteLine($"[CustomerSelectView] ViewModel is null");
                return;
            }

            var filtered = string.IsNullOrWhiteSpace(query)
                ? _allCustomers
                : _allCustomers.Where(c =>
                    c.Name.ToLower().Contains(query) ||
                    c.Email.ToLower().Contains(query) ||
                    c.TelephoneNum.ToLower().Contains(query))
                    .ToList();

            _viewModel.Customers = new ObservableCollection<Customer>(filtered);
        }
        catch (OperationCanceledException)
        {
            System.Diagnostics.Debug.WriteLine($"[CustomerSelectView] Search cancelled");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CustomerSelectView] Error during search: {ex}");
        }
    }

    private async void OnCustomerSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is Customer selectedCustomer)
        {
            await SelectCustomerAsync(selectedCustomer);
        }
    }

    private async Task SelectCustomerAsync(Customer customer)
    {
        if (_viewModel == null)
            return;

        try
        {
            System.Diagnostics.Debug.WriteLine($"[CustomerSelectView] Customer selected: {customer.Name} (ID: {customer.Id})");

            // Mode 1 = BookingPage flow (save to BookingDraftService)
            if (_viewModel.Mode == 1)
            {
                System.Diagnostics.Debug.WriteLine($"[CustomerSelectView] Mode: BOOKING DRAFT (1) - Updating BookingDraftService");
                _draftService.SelectedCustomer = customer;
                System.Diagnostics.Debug.WriteLine($"[CustomerSelectView] BookingDraftService.SelectedCustomer set to {customer.Name} (ID: {customer.Id})");
            }
            // Mode 0 = RoomDetailPage flow (save to database)
            else if (_viewModel.Mode == 0 && _viewModel.BookingId > 0)
            {
                System.Diagnostics.Debug.WriteLine($"[CustomerSelectView] Mode: EXISTING BOOKING (0) - Updating database");
                var booking = await _bookingRepo.GetBookingByIdAsync(_viewModel.BookingId);
                if (booking != null)
                {
                    booking.CustomerId = customer.Id;
                    await _bookingRepo.UpdateBookingAsync(booking);
                    System.Diagnostics.Debug.WriteLine($"[CustomerSelectView] Booking {booking.Id} updated with customer {customer.Id}");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[CustomerSelectView] Warning: No valid mode detected (Mode={_viewModel.Mode}, BookingId={_viewModel.BookingId})");
            }

            SearchEntry.Text = string.Empty;
            await NavigationService.GoBackAsync();
            return;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CustomerSelectView] Error selecting customer: {ex}");
            await Application.Current!.MainPage!.DisplayAlertAsync("Error",
                $"Failed to select customer: {ex.Message}", "OK");
        }
        finally
        {
            CustomersCollectionView.SelectedItem = null;
        }
    }
}