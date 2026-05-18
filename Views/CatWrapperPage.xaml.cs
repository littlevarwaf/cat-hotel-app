using CatHotel.Services;
using CatHotel.ViewModels;
using CatHotel.Views.CatViews;

namespace CatHotel.Views;

public partial class CatWrapperPage : ContentPage
{
    private CatWrapperViewModel _viewModel;

    public CatWrapperPage()
    {
        InitializeComponent();
        BindingContext = new CatWrapperViewModel();
        _viewModel = (CatWrapperViewModel)BindingContext;
    }

    public async void OnNavigatedTo(IDictionary<string, object> parameters)
    {
        if (_viewModel is INavigationAware navigationAware)
        {
            navigationAware.OnNavigatedTo(parameters);
        }
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        // Reset the draft service when leaving in BookingPage mode
        if (_viewModel.Mode == 1)
        {
            BookingDraftService.Instance.EndCatPick();
        }

        await NavigationService.GoBackAsync();
    }
}