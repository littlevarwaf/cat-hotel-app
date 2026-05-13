using CatHotel.Services;
using CatHotel.Views;
using Microcharts.Maui;
using Microsoft.Extensions.Logging;

using Sharpnado.Tabs;

namespace CatHotel
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseSharpnadoTabs(loggerEnable: false)
                .UseMicrocharts()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif

            builder
                .Services
                .AddSingleton<IRoomRepository, DatabaseRoomRepository>()
                .AddSingleton<HomePage>()
                .AddSingleton<CalendarPage>()
                .AddSingleton<Sales>();

            builder.Services.AddTransient<HomePage>();
            builder.Services.AddTransient<CalendarPage>();
            builder.Services.AddTransient<Sales>();
            builder.Services.AddTransient<ShopPage>();
            builder.Services.AddTransient<RoomDetailPage>();
            builder.Services.AddTransient<CartPage>();

            return builder.Build();
        }
    }
}
