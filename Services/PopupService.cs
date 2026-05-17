using CatHotel.Models;

namespace CatHotel.Services;

public class PopupService
{
    private static PopupService? _instance;
    public static PopupService Instance => _instance ??= new PopupService();

    public event EventHandler<PopupEventArgs>? ShowPopupRequested;
    public event EventHandler? HidePopupRequested;

    public void ShowPopup(PopupEventArgs args) => ShowPopupRequested?.Invoke(this, args);
    public void HidePopup() => HidePopupRequested?.Invoke(this, EventArgs.Empty);
}

public class PopupEventArgs : EventArgs
{
    public ShopItem ShopItem { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Price { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
}