using CatHotel.Services;
using CatHotel.Views;

namespace CatHotel;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        RegisterRoutes();
    }

    private static void RegisterRoutes()
    {
        Routing.RegisterRoute(NavigationService.RoomDetail, typeof(RoomDetailPage));
        Routing.RegisterRoute(NavigationService.Cart, typeof(CartPage));
        Routing.RegisterRoute(NavigationService.Shop, typeof(ShopPage));
    }
}
