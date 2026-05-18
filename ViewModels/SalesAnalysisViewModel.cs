using CatHotel.Services;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace CatHotel.ViewModels;

public class SalesAnalysisViewModel : INotifyPropertyChanged
{
    private readonly GeminiAiService _aiService;
    private readonly DatabaseService _databaseService;

    private string _aiSummary = string.Empty;
    private bool _isLoading = false;
    private DateTime? _lastUpdated = null;
    private string _errorMessage = string.Empty;
    private bool _hasError = false;
    private bool _isExpanded = false;

    public bool IsRefreshEnabled
    {
        get => !IsLoading;
    }

    public string AiSummary
    {
        get => _aiSummary;
        set
        {
            if (_aiSummary != value)
            {
                _aiSummary = value;
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
                OnPropertyChanged(nameof(IsRefreshEnabled));
                OnPropertyChanged(nameof(ShowSummaryContent));
            }
        }
    }

    public DateTime? LastUpdated
    {
        get => _lastUpdated;
        set
        {
            if (_lastUpdated != value)
            {
                _lastUpdated = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(LastUpdatedText));
            }
        }
    }

    public string LastUpdatedText
    {
        get => _lastUpdated.HasValue
            ? $"อัปเดตล่าสุด: {_lastUpdated:dd/MM/yyyy HH:mm}"
            : "ยังไม่เคยอัปเดต";
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set
        {
            if (_errorMessage != value)
            {
                _errorMessage = value;
                OnPropertyChanged();
            }
        }
    }

    public bool HasError
    {
        get => _hasError;
        set
        {
            if (_hasError != value)
            {
                _hasError = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ShowSummaryContent));
            }
        }
    }

    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded != value)
            {
                _isExpanded = value;
                OnPropertyChanged();
            }
        }
    }

    public ICommand RefreshCommand { get; }
    public ICommand ToggleExpandCommand { get; }
    public ICommand RetryCommand { get; }

    public SalesAnalysisViewModel() : this(
        IPlatformApplication.Current?.Services.GetRequiredService<GeminiAiService>(),
        IPlatformApplication.Current?.Services.GetRequiredService<DatabaseService>())
    {
    }

    public SalesAnalysisViewModel(GeminiAiService aiService, DatabaseService databaseService)
    {
        _aiService = aiService ?? throw new ArgumentNullException(nameof(aiService), "GeminiAiService is null - check DI registration");
        _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService), "DatabaseService is null - check DI registration");

        RefreshCommand = new Command(async () => await RefreshAnalysisAsync(), CanExecuteRefresh);
        ToggleExpandCommand = new Command(() => IsExpanded = !IsExpanded);
        RetryCommand = new Command(async () => await RefreshAnalysisAsync(), CanExecuteRefresh);

        // Load cached summary on initialization
        _ = LoadCachedSummaryAsync();
    }

    private bool CanExecuteRefresh() => !IsLoading;

    public async Task LoadCachedSummaryAsync()
    {
        try
        {
            var cacheKey = $"sales_ai_summary_{DateTime.Now.Year}";
            var timestampKey = $"sales_ai_timestamp_{DateTime.Now.Year}";

            if (Preferences.ContainsKey(cacheKey))
            {
                AiSummary = Preferences.Get(cacheKey, string.Empty);

                if (Preferences.ContainsKey(timestampKey) &&
                    long.TryParse(Preferences.Get(timestampKey, "0"), out var ticks))
                {
                    LastUpdated = new DateTime(ticks);
                }

                System.Diagnostics.Debug.WriteLine("[SalesAnalysisVM] Cached summary loaded");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SalesAnalysisVM] Error loading cached summary: {ex}");
        }
    }

    public async Task RefreshAnalysisAsync()
    {
        System.Diagnostics.Debug.WriteLine("[SalesAnalysisVM] RefreshAnalysisAsync called");

        IsLoading = true;
        HasError = false;
        ErrorMessage = string.Empty;

        try
        {
            var apiKey = await SecureStorage.GetAsync("gemini_api_key");
            System.Diagnostics.Debug.WriteLine($"[SalesAnalysisVM] API Key retrieved: {(string.IsNullOrWhiteSpace(apiKey) ? "EMPTY" : "SET")}");

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException("ไม่ได้ตั้งค่า API Key กรุณาไปที่การตั้งค่าเพื่อตั้งค่า Gemini API Key");
            }

            System.Diagnostics.Debug.WriteLine("[SalesAnalysisVM] Aggregating sales data...");
            var salesData = await GetCurrentYearSalesDataAsync(apiKey);

            if (salesData.MonthlyData.Count == 0)
            {
                throw new InvalidOperationException("ยังไม่มีข้อมูลเพียงพอสำหรับการวิเคราะห์");
            }

            System.Diagnostics.Debug.WriteLine($"[SalesAnalysisVM] Data aggregated: {salesData.MonthlyData.Count} months");
            System.Diagnostics.Debug.WriteLine("[SalesAnalysisVM] Calling Gemini API...");

            var summary = await _aiService.AnalyzeSalesAsync(salesData);

            System.Diagnostics.Debug.WriteLine("[SalesAnalysisVM] API call successful, caching result...");
            await CacheSummaryAsync(summary);

            AiSummary = summary;
            LastUpdated = DateTime.Now;
            IsExpanded = true;

            System.Diagnostics.Debug.WriteLine("[SalesAnalysisVM] Analysis complete");
        }
        catch (InvalidOperationException ex)
        {
            ErrorMessage = ex.Message;
            HasError = true;
            System.Diagnostics.Debug.WriteLine($"[SalesAnalysisVM] InvalidOperationException: {ex.Message}");
        }
        catch (HttpRequestException ex)
        {
            ErrorMessage = $"เกิดข้อผิดพลาด: ไม่สามารถเชื่อมต่อ API ({ex.Message})";
            HasError = true;
            System.Diagnostics.Debug.WriteLine($"[SalesAnalysisVM] HttpRequestException: {ex}");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"เกิดข้อผิดพลาดที่ไม่คาดคิด: {ex.Message}";
            HasError = true;
            System.Diagnostics.Debug.WriteLine($"[SalesAnalysisVM] Exception: {ex}");
        }
        finally
        {
            IsLoading = false;
            ((Command)RefreshCommand).ChangeCanExecute();
            ((Command)RetryCommand).ChangeCanExecute();
        }
    }

    private async Task<SalesAnalysisData> GetCurrentYearSalesDataAsync(string apiKey)
    {
        var year = DateTime.Now.Year;
        var monthlyTuples = await _databaseService.GetMonthlySalesByYearAsync(year);

        var monthlyData = new List<MonthlySalesRecord>();
        var today = DateTime.Now;

        for (int month = 1; month <= 12; month++)
        {
            var monthDate = new DateTime(year, month, 1);

            if (monthDate > today)
                break;

            var monthTuple = monthlyTuples.FirstOrDefault(m => m.Month.Month == month);

            if (monthTuple.Income <= 0 && monthTuple.Expense <= 0)
                continue;

            monthlyData.Add(new MonthlySalesRecord
            {
                Month = monthDate.ToString("MMMM yyyy", new System.Globalization.CultureInfo("en-US")),
                Income = monthTuple.Income,
                Expense = monthTuple.Expense,
                Revenue = monthTuple.Income - monthTuple.Expense
            });
        }

        var roomCounts = new Dictionary<string, int>();
        for (int month = 1; month <= today.Month; month++)
        {
            var (large, medium, small) = await _databaseService.GetRoomUsageCountByTypeAsync(year, month);

            if (!roomCounts.ContainsKey("Large"))
                roomCounts["Large"] = 0;
            if (!roomCounts.ContainsKey("Medium"))
                roomCounts["Medium"] = 0;
            if (!roomCounts.ContainsKey("Small"))
                roomCounts["Small"] = 0;

            roomCounts["Large"] += large;
            roomCounts["Medium"] += medium;
            roomCounts["Small"] += small;
        }

        return new SalesAnalysisData
        {
            ApiKey = apiKey,
            MonthlyData = monthlyData,
            RoomTypeCounts = roomCounts
        };
    }

    private async Task CacheSummaryAsync(string summary)
    {
        try
        {
            var cacheKey = $"sales_ai_summary_{DateTime.Now.Year}";
            var timestampKey = $"sales_ai_timestamp_{DateTime.Now.Year}";

            Preferences.Set(cacheKey, summary);
            Preferences.Set(timestampKey, DateTime.Now.Ticks.ToString());

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SalesAnalysisVM] Error caching summary: {ex}");
        }
    }

    public bool ShowSummaryContent
    {
        get => !IsLoading && !HasError;
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}