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
        this.Loaded += OnPageLoaded;
    }

    private void OnPageLoaded(object? sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"[CatWrapperPage] Loaded");

        // Set binding context for CatSelectView
        if (this.FindByName("CatSelectViewInstance") is CatSelectView catSelectView)
        {
            catSelectView.BindingContext = _viewModel;
            System.Diagnostics.Debug.WriteLine($"[CatWrapperPage] CatSelectView binding context set");
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