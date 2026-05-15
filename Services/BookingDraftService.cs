using System.ComponentModel;
using System.Runtime.CompilerServices;
using CatHotel.Models;

namespace CatHotel.Services;

public class BookingDraftService : INotifyPropertyChanged
{
    public static BookingDraftService Instance { get; } = new();

    private Customer? _selectedCustomer;
    private List<Cat> _selectedCats = new();
    private DateTime _fromDate = DateTime.Today.AddHours(14);
    private DateTime _toDate = DateTime.Today.AddDays(1).AddHours(12);
    private bool _isPickingCustomer;
    private bool _isPickingCats;

    public bool IsPickingCustomer
    {
        get => _isPickingCustomer;
        set => SetProperty(ref _isPickingCustomer, value);
    }

    public bool IsPickingCats
    {
        get => _isPickingCats;
        set => SetProperty(ref _isPickingCats, value);
    }

    public Customer? SelectedCustomer
    {
        get => _selectedCustomer;
        set
        {
            if (_selectedCustomer?.Id != value?.Id)
            {
                _selectedCustomer = value;
                _selectedCats.Clear();
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedCustomerDisplay));
                OnPropertyChanged(nameof(SelectedCatsDisplay));
            }
        }
    }

    public List<Cat> SelectedCats
    {
        get => _selectedCats;
        set
        {
            _selectedCats = value ?? new List<Cat>();
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedCatsDisplay));
        }
    }

    public DateTime FromDate
    {
        get => _fromDate;
        set => SetProperty(ref _fromDate, value);
    }

    public DateTime ToDate
    {
        get => _toDate;
        set => SetProperty(ref _toDate, value);
    }

    public string SelectedCustomerDisplay =>
        SelectedCustomer == null ? "ยังไม่ได้เลือกลูกค้า" : $"{SelectedCustomer.Name} · {SelectedCustomer.TelephoneNum}";

    public string SelectedCatsDisplay =>
        SelectedCats.Count == 0 ? "ยังไม่ได้เลือกแมว" : string.Join(", ", SelectedCats.Select(c => c.Name));

    public void BeginCustomerPick() => IsPickingCustomer = true;

    public void EndCustomerPick() => IsPickingCustomer = false;

    public void BeginCatPick() => IsPickingCats = true;

    public void EndCatPick() => IsPickingCats = false;

    public void ResetForRoom(int roomId, DateTime? checkInDate = null)
    {
        SelectedCustomer = null;
        SelectedCats = new List<Cat>();
        var day = (checkInDate ?? DateTime.Today).Date;
        FromDate = day.AddHours(14);
        ToDate = day.AddDays(1).AddHours(12);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;
        field = value;
        OnPropertyChanged(name);
        return true;
    }
}
