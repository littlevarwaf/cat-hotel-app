using CatHotel.Views;
using CatHotel.Views.ShopSettingViews;
using CatHotel.Views.RoomSettingViews;
using CatHotel.Views.DiscountSettingViews;
using CatHotel.Views.OutcomeSettingViews;
using CatHotel.Views.CustomerViews;
using CatHotel.Views.CatViews;

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
    public const string BookingPage = "BookingPage";
    public const string CustomerEditPage = "CustomerEditPage";
    public const string CatEditPage = "CatEditPage";

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

        // Check both the page itself AND its BindingContext for INavigationAware
        if (page is INavigationAware pageNavigationAware)
        {
            pageNavigationAware.OnNavigatedTo(parameters);
        }
        else if (page.BindingContext is INavigationAware bindingContextNavigationAware)
        {
            bindingContextNavigationAware.OnNavigatedTo(parameters);
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
            nameof(CustomerWrapperPage) => new CustomerWrapperPage(),
            nameof(CatWrapperPage) => new CatWrapperPage(), 
            nameof(ShopItemEditPage) => new ShopItemEditPage(),
            nameof(RoomEditPage) => new RoomEditPage(),
            nameof(DiscountEditPage) => new DiscountEditPage(),
            nameof(BookingPage) => new BookingPage(),
            nameof(CustomerEditPage) => new CustomerEditPage(),
            nameof(CatEditPage) => new CatEditPage(),
            _ => null
        };
    }
}

public interface INavigationAware
{
    void OnNavigatedTo(IDictionary<string, object> parameters);
}