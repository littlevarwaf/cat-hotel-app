using CatHotel.Models;
using CatHotel.Services;

namespace CatHotel.Views.CustomerViews;

public partial class CustomerSelectView : ContentView
{
    private readonly DatabaseService _db = App.Database;
    private List<Customer> _allCustomers = new();
    private bool _isInitialized;

    public CustomerSelectView()
    {
        InitializeComponent();
        CustomerService.CustomerAdded += async (_, _) => await RefreshAsync();
        CustomerService.CustomerUpdated += async (_, _) => await RefreshAsync();
        CustomerService.CustomerDeleted += async (_, _) => await RefreshAsync();
        Loaded += OnViewLoaded;
    }

    private async void OnViewLoaded(object? sender, EventArgs e)
    {
        if (_isInitialized) return;
        _isInitialized = true;
        await RefreshAsync();
    }

    public async Task RefreshAsync()
    {
        await _db.InitializeAsync();
        _allCustomers = await _db.Db.Table<Customer>().OrderByDescending(c => c.CreatedAt).ToListAsync();
        CustomersCollection.ItemsSource = new List<Customer>(_allCustomers);
    }

    private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        var query = e.NewTextValue?.Trim().ToLower() ?? "";
        var filtered = string.IsNullOrWhiteSpace(query)
            ? _allCustomers
            : _allCustomers.Where(c =>
                c.Name.ToLower().Contains(query) ||
                c.TelephoneNum.ToLower().Contains(query) ||
                c.Email.ToLower().Contains(query)).ToList();
        CustomersCollection.ItemsSource = filtered;
    }

    private async void OnCustomerTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is not Customer customer)
            return;

        if (BookingDraftService.Instance.IsPickingCustomer)
        {
            BookingDraftService.Instance.SelectedCustomer = customer;
            BookingDraftService.Instance.EndCustomerPick();
            await NavigationService.GoBackAsync();
            return;
        }
    }

    private async void OnEditClicked(object? sender, EventArgs e)
    {
        if (sender is not Button btn || btn.CommandParameter is not Customer customer)
            return;

        await NavigationService.GoToAsync(
            NavigationService.CustomerEditPage,
            new Dictionary<string, object> { ["customerId"] = customer.Id });
    }
}
