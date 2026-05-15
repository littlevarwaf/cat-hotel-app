using CatHotel.Models;
using CatHotel.Services;

namespace CatHotel.Views.CatViews;

public partial class CatEditPage : ContentPage, INavigationAware
{
    private readonly DatabaseService _db = App.Database;
    private Cat? _cat;

    public CatEditPage() => InitializeComponent();

    public async void OnNavigatedTo(IDictionary<string, object> parameters)
    {
        if (!parameters.TryGetValue("catId", out var idObj) || idObj is not int catId)
            return;

        await _db.InitializeAsync();
        _cat = await _db.Db.Table<Cat>().FirstOrDefaultAsync(c => c.Id == catId);
        if (_cat == null) return;

        NameEntry.Text = _cat.Name;
        BreedEntry.Text = _cat.Breed;
        AgeEntry.Text = _cat.Age.ToString();
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        if (_cat == null) return;

        var name = NameEntry.Text?.Trim();
        var breed = BreedEntry.Text?.Trim();
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(breed))
        {
            await DisplayAlert("กรุณากรอกข้อมูล", "ชื่อและพันธุ์จำเป็นต้องกรอก", "OK");
            return;
        }

        _cat.Name = name;
        _cat.Breed = breed;
        if (int.TryParse(AgeEntry.Text?.Trim(), out var age))
            _cat.Age = age;

        await _db.Db.UpdateAsync(_cat);
        CatService.NotifyCatUpdated(_cat);
        await NavigationService.GoBackAsync();
    }

    private async void OnBackClicked(object? sender, EventArgs e)
        => await NavigationService.GoBackAsync();
}
