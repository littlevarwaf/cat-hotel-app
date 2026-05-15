using CatHotel.Services;
using CatHotel.Views.CustomerViews;

namespace CatHotel.Views;

public partial class CustomerPickerWrapperPage : ContentPage, INavigationAware
{
    private int _selectedTabIndex;

    public CustomerPickerWrapperPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set
        {
            if (_selectedTabIndex == value) return;
            _selectedTabIndex = value;
            OnPropertyChanged();
            if (value == 0)
                _ = CustomerSelect.RefreshAsync();
        }
    }

    public async void OnNavigatedTo(IDictionary<string, object> parameters)
    {
        BookingDraftService.Instance.BeginCustomerPick();
        SelectedTabIndex = 0;
        await CustomerSelect.RefreshAsync();
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        BookingDraftService.Instance.EndCustomerPick();
        await NavigationService.GoBackAsync();
    }
}
