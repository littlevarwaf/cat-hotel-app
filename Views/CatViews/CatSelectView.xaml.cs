using CatHotel.Models;
using CatHotel.Services;
using CatHotel.ViewModels;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace CatHotel.Views.CatViews;

public partial class CatSelectView : ContentView
{
    private readonly ICatRepository _catRepo;
    private readonly IBookingCatRepository _bookingCatRepo;
    private List<Cat> _allCats = new();
    private CatWrapperViewModel? _viewModel;
    private CancellationTokenSource? _searchCancellationTokenSource;
    private bool _isInitialized;

    public CatSelectView()
    {
        InitializeComponent();
        _catRepo = IPlatformApplication.Current!.Services.GetRequiredService<ICatRepository>();
        _bookingCatRepo = IPlatformApplication.Current!.Services.GetRequiredService<IBookingCatRepository>();
        this.BindingContextChanged += OnBindingContextChanged;

        // Subscribe to cat events
        CatService.CatAdded += async (_, _) => await RefreshAsync();
        CatService.CatUpdated += async (_, _) => await RefreshAsync();
        CatService.CatDeleted += async (_, _) => await RefreshAsync();

        this.Loaded += OnViewLoaded;
    }

    private async void OnViewLoaded(object? sender, EventArgs e)
    {
        if (_isInitialized) return;
        _isInitialized = true;

        try
        {
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("[CATS][FATAL] " + ex);
        }
    }

    private void OnBindingContextChanged(object? sender, EventArgs e)
    {
        if (BindingContext is CatWrapperViewModel vm)
        {
            _viewModel = vm;
            System.Diagnostics.Debug.WriteLine($"[CatSelectView] BindingContext set to CatWrapperViewModel");

            // Subscribe to cat collection changes
            if (_viewModel.Cats is INotifyCollectionChanged notifyCollection)
            {
                notifyCollection.CollectionChanged += OnCatsCollectionChanged;
            }

            System.Diagnostics.Debug.WriteLine($"[CatSelectView] Initial cats count: {_viewModel.Cats.Count}");
        }
    }

