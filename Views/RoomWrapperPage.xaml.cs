using CatHotel.ViewModels;

namespace CatHotel.Views;

public partial class RoomWrapperPage : ContentPage
{
    private RoomWrapperViewModel _viewModel;
    private RoomDetailPageViewModel _roomDetailViewModel;

    public RoomWrapperPage()
    {
        InitializeComponent();
        BindingContext = new RoomWrapperViewModel();
    }

    private void OnPageLoaded(object? sender, EventArgs e)
    {
        // Find the RoomDetailPage in the view hierarchy and set its binding context
        if (this.FindByName("RoomDetailPageInstance") is RoomDetailPage roomDetailPage)
        {
            roomDetailPage.BindingContext = _roomDetailViewModel;
        }
    }
}