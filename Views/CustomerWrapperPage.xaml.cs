using CatHotel.Services;
using CatHotel.ViewModels;
using CatHotel.Views.CustomerViews;

namespace CatHotel.Views;

public partial class CustomerWrapperPage : ContentPage
{
    private CustomerWrapperViewModel _viewModel;

    public CustomerWrapperPage()
    {
        InitializeComponent();
        BindingContext = new CustomerWrapperViewModel();
        _viewModel = (CustomerWrapperViewModel)BindingContext;
        this.Loaded += OnPageLoaded;
    }

    private void OnPageLoaded(object? sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"[CustomerWrapperPage] Loaded");

        // Set binding context for CustomerSelectView
        if (this.FindByName("CustomerSelectViewInstance") is CustomerSelectView customerSelectView)
        {
            customerSelectView.BindingContext = _viewModel;
            System.Diagnostics.Debug.WriteLine($"[CustomerWrapperPage] CustomerSelectView binding context set");
        }
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await NavigationService.GoBackAsync();
    }
}