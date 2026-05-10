using Microsoft.Extensions.DependencyInjection;
using CatHotel.Services;

namespace CatHotel
{
    public partial class App : Application
    {
        private static DatabaseService _database;
        private static bool _dbInitialized;

        public static DatabaseService Database =>
            _database ??= new DatabaseService(Path.Combine(FileSystem.AppDataDirectory, "cathotel.db3"));

        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(new AppShell());

            // init DB หลังสร้าง UI แล้ว (ไม่ block UI thread)
            if (!_dbInitialized)
            {
                _dbInitialized = true;
                _ = InitializeDatabaseAsync();
            }

            return window;
        }

        private static async Task InitializeDatabaseAsync()
        {
            try
            {
                await Database.InitializeAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("DB init failed: " + ex);
                // ถ้าต้องการ popup ก็ทำได้ แต่ต้อง marshal ไป main thread และมี page แล้ว
            }
        }
    }
}