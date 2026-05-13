using CatHotel.Models;

namespace CatHotel.Services;

public static class RoomService
{
    public static event EventHandler<RoomEventArgs>? RoomAdded;

    public static void NotifyRoomAdded(Room room)
    {
        RoomAdded?.Invoke(null, new RoomEventArgs { Room = room });
    }
}

public class RoomEventArgs : EventArgs
{
    public Room? Room { get; set; }
}