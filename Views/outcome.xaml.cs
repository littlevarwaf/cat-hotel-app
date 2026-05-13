using CatHotel.Models;
using CatHotel.Services;

namespace CatHotel.Views;

public partial class outcome : ContentPage
{
    private readonly DatabaseService _db;

    public outcome()
    {
        InitializeComponent();
        _db = App.Database;
    }

    private async void OnAddIncomeClicked(object sender, EventArgs e)
    {
        // อยู่หน้านี้อยู่แล้ว — ไม่ทำอะไร
    }

    private async void OnHistoryIncomeClicked(object sender, EventArgs e)
    {
        await NavigationService.GoToAsync(NavigationService.OutcomeHistoryPage);
    }

    private async void OnConfirmTapped(object sender, TappedEventArgs e)
    {
        // Validate amount
        var amountText = AmountEntry?.Text?.Trim();
        if (string.IsNullOrWhiteSpace(amountText) || !double.TryParse(amountText, out double amount) || amount <= 0)
        {
            await DisplayAlert("Error", "กรุณากรอกยอดเงินให้ถูกต้อง\nPlease enter a valid amount.", "OK");
            return;
        }

        var note = NoteEditor?.Text?.Trim() ?? string.Empty;

        var record = new OutcomeRecord
        {
            Amount    = amount,
            Note      = note,
            CreatedAt = DateTime.Now
        };

        await _db.AddOutcomeRecordAsync(record);

        // Clear form
        AmountEntry.Text = string.Empty;
        NoteEditor.Text  = string.Empty;

        await DisplayAlert("สำเร็จ", $"บันทึกยอดเงิน ฿{amount:N2} เรียบร้อยแล้ว", "OK");
    }
}
