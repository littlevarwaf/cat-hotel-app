using CatHotel.Models;
using CatHotel.Services;

namespace CatHotel.Views.CustomerViews;

public partial class CustomerEditPage : ContentPage, INavigationAware
{
    private readonly DatabaseService _db = App.Database;
    private Customer? _customer;

    public CustomerEditPage() => InitializeComponent();

    public async void OnNavigatedTo(IDictionary<string, object> parameters)
    {
        if (!parameters.TryGetValue("customerId", out var idObj) || idObj is not int customerId)
            return;

        await _db.InitializeAsync();
        _customer = await _db.Db.Table<Customer>().FirstOrDefaultAsync(c => c.Id == customerId);
        if (_customer == null)
            return;

        NameEntry.Text = _customer.Name;
        PhoneEntry.Text = _customer.TelephoneNum;
        EmailEntry.Text = _customer.Email;
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        if (_customer == null)
            return;

        var name = NameEntry.Text?.Trim();
        var phone = PhoneEntry.Text?.Trim();
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(phone))
        {
            await DisplayAlert("กรุณากรอกข้อมูล", "ชื่อและเบอร์โทรจำเป็นต้องกรอก", "OK");
            return;
        }

        _customer.Name = name;
        _customer.TelephoneNum = phone;
        _customer.Email = EmailEntry.Text?.Trim() ?? "-";

        await _db.Db.UpdateAsync(_customer);
        CustomerService.NotifyCustomerUpdated(_customer);
        await NavigationService.GoBackAsync();
    }

    private async void OnBackClicked(object? sender, EventArgs e)
        => await NavigationService.GoBackAsync();
}
