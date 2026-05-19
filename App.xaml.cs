using CatHotel.Services;

namespace CatHotel
{
    public partial class App : Application
    {
        private static DatabaseService _database;

        public static DatabaseService Database =>
            _database ??= new DatabaseService(Path.Combine(FileSystem.AppDataDirectory, "cathotel3.db3"));

        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            // สร้าง loading page ก่อน
            var loadingPage = new ContentPage
            {
                BackgroundColor = Color.FromArgb("#FFF5F5"),
                Content = new ActivityIndicator
                {
                    IsRunning = true,
                    Color = Color.FromArgb("#E57373"),
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.Center
                }
            };

            NavigationPage.SetHasNavigationBar(loadingPage, false);

            var window = new Window(new NavigationPage(loadingPage));

            // init DB แล้วค่อย navigate ไป MainPage
            _ = InitializeAndNavigateAsync(window);

            return window;
        }

        private static async Task InitializeAndNavigateAsync(Window window)
        {
            try
            {
                // Uncomment the line below if you need to reset the database (e.g., due to schema migration errors)
                //await Database.DeleteDatabaseAsync();

                await Database.InitializeAsync(); // รอ DB init เสร็จก่อน
                //await Database.SeedMockDataAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("DB init failed: " + ex);

                // If initialization fails, try deleting and reinitializing the database
                try
                {
                    System.Diagnostics.Debug.WriteLine("Attempting to reset database...");
                    await Database.DeleteDatabaseAsync();
                    await Database.InitializeAsync();
                    await Database.SeedMockDataAsync();
                    System.Diagnostics.Debug.WriteLine("Database reset successful!");
                }
                catch (Exception resetEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Database reset failed: {resetEx}");
                }
            }

            // navigate ไป MainPage บน UI thread
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                if (window.Page is NavigationPage nav)
                    await nav.PushAsync(new MainPage());
            });
        }
    }
}