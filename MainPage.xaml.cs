using CatHotel.Views;

namespace CatHotel
{
    public partial class MainPage : ContentPage
    {
        private readonly MainViewModel _vm = new();
        private int? _lastActiveTab = null;

        public MainPage()
        {
            InitializeComponent();
            BindingContext = _vm;
            _vm.PropertyChanged += OnViewModelPropertyChanged;
            _vm.SelectedTabIndex = 1;
        }

        private async void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.SelectedTabIndex))
            {
                if (_vm.SelectedTabIndex == 0 && _lastActiveTab != 0)
                {
                    await HomePage.RefreshRoomsAsync();
                }

                if (_vm.SelectedTabIndex == 1 && _lastActiveTab != 1)
                {
                    await Calendar.RefreshCalendarAndRoomsAsync();
                }

                if (_vm.SelectedTabIndex == 2 && _lastActiveTab != 2)
                {
                    var salesView = FindVisualChild<CatHotel.Views.Sales>(this);
                    salesView?.OnTabActivated();
                }

                _lastActiveTab = _vm.SelectedTabIndex;
            }
        }

        public static T? FindVisualChild<T>(Element parent) where T : Element
        {
            if (parent is T matched) return matched;
            if (parent is IElementController controller)
            {
                foreach (var child in controller.LogicalChildren)
                {
                    var found = FindVisualChild<T>(child);
                    if (found != null) return found;
                }
            }
            return null;
        }
    }
}