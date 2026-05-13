using CatHotel.Models;

namespace CatHotel.Services;

/// <summary>
/// IRoomRepository implementation ที่ดึง/เขียนข้อมูลจาก SQLite จริง
/// ใช้ App.Database ที่ถูก initialize ไว้ใน App.xaml.cs แล้ว
/// </summary>
public class DatabaseRoomRepository : IRoomRepository
{
    private CatHotel.Services.DatabaseService Db => App.Database;

    // ---- Read ----

    public async Task<List<Room>> GetAllRoomsAsync()
    {
        await Db.InitializeAsync();
        return await Db.Db.Table<Room>().ToListAsync();
    }

    public async Task<List<Room>> GetAvailableRoomsAsync()
    {
        await Db.InitializeAsync();
        return await Db.Db.Table<Room>()
            .Where(r => r.Status == RoomStatus.Available)
            .ToListAsync();
    }

    /// <summary>
    /// คืนห้องที่ยังไม่มี Booking ทับวันที่เลือก
    /// (StartDate &lt;= date &lt;= EndDate ของ Booking ที่มีอยู่)
    /// </summary>
    public async Task<List<Room>> GetAvailableRoomsForDateAsync(DateTime date)
    {
        await Db.InitializeAsync();

        // ดึง booking ที่ครอบวันที่เลือก
        var bookings = await Db.Db.Table<Booking>()
            .Where(b => b.StartDate <= date && b.EndDate >= date)
            .ToListAsync();

        var bookedRoomIds = bookings.Select(b => b.RoomId).ToHashSet();

        var allRooms = await Db.Db.Table<Room>()
            .Where(r => r.Status == RoomStatus.Available)
            .ToListAsync();

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

    // ---- Write ----

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
