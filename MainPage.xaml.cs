namespace CatHotel
{
    public partial class MainPage : ContentPage
    {
        private readonly MainViewModel _vm = new();

        public MainPage()
        {
            InitializeComponent();
            BindingContext = _vm;
            _vm.PropertyChanged += OnViewModelPropertyChanged;
        }

        private async void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.SelectedTabIndex) && _vm.SelectedTabIndex == 1)
                await Calendar.RefreshRoomsAsync();
        }
    }
}
