using CatHotel.Services;
using CatHotel.Views;
using Microcharts.Maui;
using Microsoft.Extensions.Logging;

namespace CatHotel;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMicrocharts()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        builder.Services.AddSingleton<IRoomRepository, MockRoomRepository>();

        builder.Services.AddTransient<HomePage>();
        builder.Services.AddTransient<CalendarPage>();
        builder.Services.AddTransient<Sales>();
        builder.Services.AddTransient<ShopPage>();
        builder.Services.AddTransient<RoomDetailPage>();
        builder.Services.AddTransient<CartPage>();

        return builder.Build();
    }
}
