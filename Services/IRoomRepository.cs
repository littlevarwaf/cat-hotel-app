using CatHotel.Models;

namespace CatHotel.Services;

public interface IRoomRepository
{
    Task<List<Room>> GetAllRoomsAsync();
    Task<List<Room>> GetAvailableRoomsAsync();
    Task<List<Room>> GetAvailableRoomsForDateAsync(DateTime date);
    Task<Room?> GetRoomByIdAsync(int id);
    Task<int> AddRoomAsync(Room room);
    Task<int> UpdateRoomAsync(Room room);
    Task<int> DeleteRoomAsync(Room room);

    /// <summary>
    /// Fetches all bookings that overlap with the specified date range.
    /// Used to calculate room availability on the calendar.
    /// </summary>
    Task<List<Booking>> GetBookingsForDateRangeAsync(DateTime startDate, DateTime endDate);

    /// <summary>
    /// Gets the count of booked rooms for a specific date.
    /// </summary>
    Task<int> GetBookedRoomsCountForDateAsync(DateTime date);
}