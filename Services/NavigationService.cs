using CatHotel.Views;

namespace CatHotel.Services;

public static class NavigationService
{
    public const string RoomDetail = "RoomDetailPage";
    public const string Cart       = "CartPage";
    public const string Shop       = nameof(ShopPage);

    public static Task GoToAsync(string route) =>
        Shell.Current.GoToAsync(route);

    public static Task GoToAsync(string route, IDictionary<string, object> parameters) =>
        Shell.Current.GoToAsync(route, parameters);

    public static Task GoBackAsync() =>
        Shell.Current.GoToAsync("..");
}
