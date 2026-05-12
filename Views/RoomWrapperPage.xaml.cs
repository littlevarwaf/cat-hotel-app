using CatHotel.ViewModels;

namespace CatHotel.Views;

public partial class RoomWrapperPage : ContentPage
{
	public RoomWrapperPage()
	{
		InitializeComponent();
		BindingContext = new RoomWrapperViewModel();
    }
}