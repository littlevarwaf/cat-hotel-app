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
}
