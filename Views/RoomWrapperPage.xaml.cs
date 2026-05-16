using CatHotel.ViewModels;

namespace CatHotel.Views;

public partial class RoomWrapperPage : ContentPage
{
    private RoomWrapperViewModel _viewModel;

    public RoomWrapperPage()
    {
        InitializeComponent();
        _viewModel = new RoomWrapperViewModel();
        BindingContext = _viewModel;
    }

    private void OnPageLoaded(object? sender, EventArgs e)
    {
        // Find the RoomDetailPage in the view hierarchy and set its binding context
        // to the same RoomWrapperViewModel so it has access to all commands and data
        if (this.FindByName("RoomDetailPageInstance") is RoomDetailPage roomDetailPage)
        {
            roomDetailPage.BindingContext = _viewModel;
        }
    }
}