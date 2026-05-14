using CatHotel.Models;
using CatHotel.Services;

namespace CatHotel.Views.DiscountSettingViews;

public partial class DiscountEditPage : ContentPage, INavigationAware
{
    private readonly DatabaseService _db;
    private Discount _discount;

    public string DiscountCode => _discount?.Code ?? "Discount";

    public DiscountEditPage()
    {
        InitializeComponent();
        _db = App.Database;
        BindingContext = this;
    }

    public async void OnNavigatedTo(IDictionary<string, object> parameters)
    {
        System.Diagnostics.Debug.WriteLine("[DISCOUNT EDIT] OnNavigatedTo called");

        if (parameters.TryGetValue("discountId", out var discountIdObj) && int.TryParse(discountIdObj.ToString(), out int discountId))
        {
            System.Diagnostics.Debug.WriteLine($"[DISCOUNT EDIT] Loading discount with ID: {discountId}");
            _discount = await _db.Db.Table<Discount>().FirstOrDefaultAsync(x => x.Id == discountId);
            if (_discount != null)
            {
                System.Diagnostics.Debug.WriteLine($"[DISCOUNT EDIT] Discount loaded: {_discount.Code}");
                PopulateFields();
                OnPropertyChanged(nameof(DiscountCode));
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[DISCOUNT EDIT] Discount not found with ID: {discountId}");
            }
        }
    }

    private void PopulateFields()
    {
        System.Diagnostics.Debug.WriteLine("[DISCOUNT EDIT] PopulateFields called");

        // Populate read-only fields
        DiscountCodeEntry.Text = _discount.Code;
        DiscountAmountEntry.Text = _discount.Amount.ToString();

        // Populate editable fields
        DiscountDescriptionEntry.Text = _discount.Description;
        DiscountQuantityEntry.Text = _discount.Quantity.ToString();
        UsedCountEntry.Text = _discount.UsedCount.ToString();
        ExpirationDatePicker.Date = _discount.ExpirationDate;

        System.Diagnostics.Debug.WriteLine("[DISCOUNT EDIT] All fields populated successfully");
    }

    private async void OnSaveTapped(object sender, TappedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("[DISCOUNT EDIT] OnSaveTapped called");

        var description = DiscountDescriptionEntry.Text?.Trim() ?? string.Empty;
        var quantityText = DiscountQuantityEntry.Text?.Trim();
        var usedCountText = UsedCountEntry.Text?.Trim();
        var expirationDate = ExpirationDatePicker.Date ?? DateTime.Now.Date;

        System.Diagnostics.Debug.WriteLine($"[DISCOUNT EDIT] Description: '{description}', Quantity: '{quantityText}', UsedCount: '{usedCountText}', ExpirationDate: '{expirationDate}'");

        // Validation - Quantity
        if (!int.TryParse(quantityText, out int quantity) || quantity < 0)
        {
            System.Diagnostics.Debug.WriteLine($"[DISCOUNT EDIT] Validation failed: Quantity invalid. Value: '{quantityText}'");
            await DisplayAlertAsync("Validation Error", "Please enter a valid quantity.", "OK");
            return;
        }

        // Validation - UsedCount
        if (!int.TryParse(usedCountText, out int usedCount) || usedCount < 0)
        {
            System.Diagnostics.Debug.WriteLine($"[DISCOUNT EDIT] Validation failed: UsedCount invalid. Value: '{usedCountText}'");
            await DisplayAlertAsync("Validation Error", "Please enter a valid used count.", "OK");
            return;
        }

        // Validation - UsedCount cannot exceed Quantity
        if (usedCount > quantity)
        {
            System.Diagnostics.Debug.WriteLine($"[DISCOUNT EDIT] Validation failed: UsedCount ({usedCount}) exceeds Quantity ({quantity})");
            await DisplayAlertAsync("Validation Error", "Used count cannot exceed quantity.", "OK");
            return;
        }

        // Validation - ExpirationDate
        if (expirationDate < DateTime.Now.Date)
        {
            System.Diagnostics.Debug.WriteLine("[DISCOUNT EDIT] Validation failed: Expiration date is in the past");
            await DisplayAlertAsync("Validation Error", "Expiration date must be today or in the future.", "OK");
            return;
        }

        System.Diagnostics.Debug.WriteLine("[DISCOUNT EDIT] Validation passed, updating discount...");

        // Update discount properties
        _discount.Description = description;
        _discount.Quantity = quantity;
        _discount.UsedCount = usedCount;
        _discount.ExpirationDate = expirationDate;

        try
        {
            System.Diagnostics.Debug.WriteLine("[DISCOUNT EDIT] Updating discount in database...");
            await _db.Db.UpdateAsync(_discount);
            System.Diagnostics.Debug.WriteLine($"[DISCOUNT EDIT] Discount updated successfully with ID: {_discount.Id}");

            // Notify that discount was updated
            DiscountService.NotifyDiscountUpdated(_discount);
            System.Diagnostics.Debug.WriteLine("[DISCOUNT EDIT] DiscountUpdated notification sent");

            // Show success message
            await DisplayAlertAsync("Success", $"Discount code '{_discount.Code}' has been updated successfully!", "OK");

            // Navigate back
            System.Diagnostics.Debug.WriteLine("[DISCOUNT EDIT] Navigating back...");
            await NavigationService.GoBackAsync();
            System.Diagnostics.Debug.WriteLine("[DISCOUNT EDIT] Navigation back completed");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DISCOUNT EDIT] Error updating discount: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[DISCOUNT EDIT] Stack trace: {ex.StackTrace}");
            await DisplayAlertAsync("Error", $"Failed to update discount: {ex.Message}", "OK");
        }
    }

    private async void OnDeleteDiscountClicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlertAsync("Delete",
            $"Delete discount code '{_discount.Code}'? This cannot be undone.", "Delete", "Cancel");
        if (!confirm) return;

        try
        {
            System.Diagnostics.Debug.WriteLine($"[DISCOUNT EDIT] Deleting discount with ID: {_discount.Id}");
            await _db.Db.DeleteAsync(_discount);
            System.Diagnostics.Debug.WriteLine($"[DISCOUNT EDIT] Discount deleted successfully");

            // Notify that discount was deleted
            DiscountService.NotifyDiscountDeleted(_discount);
            System.Diagnostics.Debug.WriteLine("[DISCOUNT EDIT] DiscountDeleted notification sent");

            // Navigate back
            System.Diagnostics.Debug.WriteLine("[DISCOUNT EDIT] Navigating back...");
            await NavigationService.GoBackAsync();
            System.Diagnostics.Debug.WriteLine("[DISCOUNT EDIT] Navigation back completed");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DISCOUNT EDIT] Error deleting discount: {ex.Message}");
            await DisplayAlertAsync("Error", $"Failed to delete discount: {ex.Message}", "OK");
        }
    }

    public void OnNavigatingFrom(IDictionary<string, object> parameters)
    {
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await NavigationService.GoBackAsync();
    }
}