using SQLite;

namespace CatHotel.Models;

[Table("Sales")]
public class Sale
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [NotNull]
    public string BookingId { get; set; }

    [NotNull]
    public string RoomId { get; set; }

    [NotNull]
    public int RoomRevenue { get; set; } = 0;

    [NotNull]
    public int ShopRevenue { get; set; } = 0;

    [NotNull]
    public int TotalRevenue { get; set; } = 0;

    [NotNull]
    public DateTime CompletedAt { get; set; } = DateTime.Now;

    // ---- Constructors ----
    public Sale()
    {
    }

    public Sale(string bookingId, string roomId, int roomRevenue, int shopRevenue)
    {
        BookingId = bookingId;
        RoomId = roomId;
        RoomRevenue = roomRevenue;
        ShopRevenue = shopRevenue;
        TotalRevenue = roomRevenue + shopRevenue;
        CompletedAt = DateTime.Now;
    }
}