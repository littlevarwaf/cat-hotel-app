using CatHotel.Services;

namespace CatHotel.Views;

public partial class OutcomeSettingsWrapperPage : ContentPage
{
    private int _selectedTabIndex = 0;

    public OutcomeSettingsWrapperPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set
        {
            if (_selectedTabIndex != value)
            {
                _selectedTabIndex = value;
                OnPropertyChanged(nameof(SelectedTabIndex));
            }
        }
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await NavigationService.GoBackAsync();
    }
}