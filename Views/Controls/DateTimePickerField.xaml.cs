using System.ComponentModel;

namespace CatHotel.Views.Controls;

public partial class DateTimePickerField : ContentView, INotifyPropertyChanged
{
    public static readonly BindableProperty ValueProperty = BindableProperty.Create(
        nameof(Value),
        typeof(DateTime),
        typeof(DateTimePickerField),
        DateTime.Now,
        BindingMode.TwoWay,
        propertyChanged: OnValuePropertyChanged);

    public static readonly BindableProperty TitleProperty = BindableProperty.Create(
        nameof(Title),
        typeof(string),
        typeof(DateTimePickerField),
        "วันที่และเวลา",
        propertyChanged: (b, _, v) => ((DateTimePickerField)b).TitleLabel.Text = (string)v);

    private bool _syncing;

    public DateTimePickerField()
    {
        InitializeComponent();
        BindingContext = this;
        ApplyValueToPickers(Value);
        UpdateSummary();
    }

    public DateTime Value
    {
        get => (DateTime)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string DisplayText => Value.ToString("dd/MM/yyyy HH:mm");

    private static void OnValuePropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is not DateTimePickerField field || field._syncing)
            return;

        field.ApplyValueToPickers((DateTime)newValue);
        field.UpdateSummary();
        field.OnPropertyChanged(nameof(DisplayText));
    }

    private void ApplyValueToPickers(DateTime value)
    {
        _syncing = true;
        DatePart.Date = value.Date;
        TimePart.Time = value.TimeOfDay;
        _syncing = false;
    }

    private void OnPartChanged(object? sender, EventArgs e) => SyncFromPickers();

    private void OnTimePartPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TimePicker.Time))
            SyncFromPickers();
    }

    private void SyncFromPickers()
    {
        if (_syncing)
            return;

        var date = (DatePart.Date ?? DateTime.Today).Date;
        var time = TimePart.Time ?? TimeSpan.Zero;
        Value = date.Add(time);
        UpdateSummary();
        OnPropertyChanged(nameof(DisplayText));
    }

    private void UpdateSummary() => SummaryLabel.Text = DisplayText;
}
