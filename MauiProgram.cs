using CatHotel.Services;
using CatHotel.Views;
using CommunityToolkit.Maui;
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
                .UseMauiCommunityToolkit()
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

            builder.Services
                // เพิ่ม DatabaseService
                .AddSingleton<DatabaseService>(sp =>
                    new DatabaseService(Path.Combine(FileSystem.AppDataDirectory, "cathotel3.db3")))
                .AddSingleton<IRoomRepository, DatabaseRoomRepository>()
                .AddSingleton<IBookingRepository, DatabaseBookingRepository>()
                .AddSingleton<ICustomerRepository, DatabaseCustomerRepository>()
                .AddSingleton<ICatRepository, DatabaseCatRepository>()
                .AddSingleton<IBookingCatRepository, DatabaseBookingCatRepository>()

                // Pages — เลือกแค่ Singleton หรือ Transient อย่างใดอย่างหนึ่ง
                .AddTransient<HomePage>()
                .AddTransient<CalendarPage>()
                .AddTransient<Sales>()
                .AddTransient<ShopPage>()
                .AddTransient<RoomDetailPage>()
                .AddTransient<CartPage>();

            return builder.Build();
        }
    }
}
