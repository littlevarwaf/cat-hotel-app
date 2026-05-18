using CatHotel.Services;
using Color = Microsoft.Maui.Graphics.Color;

namespace CatHotel.Views;

public partial class GeminiApiKeySetupPage : ContentPage
{
    private const string ApiKeyStorageKey = "gemini_api_key";

    public GeminiApiKeySetupPage()
    {
        InitializeComponent();
        LoadApiKey();
    }

    private async void LoadApiKey()
    {
        try
        {
            var apiKey = await SecureStorage.GetAsync(ApiKeyStorageKey);
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                ApiKeyEntry.Text = apiKey;
            }
        }
        catch (Exception ex)
        {
            DisplayErrorMessage($"Error loading API key: {ex.Message}");
        }
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ApiKeyEntry.Text))
        {
            DisplayErrorMessage("Please enter your API Key");
            return;
        }

        if (ApiKeyEntry.Text.Length < 20)
        {
            DisplayErrorMessage("API Key appears to be invalid (too short)");
            return;
        }

        try
        {
            // Disable interaction during save
            ApiKeyEntry.IsEnabled = false;

            DisplayLoadingMessage("Saving API Key...");

            await SecureStorage.SetAsync(ApiKeyStorageKey, ApiKeyEntry.Text);

            DisplaySuccessMessage("API Key saved successfully!");

            // Delay before navigating back
            await Task.Delay(1500);
            await NavigationService.GoBackAsync();
        }
        catch (Exception ex)
        {
            DisplayErrorMessage($"Error saving API key: {ex.Message}");
        }
        finally
        {
            ApiKeyEntry.IsEnabled = true;
        }
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(ApiKeyEntry.Text))
        {
            bool confirmDiscard = await DisplayAlertAsync(
                "Discard Changes",
                "Are you sure you want to discard your changes?",
                "Yes",
                "No");

            if (!confirmDiscard)
                return;
        }

        await NavigationService.GoBackAsync();
    }

    private void DisplayErrorMessage(string message)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            StatusLabel.Text = message;
            StatusLabel.TextColor = (Color)Application.Current!.Resources["SemanticError"];
        });
    }

    private void DisplayLoadingMessage(string message)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            StatusLabel.Text = message;
            StatusLabel.TextColor = (Color)Application.Current!.Resources["SemanticInfo"];
        });
    }

    private void DisplaySuccessMessage(string message)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            StatusLabel.Text = message;
            StatusLabel.TextColor = (Color)Application.Current!.Resources["SemanticSuccess"];
        });
    }
}
