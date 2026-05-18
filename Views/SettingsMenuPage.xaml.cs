using CatHotel.Services;

namespace CatHotel.Views;

public partial class SettingsMenuPage : ContentPage
{
    public SettingsMenuPage()
    {
        InitializeComponent();
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await NavigationService.GoBackAsync();
    }

    private async void OnRoomSettingsClicked(object sender, EventArgs e)
    {
        await NavigationService.GoToAsync(NavigationService.RoomSettingsWrapperPage);
    }

    private async void OnShopSettingsClicked(object sender, EventArgs e)
    {
        await NavigationService.GoToAsync(NavigationService.ShopSettingsWrapperPage);
    }

    private async void OnDiscountSettingsClicked(object sender, EventArgs e)
    {
        await NavigationService.GoToAsync(NavigationService.DiscountSettingsWrapperPage);
    }

    // ปุ่ม Income -> ไปหน้า outcome โดยตรง
    private async void OnIncomeClicked(object sender, EventArgs e)
    {
        await NavigationService.GoToAsync(NavigationService.OutcomeSettingsWrapperPage);
    }

    // AI Settings
    private async void OnAiSettingsClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new GeminiApiKeySetupPage());
    }
}