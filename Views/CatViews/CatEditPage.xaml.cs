using CatHotel.Models;
using CatHotel.Services;

namespace CatHotel.Views.CatViews;

public partial class CatEditPage : ContentPage, INavigationAware
{
    private readonly ICatRepository _catRepo;
    private Cat? _cat;
    private string _selectedImagePath = string.Empty;
    private Gender _selectedGender = Gender.Unknown;

    public string CatName => _cat?.Name ?? "Cat";

    public CatEditPage()
    {
        InitializeComponent();
        _catRepo = IPlatformApplication.Current!.Services.GetRequiredService<ICatRepository>();
        BindingContext = this;
    }

    public async void OnNavigatedTo(IDictionary<string, object> parameters)
    {
        if (parameters.TryGetValue("catId", out var catIdObj) &&
            int.TryParse(catIdObj.ToString(), out int catId))
        {
            _cat = await _catRepo.GetCatByIdAsync(catId);
            if (_cat != null)
            {
                PopulateFields();
                OnPropertyChanged(nameof(CatName));
            }
        }
    }

    private void PopulateFields()
    {
        if (_cat == null) return;

        NameEntry.Text = _cat.Name;
        BreedEntry.Text = _cat.Breed;
        AgeEntry.Text = _cat.Age.ToString();

        // Set gender radio button
        _selectedGender = _cat.Gender;
        switch (_cat.Gender)
        {
            case Gender.Male:
                MaleGenderRadio.IsChecked = true;
                break;
            case Gender.Female:
                FemaleGenderRadio.IsChecked = true;
                break;
            default:
                UnknownGenderRadio.IsChecked = true;
                break;
        }

        // Load image
        if (!string.IsNullOrEmpty(_cat.ImgUrl) && File.Exists(_cat.ImgUrl))
            CatPhotoPreview.Source = ImageSource.FromFile(_cat.ImgUrl);
        else if (!string.IsNullOrEmpty(_cat.ImgUrl))
            CatPhotoPreview.Source = _cat.ImgUrl;
    }

    private async void OnUploadImageTapped(object sender, TappedEventArgs e)
    {
        try
        {
            var result = await MediaPicker.Default.PickPhotoAsync();
            if (result != null)
            {
                string selectedImagePath = result.FullPath;
                CatPhotoPreview.Source = ImageSource.FromFile(result.FullPath);

                var fileName = $"cat_{Guid.NewGuid()}.jpg";
                var destPath = Path.Combine(FileSystem.AppDataDirectory, fileName);
                File.Copy(selectedImagePath, destPath, true);

                if (_cat != null)
                    _cat.ImgUrl = destPath;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Cannot pick photo: {ex.Message}", "OK");
        }
    }

    private void OnRemoveImageTapped(object sender, TappedEventArgs e)
    {
        if (_cat != null)
            _cat.ImgUrl = string.Empty;
        CatPhotoPreview.Source = "placeholder_item.png";
    }

    private void OnGenderChanged(object sender, CheckedChangedEventArgs e)
    {
        if (sender is RadioButton rb && e.Value)
        {
            var genderValue = rb.Value?.ToString();
            _selectedGender = genderValue switch
            {
                "Male" => Gender.Male,
                "Female" => Gender.Female,
                _ => Gender.Unknown
            };
        }
    }

    private async void OnSaveTapped(object sender, TappedEventArgs e)
    {
        if (_cat == null) return;

        var name = NameEntry.Text?.Trim();
        var breed = BreedEntry.Text?.Trim();
        var ageText = AgeEntry.Text?.Trim();

        // Validation
        if (string.IsNullOrWhiteSpace(name))
        {
            await DisplayAlertAsync("Validation", "Please enter cat name.", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(breed))
        {
            await DisplayAlertAsync("Validation", "Please enter breed.", "OK");
            return;
        }

        if (!int.TryParse(ageText, out var age) || age < 0)
        {
            age = 0;
        }

        // Update cat properties
        _cat.Name = name;
        _cat.Breed = breed;
        _cat.Age = age;
        _cat.Gender = _selectedGender;

        // If no image was set, ensure it has a placeholder
        if (string.IsNullOrEmpty(_cat.ImgUrl))
        {
            _cat.ImgUrl = "placeholder_item.png";
        }

        try
        {
            await _catRepo.UpdateCatAsync(_cat);

            // Notify that cat was updated
            CatService.NotifyCatUpdated(_cat);

            System.Diagnostics.Debug.WriteLine($"[CatEditPage] Cat updated successfully: {_cat.Name}");

            await DisplayAlertAsync("Success", "Cat updated successfully!", "OK");
            await Navigation.PopAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CatEditPage] Error updating cat: {ex}");
            await DisplayAlertAsync("Error", "Failed to update cat.", "OK");
        }
    }

    private async void OnDeleteCatClicked(object sender, EventArgs e)
    {
        if (_cat == null) return;

        bool confirm = await DisplayAlertAsync("Delete",
            $"Delete cat '{_cat.Name}'? This cannot be undone.", "Delete", "Cancel");
        if (!confirm) return;

        try
        {
            await _catRepo.DeleteCatAsync(_cat);

            // Notify that cat was deleted
            CatService.NotifyCatDeleted(_cat);

            System.Diagnostics.Debug.WriteLine($"[CatEditPage] Cat deleted successfully: {_cat.Name}");

            await DisplayAlertAsync("Deleted", "Cat profile deleted.", "OK");
            await Navigation.PopAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CatEditPage] Error deleting cat: {ex}");
            await DisplayAlertAsync("Error", "Failed to delete cat.", "OK");
        }
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}