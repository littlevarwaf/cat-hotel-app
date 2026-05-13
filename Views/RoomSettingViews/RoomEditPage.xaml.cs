using CatHotel.Models;
using CatHotel.Services;

namespace CatHotel.Views.RoomSettingViews;

public partial class RoomEditPage : ContentPage, INavigationAware
{
    private readonly DatabaseService _db;
    private Room _room;
    private string _selectedImagePath = string.Empty;
    private RoomTypes _selectedRoomType = RoomTypes.Small;
    private RoomStatus _selectedRoomStatus = RoomStatus.Available;

    public string RoomName => _room?.Name ?? "Room";

    public RoomEditPage()
    {
        InitializeComponent();
        _db = App.Database;
        BindingContext = this;
    }

    public async void OnNavigatedTo(IDictionary<string, object> parameters)
    {
        if (parameters.TryGetValue("roomId", out var roomIdObj) && int.TryParse(roomIdObj.ToString(), out int roomId))
        {
            _room = await _db.Db.Table<Room>().FirstOrDefaultAsync(x => x.Id == roomId);
            if (_room != null)
            {
                PopulateRoomStatusPicker();
                PopulateFields();
                OnPropertyChanged(nameof(RoomName));
            }
        }
    }

    private void PopulateRoomStatusPicker()
    {
        foreach (var status in Enum.GetValues(typeof(RoomStatus)))
        {
            RoomStatusPicker.Items.Add(status.ToString());
        }
    }

    private void PopulateFields()
    {
        // Populate text entries
        RoomNameEntry.Text = _room.Name;
        MaxOccupantsEntry.Text = _room.MaxOccupants.ToString();
        BasePriceEntry.Text = _room.BasePrice.ToString("0.##");

        // Populate Room Type radio buttons
        _selectedRoomType = _room.RoomType;
        SetRoomTypeRadioButton();

        // Populate Room Status picker
        var statusName = _room.Status.ToString();
        for (int i = 0; i < RoomStatusPicker.Items.Count; i++)
        {
            if (RoomStatusPicker.Items[i] == statusName)
            {
                RoomStatusPicker.SelectedIndex = i;
                break;
            }
        }
        _selectedRoomStatus = _room.Status;

        // Populate image
        if (!string.IsNullOrEmpty(_room.ImgUrl) && File.Exists(_room.ImgUrl))
            RoomPhotoPreview.Source = ImageSource.FromFile(_room.ImgUrl);
        else if (!string.IsNullOrEmpty(_room.ImgUrl))
            RoomPhotoPreview.Source = _room.ImgUrl;
    }

    private void SetRoomTypeRadioButton()
    {
        SmallRadio.IsChecked = _selectedRoomType == RoomTypes.Small;
        MediumRadio.IsChecked = _selectedRoomType == RoomTypes.Medium;
        LargeRadio.IsChecked = _selectedRoomType == RoomTypes.Large;
    }

    private async void OnUploadImageTapped(object sender, TappedEventArgs e)
    {
        try
        {
            var result = await MediaPicker.Default.PickPhotoAsync();
            if (result != null)
            {
                _selectedImagePath = result.FullPath;
                RoomPhotoPreview.Source = ImageSource.FromFile(result.FullPath);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("[ROOM EDIT] Error picking photo: " + ex);
        }
    }

    private void OnRemoveImageTapped(object sender, TappedEventArgs e)
    {
        _selectedImagePath = string.Empty;
        _room.ImgUrl = string.Empty;
        RoomPhotoPreview.Source = "placeholder_item.png";
    }

    private void OnRoomTypeChanged(object sender, CheckedChangedEventArgs e)
    {
        if (sender is RadioButton rb && e.Value)
        {
            _selectedRoomType = (RoomTypes)Enum.Parse(typeof(RoomTypes), rb.Value?.ToString() ?? "Small");
        }
    }

    private async void OnSaveTapped(object sender, TappedEventArgs e)
    {
        var name = RoomNameEntry.Text?.Trim();
        var maxOccupantsText = MaxOccupantsEntry.Text?.Trim();
        var priceText = BasePriceEntry.Text?.Trim();

        // Validation
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }
        if (!int.TryParse(maxOccupantsText, out int maxOccupants) || maxOccupants < 0 || maxOccupants > 99)
        {
            return;
        }
        if (!double.TryParse(priceText, out double price) || price < 0)
        {
            return;
        }
        if (RoomStatusPicker.SelectedIndex < 0)
        {
            return;
        }

        _selectedRoomStatus = (RoomStatus)Enum.Parse(typeof(RoomStatus), RoomStatusPicker.Items[RoomStatusPicker.SelectedIndex]);

        // Handle image
        if (!string.IsNullOrEmpty(_selectedImagePath))
        {
            var fileName = $"room_{Guid.NewGuid()}.jpg";
            var destPath = Path.Combine(FileSystem.AppDataDirectory, fileName);
            File.Copy(_selectedImagePath, destPath, true);
            _room.ImgUrl = destPath;
        }

        // Update room properties
        _room.Name = name;
        _room.Status = _selectedRoomStatus;
        _room.RoomType = _selectedRoomType;
        _room.MaxOccupants = maxOccupants;
        _room.BasePrice = price;

        try
        {
            await _db.Db.UpdateAsync(_room);

            // Notify that room was updated
            RoomService.NotifyRoomUpdated(_room);

            // Navigate back
            await NavigationService.GoBackAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("[ROOM EDIT] Error updating room: " + ex);
        }
    }

    private async void OnDeleteRoomClicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Delete",
            $"Delete '{_room.Name}'? This cannot be undone.", "Delete", "Cancel");
        if (!confirm) return;

        try
        {
            await _db.Db.DeleteAsync(_room);

            // Notify that room was deleted
            RoomService.NotifyRoomDeleted(_room);

            // Navigate back
            await NavigationService.GoBackAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("[ROOM EDIT] Error deleting room: " + ex);
        }
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await NavigationService.GoBackAsync();
    }
}