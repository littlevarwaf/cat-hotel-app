using CatHotel.Views;

namespace CatHotel.Services;

public static class NavigationService
{
    public const string RoomWrapper = "RoomWrapper";
    public const string CartPage = "CartPage";

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
        if (page != null && page.BindingContext is INavigationAware navigationAware)
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
            _ => null
        };
    }
}

public interface INavigationAware
{
    void OnNavigatedTo(IDictionary<string, object> parameters);
}