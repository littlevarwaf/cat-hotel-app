using CatHotel.Models;
using CatHotel.Services;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace CatHotel.ViewModels;

public class CatWrapperViewModel : INotifyCollectionChanged, INotifyPropertyChanged, INavigationAware
{
    private readonly ICatRepository _catRepo;
    private int _selectedTabIndex = 0;
    private int _mode = 0; // 0 = RoomDetailPage, 1 = BookingPage
    private int _bookingId = 0;
    private bool _isLoading = false;
    private ObservableCollection<Cat> _cats = new();
    private Cat? _selectedCat;

    public CatWrapperViewModel()
    {
        _catRepo = IPlatformApplication.Current!.Services.GetRequiredService<ICatRepository>();
        GoBackCommand = new Command(async () => await NavigationService.GoBackAsync());
    }

    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set
        {
            if (_selectedTabIndex != value)
            {
                _selectedTabIndex = value;
                OnPropertyChanged();
            }
        }
    }

    public int Mode
    {
        get => _mode;
        set
        {
            if (_mode != value)
            {
                _mode = value;
                OnPropertyChanged();
            }
        }
    }

    public int BookingId
    {
        get => _bookingId;
        set
        {
            if (_bookingId != value)
            {
                _bookingId = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (_isLoading != value)
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }
    }

    public ObservableCollection<Cat> Cats
    {
        get => _cats;
        set
        {
            if (_cats != value)
            {
                _cats = value;
                OnPropertyChanged();
            }
        }
    }

    public Cat? SelectedCat
    {
        get => _selectedCat;
        set
        {
            if (_selectedCat != value)
            {
                _selectedCat = value;
                OnPropertyChanged();
            }
        }
    }

    public ICommand GoBackCommand { get; }

    public async void OnNavigatedTo(IDictionary<string, object> parameters)
    {
        System.Diagnostics.Debug.WriteLine($"[CatWrapper] OnNavigatedTo called with params: {string.Join(",", parameters.Keys)}");

        // Get mode (0 = RoomDetailPage, 1 = BookingPage)
        if (parameters.TryGetValue("mode", out var modeObj) && modeObj is int mode)
        {
            Mode = mode;
            System.Diagnostics.Debug.WriteLine($"[CatWrapper] Mode set to: {mode}");
        }

        // Get bookingId if provided (Mode 0 = RoomDetailPage)
        if (parameters.TryGetValue("bookingId", out var bookingIdObj) && bookingIdObj is int bookingId)
        {
            BookingId = bookingId;
            System.Diagnostics.Debug.WriteLine($"[CatWrapper] BookingId set to: {bookingId}");
        }

        // Load all cats
        await LoadCatsAsync();
    }

    private async Task LoadCatsAsync()
    {
        IsLoading = true;
        try
        {
            System.Diagnostics.Debug.WriteLine($"[CatWrapper] Loading all cats");
            var cats = await _catRepo.GetAllCatsAsync();
            cats = cats.OrderByDescending(c => c.CreatedAt).ToList();

            Cats = new ObservableCollection<Cat>(cats);
            System.Diagnostics.Debug.WriteLine($"[CatWrapper] Loaded {cats.Count} cats");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CatWrapper] Error loading cats: {ex}");
            await Application.Current!.MainPage!.DisplayAlertAsync("Error",
                $"Failed to load cats: {ex.Message}", "OK");
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task RefreshCatsAsync()
    {
        await LoadCatsAsync();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public event NotifyCollectionChangedEventHandler CollectionChanged;

    protected void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        CollectionChanged?.Invoke(this, e);
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}