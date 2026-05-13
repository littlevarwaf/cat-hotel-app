using CatHotel.Services;
using CatHotel.Views.ShopSettingViews;

namespace CatHotel.Views;

public partial class ShopSettingsWrapperPage : ContentPage, INavigationAware
{
    private int _selectedTabIndex = 0;

    public ShopSettingsWrapperPage()
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
                
                // Refresh ShopItemsView when tab 0 (Edit Shop Items) is selected
                if (value == 0 && this.FindByName("ItemsView") is ShopItemsView itemsView)
                {
                    MainThread.BeginInvokeOnMainThread(async () => await itemsView.RefreshAsync());
                }
            }
        }
    }

    public async void OnNavigatedTo(IDictionary<string, object> parameters)
    {
        // Also refresh when navigating back to this wrapper page
        if (this.FindByName("ItemsView") is ShopItemsView itemsView)
        {
            await itemsView.RefreshAsync();
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