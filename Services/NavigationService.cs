using CatHotel.Views;
using CatHotel.Views.ShopSettingViews;
using CatHotel.Views.RoomSettingViews;
using CatHotel.Views.DiscountSettingViews;
using CatHotel.Views.OutcomeSettingViews;
using CatHotel.Views.CustomerViews;

namespace CatHotel.Services;

public static class NavigationService
{
    public const string RoomWrapper = "RoomWrapper";
    public const string RoomSettingsWrapperPage = "RoomSettingsWrapperPage";
    public const string ShopSettingsWrapperPage = "ShopSettingsWrapperPage";
    public const string DiscountSettingsWrapperPage = "DiscountSettingsWrapperPage";
    public const string OutcomeSettingsWrapperPage = "OutcomeSettingsWrapperPage";
    public const string CustomerWrapperPage = "CustomerWrapperPage";
    public const string CatWrapperPage = "CatWrapperPage";
    public const string CartPage = "CartPage";
    public const string SettingsMenuPage = "SettingsMenuPage";
    public const string ShopItemEditPage = "ShopItemEditPage";
    public const string RoomEditPage = "RoomEditPage";
    public const string CustomerEditPage = "CustomerEditPage";

    // outcome / outcome history
    public const string OutcomePage = "OutcomePage";
    public const string OutcomeHistoryPage = "OutcomeHistoryPage";

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
            nameof(OutcomeSettingsWrapperPage) => new OutcomeSettingsWrapperPage(),
            nameof(CustomerWrapperPage) => new CustomerWrapperPage(), // Placeholder
            nameof(CatWrapperPage) => new CatWrapperPage(), // Placeholder
            nameof(ShopItemEditPage) => new ShopItemEditPage(),
            nameof(RoomEditPage) => new RoomEditPage(),
            nameof(CustomerEditPage) => new CustomerEditPage(),
            _ => null
        };
    }
}

public interface INavigationAware
{
    void OnNavigatedTo(IDictionary<string, object> parameters);
}