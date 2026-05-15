using CatHotel.Models;
using CatHotel.Services;
using CatHotel.ViewModels;

namespace CatHotel.Views.CustomerViews;

public partial class CustomerAddView : ContentView
{
    private readonly ICustomerRepository _customerRepo;
    private readonly DatabaseService _db;
    private string _selectedImagePath = string.Empty;
    private CustomerWrapperViewModel? _viewModel;

    public CustomerAddView()
    {
        InitializeComponent();
        _customerRepo = IPlatformApplication.Current!.Services.GetRequiredService<ICustomerRepository>();
        _db = App.Database;
        this.BindingContextChanged += OnBindingContextChanged;
    }

    private void OnBindingContextChanged(object? sender, EventArgs e)
    {
        if (BindingContext is CustomerWrapperViewModel vm)
        {
            _viewModel = vm;
            System.Diagnostics.Debug.WriteLine($"[CustomerAddView] BindingContext set to CustomerWrapperViewModel");
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
                CustomerPhotoPreview.Source = ImageSource.FromFile(result.FullPath);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CustomerAddView] Error picking photo: {ex}");
        }
    }

    private void OnRemoveImageTapped(object sender, TappedEventArgs e)
    {
        _selectedImagePath = string.Empty;
        CustomerPhotoPreview.Source = "placeholder_user.png";
    }

    private async void OnAddCustomerClicked(object sender, EventArgs e)
    {
        var name = NameEntry.Text?.Trim();
        var telephone = TelephoneEntry.Text?.Trim();
        var email = EmailEntry.Text?.Trim();
        var lineid = LineIdEntry.Text?.Trim();

        // Validation
        if (string.IsNullOrWhiteSpace(name))
        {
            await Application.Current!.MainPage!.DisplayAlertAsync("Validation",
                "Please enter customer name.", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(telephone))
        {
            await Application.Current!.MainPage!.DisplayAlertAsync("Validation",
                "Please enter telephone number.", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            await Application.Current!.MainPage!.DisplayAlertAsync("Validation",
                "Please enter email address.", "OK");
            return;
        }

        try
        {
            string savedImgPath = string.Empty;

            // Handle image save
            if (!string.IsNullOrEmpty(_selectedImagePath))
            {
                var fileName = $"customer_{Guid.NewGuid()}.jpg";
                var destPath = Path.Combine(FileSystem.AppDataDirectory, fileName);
                File.Copy(_selectedImagePath, destPath, true);
                savedImgPath = destPath;
            }
            else
            {
                // Use placeholder if no image selected
                savedImgPath = "placeholder_user.png";
            }

            if (string.IsNullOrEmpty(lineid))
            {
                lineid = "-";
            }

            // Create and save customer
            var customer = new Customer(name, telephone, email, lineid, savedImgPath);
            await _customerRepo.AddCustomerAsync(customer);

            System.Diagnostics.Debug.WriteLine($"[CustomerAddView] Customer added successfully: {customer.Name}");

            // Refresh customers list in view model
            if (_viewModel != null)
            {
                await _viewModel.RefreshCustomersAsync();
            }

            // Clear form
            ClearForm();

            await Application.Current!.MainPage!.DisplayAlertAsync("Success",
                "Customer added successfully!", "OK");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CustomerAddView] Error adding customer: {ex}");
            await Application.Current!.MainPage!.DisplayAlertAsync("Error",
                $"Failed to add customer: {ex.Message}", "OK");
        }
    }

    private void ClearForm()
    {
        NameEntry.Text = string.Empty;
        TelephoneEntry.Text = string.Empty;
        EmailEntry.Text = string.Empty;
        LineIdEntry.Text = string.Empty;
        _selectedImagePath = string.Empty;
        CustomerPhotoPreview.Source = "placeholder_user.png";
    }
}