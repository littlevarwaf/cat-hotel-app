using CatHotel.Models;
using CatHotel.Services;

namespace CatHotel.Views.CustomerViews;

public partial class CustomerAddView : ContentView
{
    private readonly DatabaseService _db = App.Database;

    public CustomerAddView() => InitializeComponent();

    private async void OnSaveTapped(object? sender, TappedEventArgs e)
    {
        var name = NameEntry.Text?.Trim();
        var phone = PhoneEntry.Text?.Trim();
        var email = EmailEntry.Text?.Trim();

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(phone))
        {
            await Application.Current!.MainPage!.DisplayAlert("กรุณากรอกข้อมูล", "ชื่อและเบอร์โทรจำเป็นต้องกรอก", "OK");
            return;
        }

        var customer = new Customer
        {
            Name = name,
            TelephoneNum = phone,
            Email = string.IsNullOrWhiteSpace(email) ? "-" : email,
            ImgUrl = "placeholder_item.png",
            CreatedAt = DateTime.Now
        };

        await _db.InitializeAsync();
        await _db.Db.InsertAsync(customer);
        CustomerService.NotifyCustomerAdded(customer);

        if (BookingDraftService.Instance.IsPickingCustomer)
        {
            BookingDraftService.Instance.SelectedCustomer = customer;
            BookingDraftService.Instance.EndCustomerPick();
            await NavigationService.GoBackAsync();
            return;
        }

        NameEntry.Text = "";
        PhoneEntry.Text = "";
        EmailEntry.Text = "";
        await Application.Current!.MainPage!.DisplayAlert("สำเร็จ", "เพิ่มลูกค้าแล้ว", "OK");
    }
}
