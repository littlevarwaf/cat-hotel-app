using CatHotel.Services;
using CatHotel.Views.RoomSettingViews;

namespace CatHotel.Views;

public partial class RoomSettingsWrapperPage : ContentPage, INavigationAware
{
    private int _selectedTabIndex = 0;

    public RoomSettingsWrapperPage()
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

                // Refresh RoomsView when tab 0 (Edit Rooms) is selected
                if (value == 0 && this.FindByName("RoomsView") is RoomsView roomsView)
                {
                    MainThread.BeginInvokeOnMainThread(async () => await roomsView.RefreshAsync());
                }
            }
        }
    }

    public async void OnNavigatedTo(IDictionary<string, object> parameters)
    {
        // Also refresh when navigating back to this wrapper page
        if (this.FindByName("RoomsView") is RoomsView roomsView)
        {
            await roomsView.RefreshAsync();
        }
    }

    public void OnNavigatingFrom(IDictionary<string, object> parameters)
    {
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await NavigationService.GoBackAsync();
    }
}