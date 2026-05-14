using CatHotel.Models;
using CatHotel.Services;

namespace CatHotel.Views.DiscountSettingViews;

public partial class DiscountAddView : ContentView
{
    private readonly DatabaseService _db;

    public DiscountAddView()
    {
        InitializeComponent();
        _db = App.Database;
        
        // Set default expiration date to today
        ExpirationDatePicker.Date = DateTime.Now.Date;
    }

    private async void OnAddDiscountTapped(object sender, TappedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("[DISCOUNT ADD] OnAddDiscountTapped called");

        var code = DiscountCodeEntry.Text?.Trim();
        var description = DiscountDescriptionEntry.Text?.Trim() ?? string.Empty;
        var amountText = DiscountAmountEntry.Text?.Trim();
        var quantityText = DiscountQuantityEntry.Text?.Trim();
        var expirationDate = ExpirationDatePicker.Date ?? DateTime.Now.Date;

        System.Diagnostics.Debug.WriteLine($"[DISCOUNT ADD] Code: '{code}', Description: '{description}', Amount: '{amountText}', Quantity: '{quantityText}', ExpirationDate: '{expirationDate}'");

        // Validation - Code
        if (string.IsNullOrWhiteSpace(code))
        {
            System.Diagnostics.Debug.WriteLine("[DISCOUNT ADD] Validation failed: Code is empty");
            await MainThread.InvokeOnMainThreadAsync(async () =>
                await Application.Current!.MainPage!.DisplayAlertAsync("Validation Error", "Please enter a discount code.", "OK"));
            return;
        }

        // Validation - Amount
        if (!int.TryParse(amountText, out int amount) || amount < 0)
        {
            System.Diagnostics.Debug.WriteLine($"[DISCOUNT ADD] Validation failed: Amount invalid. Value: '{amountText}'");
            await MainThread.InvokeOnMainThreadAsync(async () =>
                await Application.Current!.MainPage!.DisplayAlertAsync("Validation Error", "Please enter a valid discount amount.", "OK"));
            return;
        }

        // Validation - Quantity
        if (!int.TryParse(quantityText, out int quantity) || quantity < 0)
        {
            System.Diagnostics.Debug.WriteLine($"[DISCOUNT ADD] Validation failed: Quantity invalid. Value: '{quantityText}'");
            await MainThread.InvokeOnMainThreadAsync(async () =>
                await Application.Current!.MainPage!.DisplayAlertAsync("Validation Error", "Please enter a valid quantity.", "OK"));
            return;
        }

        // Validation - ExpirationDate
        if (expirationDate < DateTime.Now.Date)
        {
            System.Diagnostics.Debug.WriteLine("[DISCOUNT ADD] Validation failed: Expiration date is in the past");
            await MainThread.InvokeOnMainThreadAsync(async () =>
                await Application.Current!.MainPage!.DisplayAlertAsync("Validation Error", "Expiration date must be today or in the future.", "OK"));
            return;
        }

        // Check for duplicate discount code
        try
        {
            var existingDiscount = await _db.Db.Table<Discount>().FirstOrDefaultAsync(d =>
                d.Code.ToLower() == code.ToLower());

            if (existingDiscount != null)
            {
                System.Diagnostics.Debug.WriteLine("[DISCOUNT ADD] Validation failed: Discount code already exists");
                await MainThread.InvokeOnMainThreadAsync(async () =>
                    await Application.Current!.MainPage!.DisplayAlertAsync("Duplicate Code",
                        $"A discount code '{code}' already exists. Please use a different code.", "OK"));
                return;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DISCOUNT ADD] Error checking for duplicate discount: {ex.Message}");
            await MainThread.InvokeOnMainThreadAsync(async () =>
                await Application.Current!.MainPage!.DisplayAlertAsync("Database Error",
                    "Error checking for duplicate codes. Please try again.", "OK"));
            return;
        }

        System.Diagnostics.Debug.WriteLine("[DISCOUNT ADD] Validation passed, creating discount...");

        // Create and insert discount
        var discount = new Discount
        {
            Code = code,
            Description = description,
            Amount = amount,
            Quantity = quantity,
            ExpirationDate = expirationDate,
            UsedCount = 0,
            CreatedAt = DateTime.Now
        };

        try
        {
            System.Diagnostics.Debug.WriteLine("[DISCOUNT ADD] Inserting discount into database...");
            await _db.Db.InsertAsync(discount);
            System.Diagnostics.Debug.WriteLine($"[DISCOUNT ADD] Discount inserted successfully with ID: {discount.Id}");

            // Notify that discount was added
            DiscountService.NotifyDiscountAdded(discount);
            System.Diagnostics.Debug.WriteLine("[DISCOUNT ADD] DiscountAdded notification sent");

            // Show success message
            await MainThread.InvokeOnMainThreadAsync(async () =>
                await Application.Current!.MainPage!.DisplayAlertAsync("Success",
                    $"Discount code '{code}' has been added successfully!", "OK"));

            // Clear form after successful addition
            ClearForm();

            // Navigate back to wrapper page
            System.Diagnostics.Debug.WriteLine("[DISCOUNT ADD] Navigating back...");
            await NavigationService.GoBackAsync();
            System.Diagnostics.Debug.WriteLine("[DISCOUNT ADD] Navigation back completed");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DISCOUNT ADD] Error adding discount: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[DISCOUNT ADD] Stack trace: {ex.StackTrace}");
            await MainThread.InvokeOnMainThreadAsync(async () =>
                await Application.Current!.MainPage!.DisplayAlertAsync("Error",
                    $"Failed to add discount: {ex.Message}", "OK"));
        }
    }

    private void ClearForm()
    {
        DiscountCodeEntry.Text = string.Empty;
        DiscountDescriptionEntry.Text = string.Empty;
        DiscountAmountEntry.Text = string.Empty;
        DiscountQuantityEntry.Text = string.Empty;
        ExpirationDatePicker.Date = DateTime.Now.Date;
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await NavigationService.GoBackAsync();
    }
}