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
        System.Diagnostics.Debug.WriteLine("[ROOM ADD] OnAddRoomTapped called");

        var name = RoomNameEntry.Text?.Trim();
        var maxOccupantsText = MaxOccupantsEntry.Text?.Trim();
        var priceText = BasePriceEntry.Text?.Trim();

        System.Diagnostics.Debug.WriteLine($"[ROOM ADD] Name: '{name}', MaxOcc: '{maxOccupantsText}', Price: '{priceText}'");

        // Validation - Name
        if (string.IsNullOrWhiteSpace(name))
        {
            System.Diagnostics.Debug.WriteLine("[ROOM ADD] Validation failed: Name is empty");
            await MainThread.InvokeOnMainThreadAsync(async () =>
                await Application.Current!.MainPage!.DisplayAlertAsync("Validation Error", "Please enter a room name.", "OK"));
            return;
        }

        // Validation - Max Occupants
        if (!int.TryParse(maxOccupantsText, out int maxOccupants) || maxOccupants < 0 || maxOccupants > 99)
        {
            System.Diagnostics.Debug.WriteLine($"[ROOM ADD] Validation failed: MaxOccupants invalid. Value: '{maxOccupantsText}'");
            await MainThread.InvokeOnMainThreadAsync(async () =>
                await Application.Current!.MainPage!.DisplayAlertAsync("Validation Error", "Please enter a valid number of occupants (0-99).", "OK"));
            return;
        }

        // Validation - Price
        if (!double.TryParse(priceText, out double price) || price < 0)
        {
            System.Diagnostics.Debug.WriteLine($"[ROOM ADD] Validation failed: Price invalid. Value: '{priceText}'");
            await MainThread.InvokeOnMainThreadAsync(async () =>
                await Application.Current!.MainPage!.DisplayAlertAsync("Validation Error", "Please enter a valid price.", "OK"));
            return;
        }

        // Validation - Room Status Picker
        if (RoomStatusPicker.SelectedIndex < 0)
        {
            System.Diagnostics.Debug.WriteLine("[ROOM ADD] Validation failed: RoomStatusPicker not selected");
            await MainThread.InvokeOnMainThreadAsync(async () =>
                await Application.Current!.MainPage!.DisplayAlertAsync("Validation Error", "Please select a room status.", "OK"));
            return;
        }

        // Check for duplicate room name + room type combination
        try
        {
            var existingRoom = await _db.Db.Table<Room>().FirstOrDefaultAsync(r =>
                r.Name.ToLower() == name.ToLower() && r.RoomType == _selectedRoomType);

            if (existingRoom != null)
            {
                System.Diagnostics.Debug.WriteLine("[ROOM ADD] Validation failed: Room with same name and type already exists");
                await MainThread.InvokeOnMainThreadAsync(async () =>
                    await Application.Current!.MainPage!.DisplayAlertAsync("Duplicate Room",
                        $"A room named '{name}' with type '{_selectedRoomType}' already exists. Please use a different name or type.", "OK"));
                return;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ROOM ADD] Error checking for duplicate room: {ex.Message}");
            await MainThread.InvokeOnMainThreadAsync(async () =>
                await Application.Current!.MainPage!.DisplayAlertAsync("Database Error",
                    "Error checking for duplicate rooms. Please try again.", "OK"));
            return;
        }

        System.Diagnostics.Debug.WriteLine("[ROOM ADD] Validation passed, creating room...");

        _selectedRoomStatus = (RoomStatus)Enum.Parse(typeof(RoomStatus), RoomStatusPicker.Items[RoomStatusPicker.SelectedIndex]);

        // Handle image
        string savedImgPath = string.Empty;
        if (!string.IsNullOrEmpty(_selectedImagePath))
        {
            try
            {
                var fileName = $"room_{Guid.NewGuid()}.jpg";
                var destPath = Path.Combine(FileSystem.AppDataDirectory, fileName);
                File.Copy(_selectedImagePath, destPath, true);
                savedImgPath = destPath;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ROOM ADD] Error saving image: {ex.Message}");
                savedImgPath = "placeholder_item.png";
            }
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
            System.Diagnostics.Debug.WriteLine("[ROOM ADD] Inserting room into database...");
            await _db.Db.InsertAsync(room);
            System.Diagnostics.Debug.WriteLine($"[ROOM ADD] Room inserted successfully with ID: {room.Id}");

            // Notify that room was added
            RoomService.NotifyRoomAdded(room);
            System.Diagnostics.Debug.WriteLine("[ROOM ADD] RoomAdded notification sent");

            // Show success message
            await MainThread.InvokeOnMainThreadAsync(async () =>
                await Application.Current!.MainPage!.DisplayAlertAsync("Success", 
                    $"Room '{name}' has been added successfully!", "OK"));

            // Clear form after successful addition
            ClearForm();

            // Navigate back to wrapper page
            System.Diagnostics.Debug.WriteLine("[ROOM ADD] Navigating back...");
            await NavigationService.GoBackAsync();
            System.Diagnostics.Debug.WriteLine("[ROOM ADD] Navigation back completed");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ROOM ADD] Error adding room: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[ROOM ADD] Stack trace: {ex.StackTrace}");
            await MainThread.InvokeOnMainThreadAsync(async () =>
                await Application.Current!.MainPage!.DisplayAlertAsync("Error", 
                    $"Failed to add room: {ex.Message}", "OK"));
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