using CatHotel.Models;
using CatHotel.Services;

namespace CatHotel.Views.OutcomeSettingViews;

public partial class OutcomeView : ContentView
{
    private readonly DatabaseService _db;

    public OutcomeView()
    {
        InitializeComponent();
        _db = App.Database;
    }

    private async void OnConfirmTapped(object sender, TappedEventArgs e)
    {
        // Validate amount
        var amountText = AmountEntry?.Text?.Trim();
        if (string.IsNullOrWhiteSpace(amountText) || !double.TryParse(amountText, out double amount) || amount <= 0)
        {
            await Application.Current!.MainPage!.DisplayAlert("Error", "กรุณากรอกยอดเงินให้ถูกต้อง\nPlease enter a valid amount.", "OK");
            return;
        }

        var note = NoteEntry?.Text?.Trim() ?? string.Empty;

        var record = new OutcomeRecord
        {
            Amount = amount,
            Note = note,
            CreatedAt = DateTime.Now
        };

        await _db.AddOutcomeRecordAsync(record);

        // Clear form
        AmountEntry.Text = string.Empty;
        NoteEntry.Text = string.Empty;

        // Notify subscribers that outcome was added
        OutcomeService.NotifyOutcomeAdded(record);

        await Application.Current!.MainPage!.DisplayAlertAsync("สำเร็จ", $"บันทึกยอดเงิน ฿{amount:N2} เรียบร้อยแล้ว", "OK");
    }
}