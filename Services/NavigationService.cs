using CatHotel.Views;
using CatHotel.Views.ShopSettingViews;

namespace CatHotel.Services;

public static class NavigationService
{
    public const string RoomWrapper = "RoomWrapper";
    public const string RoomSettingsWrapperPage = "RoomSettingsWrapperPage";
    public const string ShopSettingsWrapperPage = "ShopSettingsWrapperPage";
    public const string DiscountSettingsWrapperPage = "DiscountSettingsWrapperPage";
    public const string CartPage = "CartPage";
    public const string SettingsMenuPage = "SettingsMenuPage";
    public const string ShopItemEditPage = "ShopItemEditPage";

    public static async Task GoToAsync(string route)
    {
        var page = GetPageByRoute(route);
        if (page != null)
        {
            await Application.Current!.MainPage!.Navigation.PushAsync(page);
        }
    }

    public static async Task GoToAsync(string route, IDictionary<string, object> parameters)
    {
        var page = GetPageByRoute(route);
        if (page == null)
        {
            throw new InvalidOperationException($"Route '{route}' not found in navigation service.");
        }

        if (page.BindingContext is INavigationAware navigationAware)
        {
            navigationAware.OnNavigatedTo(parameters);
        }
        await Application.Current!.MainPage!.Navigation.PushAsync(page);
    }

    public static async Task GoBackAsync()
    {
        await Application.Current!.MainPage!.Navigation.PopAsync();
    }

    private static ContentPage? GetPageByRoute(string route)
    {
        return route switch
        {
            nameof(RoomWrapper) => new RoomWrapperPage(),
            nameof(CartPage) => new CartPage(),
            nameof(SettingsMenuPage) => new SettingsMenuPage(),
            nameof(RoomSettingsWrapperPage) => new RoomSettingsWrapperPage(),
            nameof(ShopSettingsWrapperPage) => new ShopSettingsWrapperPage(),
            nameof(DiscountSettingsWrapperPage) => new DiscountSettingsWrapperPage(),
            nameof(ShopItemEditPage) => new ShopItemEditPage(),
            _ => null
        };
    }
}

public interface INavigationAware
{
    void OnNavigatedTo(IDictionary<string, object> parameters);
}