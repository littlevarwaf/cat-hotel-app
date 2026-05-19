using CatHotel.Models;

namespace CatHotel.Services;

public class DatabaseRoomRepository : IRoomRepository
{
    private CatHotel.Services.DatabaseService Db => App.Database;

    public async Task<List<Room>> GetAllRoomsAsync()
    {
        await Db.InitializeAsync();
        return await Db.Db.Table<Room>().ToListAsync();
    }

    public async Task<List<Room>> GetAvailableRoomsAsync()
    {
        await Db.InitializeAsync();
        return await Db.Db.Table<Room>().ToListAsync();
    }

    /// <summary>
    /// Gets available rooms for a date by checking bookings, NOT room status.
    /// Room.Status can get out of sync, so we only trust the Booking data.
    /// </summary>
    public async Task<List<Room>> GetAvailableRoomsForDateAsync(DateTime date)
    {
        await Db.InitializeAsync();

        // Fetch ALL bookings and filter in memory
        var allBookings = await Db.Db.Table<Booking>().ToListAsync();

        // Get rooms that are booked on this date
        var bookedRoomIds = allBookings
            .Where(b => BookingDateHelper.IsBookingActiveOnDate(b.StartDate, b.EndDate, date))
            .Select(b => b.RoomId)
            .ToHashSet();

        // Get all rooms
        var allRooms = await Db.Db.Table<Room>().ToListAsync();

        // Return only rooms that are NOT booked
        return allRooms
            .Where(r => !bookedRoomIds.Contains(r.Id))
            .ToList();
    }

    public async Task<Room?> GetRoomByIdAsync(int id)
    {
        await Db.InitializeAsync();
        return await Db.Db.Table<Room>()
            .Where(r => r.Id == id)
            .FirstOrDefaultAsync();
    }

    public async Task<List<Booking>> GetBookingsForDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        await Db.InitializeAsync();
        
        var allBookings = await Db.Db.Table<Booking>().ToListAsync();
        
        return allBookings
            .Where(b => BookingDateHelper.IsBookingOverlappingDateRange(b.StartDate, b.EndDate, startDate, endDate))
            .ToList();
    }

    public async Task<int> GetBookedRoomsCountForDateAsync(DateTime date)
    {
        await Db.InitializeAsync();

        var allBookings = await Db.Db.Table<Booking>().ToListAsync();

        return allBookings
            .Where(b => BookingDateHelper.IsBookingActiveOnDate(b.StartDate, b.EndDate, date))
            .Select(b => b.RoomId)
            .Distinct()
            .Count();
    }

    public async Task<int> AddRoomAsync(Room room)
    {
        await Db.InitializeAsync();
        await Db.Db.InsertAsync(room);
        return room.Id;
    }

    public async Task<int> UpdateRoomAsync(Room room)
    {
        await Db.InitializeAsync();
        return await Db.Db.UpdateAsync(room);
    }

    public async Task<int> DeleteRoomAsync(Room room)
    {
        await Db.InitializeAsync();
        return await Db.Db.DeleteAsync(room);
    }
}