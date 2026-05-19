using CatHotel.Models;

namespace CatHotel.Services;

public class MockRoomRepository : IRoomRepository
{
    private readonly List<Room> _rooms = new()
    {
        new Room("SMALL001", RoomStatus.Available, RoomTypes.Small, 1, 500, DateTime.Now.AddYears(1), "") { Id = 1 },
        new Room("SMALL002", RoomStatus.Available, RoomTypes.Small, 1, 500, DateTime.Now.AddYears(1), "") { Id = 2 },
        new Room("SMALL003", RoomStatus.Unavailable, RoomTypes.Small, 1, 500, DateTime.Now.AddYears(1), "") { Id = 3 },
        new Room("MID001",   RoomStatus.Available, RoomTypes.Medium, 2, 800, DateTime.Now.AddYears(1), "") { Id = 4 },
        new Room("MID002",   RoomStatus.Unavailable, RoomTypes.Medium, 2, 800, DateTime.Now.AddYears(1), "") { Id = 5 },
    };

    private readonly List<Booking> _bookings = new();
    private int _nextId = 6;

    public Task<List<Room>> GetAllRoomsAsync() =>
        Task.FromResult(_rooms.ToList());

    public Task<List<Room>> GetAvailableRoomsAsync() =>
        Task.FromResult(_rooms.Where(r => r.Status == RoomStatus.Available).ToList());

    public Task<List<Room>> GetAvailableRoomsForDateAsync(DateTime date) =>
        Task.FromResult(_rooms.Where(r => r.Status == RoomStatus.Available).ToList());

    public Task<Room?> GetRoomByIdAsync(int id) =>
        Task.FromResult(_rooms.FirstOrDefault(r => r.Id == id));

    public Task<List<Booking>> GetBookingsForDateRangeAsync(DateTime startDate, DateTime endDate) =>
        Task.FromResult(_bookings
            .Where(b => b.StartDate < endDate && b.EndDate > startDate)
            .ToList());

    public Task<int> GetBookedRoomsCountForDateAsync(DateTime date)
    {
        var count = _bookings
            .Where(b => b.StartDate <= date && b.EndDate > date)
            .Select(b => b.RoomId)
            .Distinct()
            .Count();
        return Task.FromResult(count);
    }

    public Task<int> AddRoomAsync(Room room)
    {
        room.Id = _nextId++;
        _rooms.Add(room);
        return Task.FromResult(room.Id);
    }

    public Task<int> UpdateRoomAsync(Room room)
    {
        var idx = _rooms.FindIndex(r => r.Id == room.Id);
        if (idx >= 0) _rooms[idx] = room;
        return Task.FromResult(idx >= 0 ? 1 : 0);
    }

    public Task<int> DeleteRoomAsync(Room room)
    {
        var removed = _rooms.Remove(room);
        return Task.FromResult(removed ? 1 : 0);
    }
}