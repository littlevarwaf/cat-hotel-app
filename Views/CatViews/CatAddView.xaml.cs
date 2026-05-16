using CatHotel.Models;
using CatHotel.Services;
using CatHotel.ViewModels;

namespace CatHotel.Views.CatViews;

public partial class CatAddView : ContentView
{
    private readonly ICatRepository _catRepo;
    private string _selectedImagePath = string.Empty;
    private Gender _selectedGender = Gender.Unknown;
    private CatWrapperViewModel? _viewModel;

    public CatAddView()
    {
        InitializeComponent();
        _catRepo = IPlatformApplication.Current!.Services.GetRequiredService<ICatRepository>();
        this.BindingContextChanged += OnBindingContextChanged;
    }

    private void OnBindingContextChanged(object? sender, EventArgs e)
    {
        if (BindingContext is CatWrapperViewModel vm)
        {
            _viewModel = vm;
            System.Diagnostics.Debug.WriteLine($"[CatAddView] BindingContext set to CatWrapperViewModel");
        }
    }

    private async void OnUploadImageTapped(object sender, TappedEventArgs e)
    {
        try
        {
            var result = await MediaPicker.Default.PickPhotoAsync();
            if (result != null)
            {
                _selectedImagePath = result.FullPath;
                CatPhotoPreview.Source = ImageSource.FromFile(result.FullPath);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CatAddView] Error picking photo: {ex}");
        }
    }

    private void OnRemoveImageTapped(object sender, TappedEventArgs e)
    {
        _selectedImagePath = string.Empty;
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
        var name = NameEntry.Text?.Trim();
        var breed = BreedEntry.Text?.Trim();
        var ageText = AgeEntry.Text?.Trim();

        // Validation
        if (string.IsNullOrWhiteSpace(name))
        {
            await Application.Current!.MainPage!.DisplayAlertAsync("Validation",
                "Please enter cat name.", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(breed))
        {
            await Application.Current!.MainPage!.DisplayAlertAsync("Validation",
                "Please enter breed.", "OK");
            return;
        }

        if (!int.TryParse(ageText, out var age) || age < 0)
        {
            age = 0;
        }

        try
        {
            string savedImgPath = string.Empty;

            // Handle image save
            if (!string.IsNullOrEmpty(_selectedImagePath))
            {
                var fileName = $"cat_{Guid.NewGuid()}.jpg";
                var destPath = Path.Combine(FileSystem.AppDataDirectory, fileName);
                File.Copy(_selectedImagePath, destPath, true);
                savedImgPath = destPath;
            }
            else
            {
                // Use placeholder if no image selected
                savedImgPath = "placeholder_item.png";
            }

            // Create and save cat
            var cat = new Cat
            {
                Name = name,
                Breed = breed,
                Age = age,
                Gender = _selectedGender,
                ImgUrl = savedImgPath,
                CreatedAt = DateTime.Now
            };

            await _catRepo.AddCatAsync(cat);

            System.Diagnostics.Debug.WriteLine($"[CatAddView] Cat added successfully: {cat.Name}");

            // Refresh cats list in view model
            if (_viewModel != null)
            {
                await _viewModel.RefreshCatsAsync();
            }

            // Clear form
            ClearForm();

            await Application.Current!.MainPage!.DisplayAlertAsync("Success",
                "Cat added successfully!", "OK");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CatAddView] Error adding cat: {ex}");
            await Application.Current!.MainPage!.DisplayAlertAsync("Error",
                $"Failed to add cat: {ex.Message}", "OK");
        }
    }

    private void ClearForm()
    {
        NameEntry.Text = string.Empty;
        BreedEntry.Text = string.Empty;
        AgeEntry.Text = string.Empty;
        UnknownGenderRadio.IsChecked = true;
        _selectedGender = Gender.Unknown;
        _selectedImagePath = string.Empty;
        CatPhotoPreview.Source = "placeholder_item.png";
    }
}