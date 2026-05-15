using CatHotel.Models;
using CatHotel.Services;

namespace CatHotel.Views.CatViews;

public partial class CatAddView : ContentView
{
    private readonly DatabaseService _db = App.Database;

    public CatAddView()
    {
        InitializeComponent();
        GenderPicker.SelectedIndex = 0;
    }

    private async void OnSaveTapped(object? sender, TappedEventArgs e)
    {
        var customer = BookingDraftService.Instance.SelectedCustomer;
        if (customer == null)
        {
            await Application.Current!.MainPage!.DisplayAlert("เลือกลูกค้าก่อน", "กรุณาเลือกลูกค้าก่อนเพิ่มแมว", "OK");
            return;
        }

        var name = NameEntry.Text?.Trim();
        var breed = BreedEntry.Text?.Trim();
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(breed))
        {
            await Application.Current!.MainPage!.DisplayAlert("กรุณากรอกข้อมูล", "ชื่อและพันธุ์จำเป็นต้องกรอก", "OK");
            return;
        }

        if (!int.TryParse(AgeEntry.Text?.Trim(), out var age) || age < 0)
            age = 0;

        var gender = GenderPicker.SelectedIndex switch
        {
            1 => Gender.Male,
            2 => Gender.Female,
            _ => Gender.Unknown
        };

        var cat = new Cat
        {
            CustomerId = customer.Id,
            Name = name,
            Breed = breed,
            Age = age,
            Gender = gender,
            ImgUrl = "placeholder_item.png",
            CreatedAt = DateTime.Now
        };

        await _db.InitializeAsync();
        await _db.Db.InsertAsync(cat);
        CatService.NotifyCatAdded(cat);

        NameEntry.Text = "";
        BreedEntry.Text = "";
        AgeEntry.Text = "";
        GenderPicker.SelectedIndex = 0;

        await Application.Current!.MainPage!.DisplayAlert("สำเร็จ", "เพิ่มแมวแล้ว", "OK");
    }
}
