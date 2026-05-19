namespace CatHotel.Services;

/// <summary>
/// Helper service for consistent booking date range comparisons.
/// All date comparisons use date-only (no time) for consistency.
/// </summary>
public static class BookingDateHelper
{
    /// <summary>
    /// Determines if a booking overlaps with a specific date.
    /// A booking is active on a date if: StartDate.Date &lt;= date.Date &lt;= EndDate.Date
    /// This includes both the check-in and check-out dates.
    /// </summary>
    public static bool IsBookingActiveOnDate(DateTime bookingStartDate, DateTime bookingEndDate, DateTime date)
    {
        var bookingStart = bookingStartDate.Date;
        var bookingEnd = bookingEndDate.Date;
        var checkDate = date.Date;

        // Room is occupied from start date through end date (inclusive on both ends)
        return bookingStart <= checkDate && checkDate <= bookingEnd;
    }

    /// <summary>
    /// Determines if a booking overlaps with a date range.
    /// </summary>
    public static bool IsBookingOverlappingDateRange(DateTime bookingStartDate, DateTime bookingEndDate, 
        DateTime rangeStartDate, DateTime rangeEndDate)
    {
        var bookingStart = bookingStartDate.Date;
        var bookingEnd = bookingEndDate.Date;
        var rangeStart = rangeStartDate.Date;
        var rangeEnd = rangeEndDate.Date;

        return bookingStart <= rangeEnd && bookingEnd >= rangeStart;
    }

    /// <summary>
    /// Determines if a booking is currently active (checked-in).
    /// Type B: StartDate.Date &lt;= today &lt;= EndDate.Date
    /// </summary>
    public static bool IsBookingCurrentlyActive(DateTime bookingStartDate, DateTime bookingEndDate)
    {
        var today = DateTime.Today;
        return bookingStartDate.Date <= today && today <= bookingEndDate.Date;
    }

    /// <summary>
    /// Determines if a booking is in the future (not yet started).
    /// Type A: StartDate.Date &gt; today
    /// </summary>
    public static bool IsBookingFuture(DateTime bookingStartDate)
    {
        var today = DateTime.Today;
        return bookingStartDate.Date > today;
    }
}