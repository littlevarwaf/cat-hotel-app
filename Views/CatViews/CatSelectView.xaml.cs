using CatHotel.Models;
using CatHotel.Services;

namespace CatHotel.Views.CatViews;

public partial class CatSelectView : ContentView
{
    private readonly DatabaseService _db = App.Database;
    private readonly HashSet<int> _selectedIds = new();
    private List<Cat> _cats = new();
    private bool _isInitialized;

    public CatSelectView()
    {
        InitializeComponent();
        CatService.CatAdded += async (_, _) => await RefreshAsync();
        CatService.CatUpdated += async (_, _) => await RefreshAsync();
        Loaded += OnViewLoaded;
    }

    private async void OnViewLoaded(object? sender, EventArgs e)
    {
        if (_isInitialized) return;
        _isInitialized = true;
        await RefreshAsync();
    }

    public async Task RefreshAsync()
    {
        await _db.InitializeAsync();
        var customerId = BookingDraftService.Instance.SelectedCustomer?.Id;
        if (!customerId.HasValue)
        {
            HintLabel.Text = "กรุณาเลือกลูกค้าก่อน";
            CatsCollection.ItemsSource = Array.Empty<Cat>();
            return;
        }

        _cats = await _db.Db.Table<Cat>()
            .Where(c => c.CustomerId == customerId.Value)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        _selectedIds.Clear();
        foreach (var cat in BookingDraftService.Instance.SelectedCats)
            _selectedIds.Add(cat.Id);

        CatsCollection.ItemsSource = _cats;
        HintLabel.Text = _cats.Count == 0
            ? "ยังไม่มีแมวของลูกค้านี้ — เพิ่มแมวในแท็บถัดไป"
            : "แตะเพื่อเลือก/ยกเลิก แล้วกดยืนยัน";
    }

    private void OnCatTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is not Cat cat)
            return;

        if (_selectedIds.Contains(cat.Id))
            _selectedIds.Remove(cat.Id);
        else
            _selectedIds.Add(cat.Id);

        CatsCollection.ItemsSource = null;
        CatsCollection.ItemsSource = _cats;
    }

    private async void OnConfirmClicked(object? sender, EventArgs e)
    {
        var selected = _cats.Where(c => _selectedIds.Contains(c.Id)).ToList();
        BookingDraftService.Instance.SelectedCats = selected;

        if (BookingDraftService.Instance.IsPickingCats)
        {
            BookingDraftService.Instance.EndCatPick();
            await NavigationService.GoBackAsync();
        }
    }
}
