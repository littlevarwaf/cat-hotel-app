using CatHotel.Models;
using CatHotel.Services;

namespace CatHotel.Views.ShopSettingViews;

public partial class ShopItemEditPage : ContentPage, INavigationAware
{
    private readonly DatabaseService _db;
    private ShopItem _item;
    private string _selectedImagePath = string.Empty;
    private ItemStatus _itemStatus = ItemStatus.Unavailable;

    public string ItemName => _item?.Name ?? "Item";

    public ShopItemEditPage()
    {
        InitializeComponent();
        _db = App.Database;
        BindingContext = this;
    }

    public async void OnNavigatedTo(IDictionary<string, object> parameters)
    {
        if (parameters.TryGetValue("itemId", out var itemIdObj) && int.TryParse(itemIdObj.ToString(), out int itemId))
        {
            _item = await _db.Db.Table<ShopItem>().FirstOrDefaultAsync(x => x.Id == itemId);
            if (_item != null)
            {
                _itemStatus = _item.ItemStatus;
                PopulateItemTypes();
                PopulateFields();
                OnPropertyChanged(nameof(ItemName));
            }
        }
    }

    private void PopulateItemTypes()
    {
        foreach (var itemType in Enum.GetValues(typeof(ItemType)))
        {
            ItemTypePicker.Items.Add(itemType.ToString());
        }
    }

    private void PopulateFields()
    {
        NameEntry.Text = _item.Name;
        DescriptionEntry.Text = _item.Description;
        PriceEntry.Text = _item.ItemPrice.ToString("0.##");

        var typeName = _item.ItemType.ToString();
        for (int i = 0; i < ItemTypePicker.Items.Count; i++)
        {
            if (ItemTypePicker.Items[i] == typeName)
            {
                ItemTypePicker.SelectedIndex = i;
                break;
            }
        }

        // Set availability radio buttons based on ItemStatus
        if (_item.ItemStatus == ItemStatus.Available)
        {
            AvailableRadio.IsChecked = true;
        }
        else
        {
            UnavailableRadio.IsChecked = true;
        }

        if (!string.IsNullOrEmpty(_item.ImgUrl) && File.Exists(_item.ImgUrl))
            ItemPhotoPreview.Source = ImageSource.FromFile(_item.ImgUrl);
        else if (!string.IsNullOrEmpty(_item.ImgUrl))
            ItemPhotoPreview.Source = _item.ImgUrl;
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
            await DisplayAlertAsync("Error", $"Cannot pick photo: {ex.Message}", "OK");
        }
    }

    private void OnRemoveImageTapped(object sender, TappedEventArgs e)
    {
        _selectedImagePath = string.Empty;
        _item.ImgUrl = string.Empty;
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

    private async void OnSaveTapped(object sender, TappedEventArgs e)
    {
        var name = NameEntry.Text?.Trim();
        var description = DescriptionEntry.Text?.Trim();
        var priceText = PriceEntry.Text?.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            await DisplayAlertAsync("Validation", "Please enter item name.", "OK");
            return;
        }
        if (ItemTypePicker.SelectedIndex < 0)
        {
            await DisplayAlertAsync("Validation", "Please select item type.", "OK");
            return;
        }
        if (!double.TryParse(priceText, out double price) || price < 0)
        {
            await DisplayAlertAsync("Validation", "Please enter a valid price.", "OK");
            return;
        }

        if (!string.IsNullOrEmpty(_selectedImagePath))
        {
            var fileName = $"item_{Guid.NewGuid()}.jpg";
            var destPath = Path.Combine(FileSystem.AppDataDirectory, fileName);
            File.Copy(_selectedImagePath, destPath, true);
            _item.ImgUrl = destPath;
        }

        _item.Name = name;
        _item.Description = description ?? string.Empty;
        _item.ItemPrice = price;
        _item.ItemType = (ItemType)Enum.Parse(typeof(ItemType), ItemTypePicker.Items[ItemTypePicker.SelectedIndex]);
        _item.ItemStatus = _itemStatus;

        await _db.Db.UpdateAsync(_item);
        await DisplayAlertAsync("Success", "Shop item updated successfully!", "OK");
        await Navigation.PopAsync();
    }

    private async void OnDeleteItemClicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlertAsync("Delete",
            $"Delete '{_item.Name}'? This cannot be undone.", "Delete", "Cancel");
        if (!confirm) return;

        await _db.Db.DeleteAsync(_item);
        await DisplayAlertAsync("Deleted", "Shop item deleted.", "OK");
        await Navigation.PopAsync();
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}