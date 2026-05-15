using CatHotel.Models;
using CatHotel.Services;

namespace CatHotel.Views.CustomerViews;

public partial class CustomerEditPage : ContentPage, INavigationAware
{
    private readonly DatabaseService _db;
    private Customer _customer = new();

    public string CustomerName => _customer?.Name ?? "Customer";

    public CustomerEditPage()
    {
        InitializeComponent();
        _db = App.Database;
        BindingContext = this;
    }

    public async void OnNavigatedTo(IDictionary<string, object> parameters)
    {
        if (parameters.TryGetValue("customerId", out var customerIdObj) &&
            int.TryParse(customerIdObj.ToString(), out int customerId))
        {
            _customer = await _db.Db.Table<Customer>().FirstOrDefaultAsync(x => x.Id == customerId);
            if (_customer != null)
            {
                PopulateFields();
                OnPropertyChanged(nameof(CustomerName));
            }
        }
    }

    private void PopulateFields()
    {
        NameEntry.Text = _customer.Name;
        TelephoneEntry.Text = _customer.TelephoneNum;
        EmailEntry.Text = _customer.Email;
        LineIdEntry.Text = _customer.LineId;

        if (!string.IsNullOrEmpty(_customer.ImgUrl) && File.Exists(_customer.ImgUrl))
            CustomerPhotoPreview.Source = ImageSource.FromFile(_customer.ImgUrl);
        else if (!string.IsNullOrEmpty(_customer.ImgUrl))
            CustomerPhotoPreview.Source = _customer.ImgUrl;
    }

    private async void OnUploadImageTapped(object sender, TappedEventArgs e)
    {
        try
        {
            var result = await MediaPicker.Default.PickPhotoAsync();
            if (result != null)
            {
                string selectedImagePath = result.FullPath;
                CustomerPhotoPreview.Source = ImageSource.FromFile(result.FullPath);

                var fileName = $"customer_{Guid.NewGuid()}.jpg";
                var destPath = Path.Combine(FileSystem.AppDataDirectory, fileName);
                File.Copy(selectedImagePath, destPath, true);
                _customer.ImgUrl = destPath;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Cannot pick photo: {ex.Message}", "OK");
        }
    }

    private void OnRemoveImageTapped(object sender, TappedEventArgs e)
    {
        _customer.ImgUrl = string.Empty;
        CustomerPhotoPreview.Source = "placeholder_user.png";
    }

    private async void OnSaveTapped(object sender, TappedEventArgs e)
    {
        var name = NameEntry.Text?.Trim();
        var tel = TelephoneEntry.Text?.Trim();
        var email = EmailEntry.Text?.Trim();
        var lineId = LineIdEntry.Text?.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            await DisplayAlertAsync("Validation", "Please enter customer name.", "OK");
            return;
        }
        if (string.IsNullOrWhiteSpace(tel))
        {
            await DisplayAlertAsync("Validation", "Please enter telephone number.", "OK");
            return;
        }
        if (string.IsNullOrWhiteSpace(email))
        {
            await DisplayAlertAsync("Validation", "Please enter email.", "OK");
            return;
        }

        _customer.Name = name;
        _customer.TelephoneNum = tel;
        _customer.Email = email;
        _customer.LineId = lineId ?? string.Empty;

        try
        {
            await _db.Db.UpdateAsync(_customer);

            // Notify that customer was updated
            CustomerService.NotifyCustomerUpdated(_customer);

            await DisplayAlertAsync("Success", "Customer updated successfully!", "OK");
            await Navigation.PopAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("[CUSTOMER EDIT] Error updating customer: " + ex);
            await DisplayAlertAsync("Error", "Failed to update customer.", "OK");
        }
    }

    private async void OnDeleteCustomerClicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlertAsync("Delete",
            $"Delete customer '{_customer.Name}'? This cannot be undone.", "Delete", "Cancel");
        if (!confirm) return;

        try
        {
            await _db.Db.DeleteAsync(_customer);

            // Notify that customer was deleted
            CustomerService.NotifyCustomerDeleted(_customer);

            await DisplayAlertAsync("Deleted", "Customer profile deleted.", "OK");
            await Navigation.PopAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("[CUSTOMER EDIT] Error deleting customer: " + ex);
            await DisplayAlertAsync("Error", "Failed to delete customer.", "OK");
        }
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}