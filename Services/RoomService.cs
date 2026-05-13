using CatHotel.Models;

namespace CatHotel.Services;

public static class RoomService
{
    public static event EventHandler<RoomEventArgs>? RoomAdded;
    public static event EventHandler<RoomEventArgs>? RoomUpdated;
    public static event EventHandler<RoomEventArgs>? RoomDeleted;

    public static void NotifyRoomAdded(Room room)
    {
        RoomAdded?.Invoke(null, new RoomEventArgs { Room = room });
    }

    public static void NotifyRoomUpdated(Room room)
    {
        RoomUpdated?.Invoke(null, new RoomEventArgs { Room = room });
    }

    public static void NotifyRoomDeleted(Room room)
    {
        RoomDeleted?.Invoke(null, new RoomEventArgs { Room = room });
    }
}

public class RoomEventArgs : EventArgs
{
    public Room? Room { get; set; }
}