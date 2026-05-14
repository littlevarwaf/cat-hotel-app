using CatHotel.Services;
using CatHotel.Views.DiscountSettingViews;

namespace CatHotel.Views;

public partial class DiscountSettingsWrapperPage : ContentPage, INavigationAware
{
    private int _selectedTabIndex = 0;

    public DiscountSettingsWrapperPage()
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

                // Refresh DiscountsView when tab 0 (View Discounts) is selected
                if (value == 0 && this.FindByName("DiscountsView") is DiscountsView discountsView)
                {
                    MainThread.BeginInvokeOnMainThread(async () => await discountsView.RefreshAsync());
                }
            }
        }
    }

    public async void OnNavigatedTo(IDictionary<string, object> parameters)
    {
        // Switch to tab 0 (View Discounts) when navigating back to this wrapper page
        SelectedTabIndex = 0;

        // Also refresh DiscountsView
        if (this.FindByName("DiscountsView") is DiscountsView discountsView)
        {
            await discountsView.RefreshAsync();
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