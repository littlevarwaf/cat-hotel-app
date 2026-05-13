using CatHotel.Models;
using CatHotel.Services;

namespace CatHotel.Views.RoomSettingViews;

public partial class RoomAddView : ContentView
{
    private readonly DatabaseService _db;
    private string _selectedImagePath = string.Empty;
    private RoomTypes _selectedRoomType = RoomTypes.Small;
    private RoomStatus _selectedRoomStatus = RoomStatus.Available;

    public RoomAddView()
    {
        InitializeComponent();
        _db = App.Database;
        PopulateRoomStatusPicker();
    }

    private void PopulateRoomStatusPicker()
    {
        foreach (var status in Enum.GetValues(typeof(RoomStatus)))
        {
            RoomStatusPicker.Items.Add(status.ToString());
        }
        RoomStatusPicker.SelectedIndex = 0;
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
            System.Diagnostics.Debug.WriteLine("[ROOM ADD] Error picking photo: " + ex);
        }
    }

    private void OnRemoveImageTapped(object sender, TappedEventArgs e)
    {
        _selectedImagePath = string.Empty;
        RoomPhotoPreview.Source = "placeholder_item.png";
    }

    private void OnRoomTypeChanged(object sender, CheckedChangedEventArgs e)
    {
        if (sender is RadioButton rb && e.Value)
        {
            _selectedRoomType = (RoomTypes)Enum.Parse(typeof(RoomTypes), rb.Value?.ToString() ?? "Small");
        }
    }

    private async void OnAddRoomTapped(object sender, TappedEventArgs e)
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
        string savedImgPath = string.Empty;
        if (!string.IsNullOrEmpty(_selectedImagePath))
        {
            var fileName = $"room_{Guid.NewGuid()}.jpg";
            var destPath = Path.Combine(FileSystem.AppDataDirectory, fileName);
            File.Copy(_selectedImagePath, destPath, true);
            savedImgPath = destPath;
        }
        else
        {
            savedImgPath = "placeholder_item.png";
        }

        // Create and insert room
        var room = new Room
        {
            Name = name,
            Status = _selectedRoomStatus,
            RoomType = _selectedRoomType,
            MaxOccupants = maxOccupants,
            BasePrice = price,
            StartDate = DateTime.Now,
            EndDate = DateTime.Now.AddDays(1),
            ImgUrl = savedImgPath
        };

        try
        {
            await _db.Db.InsertAsync(room);

            // Notify that room was added
            RoomService.NotifyRoomAdded(room);

            // Navigate back
            await NavigationService.GoBackAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("[ROOM ADD] Error adding room: " + ex);
        }
    }

    private void ClearForm()
    {
        RoomNameEntry.Text = string.Empty;
        MaxOccupantsEntry.Text = string.Empty;
        BasePriceEntry.Text = string.Empty;
        RoomStatusPicker.SelectedIndex = 0;
        SmallRadio.IsChecked = true;
        _selectedImagePath = string.Empty;
        RoomPhotoPreview.Source = "placeholder_item.png";
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await NavigationService.GoBackAsync();
    }
}