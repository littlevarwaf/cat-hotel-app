using CatHotel.Models;
using CatHotel.Services;

namespace CatHotel.Views.ShopSettingViews;

public partial class ShopItemAddView : ContentView
{
    private readonly DatabaseService _db;
    private string _selectedImagePath = string.Empty;
    private ItemStatus _itemStatus = ItemStatus.Unavailable;

    public ShopItemAddView()
    {
        InitializeComponent();
        _db = App.Database;
        PopulateItemTypes();
    }

    private void PopulateItemTypes()
    {
        foreach (var itemType in Enum.GetValues(typeof(ItemType)))
        {
            ItemTypePicker.Items.Add(itemType.ToString());
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
                ItemPhotoPreview.Source = ImageSource.FromFile(result.FullPath);
            }
        }
        catch (Exception ex)
        {
            //await DisplayAlert("Error", $"Cannot pick photo: {ex.Message}", "OK");
        }
    }

    private void OnRemoveImageTapped(object sender, TappedEventArgs e)
    {
        _selectedImagePath = string.Empty;
        ItemPhotoPreview.Source = "placeholder_item.png";
    }

    private void OnAvailabilityChanged(object sender, CheckedChangedEventArgs e)
    {
        if (sender is RadioButton rb && e.Value)
        {
            _itemStatus = rb.Value?.ToString() == "Available" 
                ? ItemStatus.Available 
                : ItemStatus.Unavailable;
        }
    }

    private async void OnAddItemTapped(object sender, TappedEventArgs e)
    {
        var name = NameEntry.Text?.Trim();
        var description = DescriptionEntry.Text?.Trim();
        var priceText = PriceEntry.Text?.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            //await DisplayAlert("Validation", "Please enter item name.", "OK");
            return;
        }
        if (ItemTypePicker.SelectedIndex < 0)
        {
            //await DisplayAlert("Validation", "Please select item type.", "OK");
            return;
        }
        if (!double.TryParse(priceText, out double price) || price < 0)
        {
            //await DisplayAlert("Validation", "Please enter a valid price.", "OK");
            return;
        }

        var itemType = (ItemType)Enum.Parse(typeof(ItemType), ItemTypePicker.Items[ItemTypePicker.SelectedIndex]);

        string savedImgPath = string.Empty;
        if (!string.IsNullOrEmpty(_selectedImagePath))
        {
            var fileName = $"item_{Guid.NewGuid()}.jpg";
            var destPath = Path.Combine(FileSystem.AppDataDirectory, fileName);
            File.Copy(_selectedImagePath, destPath, true);
            savedImgPath = destPath;
        }

        var item = new ShopItem
        {
            Name = name,
            Description = description ?? string.Empty,
            ItemPrice = price,
            ItemType = itemType,
            ItemStatus = _itemStatus,
            ImgUrl = savedImgPath,
            CreatedAt = DateTime.Now
        };

        await _db.Db.InsertAsync(item);
        //await DisplayAlert("Success", "Shop item added successfully!", "OK");
        
        // Clear form after successful addition
        ClearForm();
    }

    private void ClearForm()
    {
        NameEntry.Text = string.Empty;
        DescriptionEntry.Text = string.Empty;
        PriceEntry.Text = string.Empty;
        ItemTypePicker.SelectedIndex = -1;
        _selectedImagePath = string.Empty;
        ItemPhotoPreview.Source = "placeholder_item.png";
        UnavailableRadio.IsChecked = true;
        _itemStatus = ItemStatus.Unavailable;
    }

    private async void OnEditRoomTabTapped(object sender, TappedEventArgs e)
    {
        await NavigationService.GoBackAsync();
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await NavigationService.GoBackAsync();
    }
}