    private void OnCatsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Update _allCats when the ViewModel's collection changes
        if (_viewModel != null)
        {
            UpdateAllCats();
            System.Diagnostics.Debug.WriteLine($"[CatSelectView] Cats collection updated. Total: {_allCats.Count}");
        }
    }

    public async Task RefreshAsync()
    {
        try
        {
            if (_viewModel != null)
            {
                await _viewModel.RefreshCatsAsync();
                UpdateAllCats();
                ApplyFiltering();
            }
            SearchEntry.Text = string.Empty;
            System.Diagnostics.Debug.WriteLine($"[CatSelectView] Refreshed");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CatSelectView] Error refreshing: {ex}");
        }
    }

    private void UpdateAllCats()
    {
        if (_viewModel != null)
        {
            _allCats = new List<Cat>(_viewModel.Cats);
            System.Diagnostics.Debug.WriteLine($"[CatSelectView] Updated _allCats with {_allCats.Count} cats");
        }
    }

    private void ApplyFiltering()
    {
        if (_viewModel == null)
            return;

        var displayCats = _allCats.ToList();

        // Filter out already selected cats
        if (_viewModel.Mode == 1)
        {
            // BookingPage mode: hide cats already in BookingDraftService
            var selectedIds = BookingDraftService.Instance.SelectedCats.Select(c => c.Id).ToHashSet();
            displayCats = displayCats.Where(c => !selectedIds.Contains(c.Id)).ToList();
            System.Diagnostics.Debug.WriteLine($"[CatSelectView] Filtered for BookingPage: showing {displayCats.Count} cats (hidden {_allCats.Count - displayCats.Count})");
        }
        else if (_viewModel.Mode == 0)
        {
            // RoomDetailPage mode: hide cats already in the booking
            var selectedIds = _viewModel.Cats.Select(c => c.Id).ToHashSet();
            displayCats = displayCats.Where(c => !selectedIds.Contains(c.Id)).ToList();
            System.Diagnostics.Debug.WriteLine($"[CatSelectView] Filtered for RoomDetailPage: showing {displayCats.Count} cats (hidden {_allCats.Count - displayCats.Count})");
        }

        _viewModel.Cats = new ObservableCollection<Cat>(displayCats);
    }

    private async void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        _searchCancellationTokenSource?.Cancel();
        _searchCancellationTokenSource = new CancellationTokenSource();

        try
        {
            await Task.Delay(300, _searchCancellationTokenSource.Token);

            var query = e.NewTextValue?.ToLower() ?? string.Empty;

            if (_viewModel == null)
            {
                System.Diagnostics.Debug.WriteLine($"[CatSelectView] ViewModel is null");
                return;
            }

            var filtered = string.IsNullOrWhiteSpace(query)
                ? _allCats
                : _allCats.Where(c =>
                    c.Name.ToLower().Contains(query) ||
                    c.Breed.ToLower().Contains(query))
                    .ToList();

            // Apply filtering after search
            if (_viewModel.Mode == 1)
            {
                var selectedIds = BookingDraftService.Instance.SelectedCats.Select(c => c.Id).ToHashSet();
                filtered = filtered.Where(c => !selectedIds.Contains(c.Id)).ToList();
            }
            else if (_viewModel.Mode == 0)
            {
                var selectedIds = _viewModel.Cats.Select(c => c.Id).ToHashSet();
                filtered = filtered.Where(c => !selectedIds.Contains(c.Id)).ToList();
            }

            _viewModel.Cats = new ObservableCollection<Cat>(filtered);
        }
        catch (OperationCanceledException)
        {
            System.Diagnostics.Debug.WriteLine($"[CatSelectView] Search cancelled");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CatSelectView] Error during search: {ex}");
        }
    }

    private async void OnCatSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is Cat selectedCat)
        {
            await SelectCatAsync(selectedCat);
        }
    }

    private async Task SelectCatAsync(Cat cat)
    {
        if (_viewModel == null)
            return;

        try
        {
            System.Diagnostics.Debug.WriteLine($"[CatSelectView] Cat selected: {cat.Name} (ID: {cat.Id})");

            // Mode 1 = BookingPage flow (save to BookingDraftService)
            if (_viewModel.Mode == 1)
            {
                System.Diagnostics.Debug.WriteLine($"[CatSelectView] Mode: BOOKING DRAFT (1)");
                var selectedCats = BookingDraftService.Instance.SelectedCats;
                var editingIndex = BookingDraftService.Instance.EditingCatIndex;

                // If editing a specific cat, replace it
                if (editingIndex.HasValue && editingIndex.Value >= 0 && editingIndex.Value < selectedCats.Count)
                {
                    System.Diagnostics.Debug.WriteLine($"[CatSelectView] Replacing cat at index {editingIndex.Value}");
                    selectedCats[editingIndex.Value] = cat;
                }
                else
                {
                    // Normal add mode
                    if (selectedCats.Any(c => c.Id == cat.Id))
                    {
                        selectedCats.Remove(selectedCats.First(c => c.Id == cat.Id));
                    }
                    else
                    {
                        selectedCats.Add(cat);
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[CatSelectView] BookingDraftService.SelectedCats updated. Total: {selectedCats.Count}");
            }
            // Mode 0 = RoomDetailPage flow (save to database)
            else if (_viewModel.Mode == 0 && _viewModel.BookingId > 0)
            {
                System.Diagnostics.Debug.WriteLine($"[CatSelectView] Mode: ROOM DETAIL (0) - BookingId: {_viewModel.BookingId}");

                // Check if we're in edit mode (replacing a cat)
                if (_viewModel.EditingBookingCatId.HasValue && _viewModel.EditingBookingCatId.Value > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[CatSelectView] EDIT MODE: Replacing cat with BookingCatId={_viewModel.EditingBookingCatId}");

                    // Get the current BookingCat record to find what cat is currently assigned
                    var currentBookingCat = await _bookingCatRepo.GetBookingCatByIdAsync(_viewModel.EditingBookingCatId.Value);
                    if (currentBookingCat == null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[CatSelectView] Error: BookingCat not found: {_viewModel.EditingBookingCatId}");
                        await Application.Current!.MainPage!.DisplayAlertAsync("Error",
                            "Could not find booking cat record.", "OK");
                        CatsCollectionView.SelectedItem = null;
                        return;
                    }

                    // Check 1: Is it the same cat (no change needed)?
                    if (currentBookingCat.CatId == cat.Id)
                    {
                        System.Diagnostics.Debug.WriteLine($"[CatSelectView] Cat is already selected: {cat.Name}");
                        await Application.Current!.MainPage!.DisplayAlertAsync("No Change",
                            $"'{cat.Name}' is already selected for this booking.", "OK");
                        CatsCollectionView.SelectedItem = null;
                        return;
                    }

                    // Check 2: Is the new cat already in the booking (other than the current one)?
                    var isAlreadyInBooking = await _bookingCatRepo.IsCatInBookingAsync(_viewModel.BookingId, cat.Id);

                    if (isAlreadyInBooking)
                    {
                        System.Diagnostics.Debug.WriteLine($"[CatSelectView] Cat already exists in booking: {cat.Name}");
                        await Application.Current!.MainPage!.DisplayAlertAsync("Duplicate Cat",
                            $"'{cat.Name}' is already assigned to this booking.", "OK");
                        CatsCollectionView.SelectedItem = null;
                        return;
                    }

                    // Update the BookingCat record with the new CatId
                    try
                    {
                        System.Diagnostics.Debug.WriteLine($"[CatSelectView] Updating BookingCat: {_viewModel.EditingBookingCatId} with new CatId: {cat.Id}");
                        await _bookingCatRepo.UpdateCatInBookingAsync(_viewModel.EditingBookingCatId.Value, cat.Id);
                        System.Diagnostics.Debug.WriteLine($"[CatSelectView] BookingCat updated successfully");
                    }
                    catch (Exception dbEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"[CatSelectView] Error updating BookingCat: {dbEx}");
                        await Application.Current!.MainPage!.DisplayAlertAsync("Error",
                            $"Failed to replace cat: {dbEx.Message}", "OK");
                        CatsCollectionView.SelectedItem = null;
                        return;
                    }
                }
                else
                {
                    // Normal add mode (adding a new cat)
                    System.Diagnostics.Debug.WriteLine($"[CatSelectView] ADD MODE: Adding new cat to booking {_viewModel.BookingId}");

                    try
                    {
                        await _bookingCatRepo.AddCatToBookingAsync(_viewModel.BookingId, cat.Id);
                        System.Diagnostics.Debug.WriteLine($"[CatSelectView] BookingCat inserted: BookingId={_viewModel.BookingId}, CatId={cat.Id}");
                    }
                    catch (Exception dbEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"[CatSelectView] Error inserting BookingCat: {dbEx}");
                        await Application.Current!.MainPage!.DisplayAlertAsync("Error",
                            $"Failed to add cat to booking: {dbEx.Message}", "OK");
                        CatsCollectionView.SelectedItem = null;
                        return;
                    }
                }
            }

            SearchEntry.Text = string.Empty;
            await NavigationService.GoBackAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CatSelectView] Error selecting cat: {ex}");
            await Application.Current!.MainPage!.DisplayAlertAsync("Error",
                $"Failed to select cat: {ex.Message}", "OK");
        }
        finally
        {
            CatsCollectionView.SelectedItem = null;
        }
    }

    private async void OnEditButtonClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.BindingContext is Cat cat)
        {
            await NavigationService.GoToAsync("CatEditPage",
                new Dictionary<string, object> { ["catId"] = cat.Id });
        }
    }
}