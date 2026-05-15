using CatHotel.Services;

namespace CatHotel.Views;

public partial class RoomDetailPage : ContentView
{
    private bool _isInitialized;
    private readonly BookingDraftService _draft = BookingDraftService.Instance;

    public RoomDetailPage()
    {
        InitializeComponent();
        _draft.PropertyChanged += (_, _) => RefreshSummary();
        Loaded += OnViewLoaded;
    }

    private void OnViewLoaded(object? sender, EventArgs e)
    {
        if (_isInitialized) return;
        _isInitialized = true;

        FromDateField.Value = _draft.FromDate;
        ToDateField.Value = _draft.ToDate;
        RefreshSummary();
    }

    public void RefreshBookingFields()
    {
        FromDateField.Value = _draft.FromDate;
        ToDateField.Value = _draft.ToDate;
        RefreshSummary();
    }

    private void RefreshSummary()
    {
        CustomerSummaryLabel.Text = _draft.SelectedCustomerDisplay;
        CatsSummaryLabel.Text = _draft.SelectedCatsDisplay;
    }

    private async void OnPickCustomerClicked(object? sender, EventArgs e)
    {
        _draft.FromDate = FromDateField.Value;
        _draft.ToDate = ToDateField.Value;
        await NavigationService.GoToAsync(NavigationService.CustomerPickerWrapperPage);
    }

    private async void OnPickCatsClicked(object? sender, EventArgs e)
    {
        if (_draft.SelectedCustomer == null)
        {
            await Application.Current!.MainPage!.DisplayAlert("เลือกลูกค้าก่อน", "กรุณาเลือกลูกค้าก่อนเลือกแมว", "OK");
            return;
        }

        _draft.FromDate = FromDateField.Value;
        _draft.ToDate = ToDateField.Value;
        await NavigationService.GoToAsync(NavigationService.CatPickerWrapperPage);
    }

    private async void OnBackClicked(object? sender, EventArgs e)
        => await NavigationService.GoBackAsync();

    private async void OnCheckout(object? sender, EventArgs e)
    {
        _draft.FromDate = FromDateField.Value;
        _draft.ToDate = ToDateField.Value;
        await NavigationService.GoBackAsync();
    }
}
