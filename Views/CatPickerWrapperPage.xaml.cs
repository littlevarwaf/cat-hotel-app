using CatHotel.Services;
using CatHotel.Views.CatViews;

namespace CatHotel.Views;

public partial class CatPickerWrapperPage : ContentPage, INavigationAware
{
    private int _selectedTabIndex;

    public CatPickerWrapperPage()
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
                _ = CatSelect.RefreshAsync();
        }
    }

    public async void OnNavigatedTo(IDictionary<string, object> parameters)
    {
        BookingDraftService.Instance.BeginCatPick();
        SelectedTabIndex = 0;
        await CatSelect.RefreshAsync();
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        BookingDraftService.Instance.EndCatPick();
        await NavigationService.GoBackAsync();
    }
}
