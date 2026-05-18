using CatHotel.Services;
using CatHotel.ViewModels;

namespace CatHotel.Views;

public partial class SalesAiSummaryView : ContentView
{
    public SalesAiSummaryView()
    {
        InitializeComponent();

        // Use dependency injection to resolve services
        try
        {
            var geminiService = IPlatformApplication.Current?.Services.GetRequiredService<GeminiAiService>();
            var databaseService = IPlatformApplication.Current?.Services.GetRequiredService<DatabaseService>();

            if (geminiService != null && databaseService != null)
            {
                BindingContext = new SalesAnalysisViewModel(geminiService, databaseService);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[SalesAiSummaryView] Services not found in DI container");
                BindingContext = new SalesAnalysisViewModel();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SalesAiSummaryView] Error initializing ViewModel: {ex}");
            BindingContext = new SalesAnalysisViewModel();
        }
    }

    private void OnTestClicked(object sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("[SalesAiSummaryView] TEST BUTTON CLICKED!");
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await Application.Current.MainPage.DisplayAlert("Test", "Button works!", "OK");
        });
    }
}