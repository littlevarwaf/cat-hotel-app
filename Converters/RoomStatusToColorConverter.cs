using CatHotel.Models;
using System.Globalization;

namespace CatHotel.Converters;

public class RoomStatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not RoomStatus status)
            return Colors.Gray;

        return status switch
        {
            RoomStatus.Available => Colors.Green,
            RoomStatus.Occupied => Colors.Orange,
            RoomStatus.Unavailable => Colors.Red,
            _ => Colors.Gray
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}