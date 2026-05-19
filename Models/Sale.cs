using SQLite;

namespace CatHotel.Models;

[Table("Sales")]
public class Sale
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [NotNull]
    public int BookingId { get; set; }

    [NotNull]
    public int RoomId { get; set; }

    [NotNull]
    public int RoomRevenue { get; set; } = 0;

    [NotNull]
    public int ShopRevenue { get; set; } = 0;

    [NotNull]
    public int TotalRevenue { get; set; } = 0;

    public int? DiscountId { get; set; }  // Changed to nullable int

    [NotNull]
    public DateTime CompletedAt { get; set; } = DateTime.Now;

    // ---- Constructors ----
    public Sale()
    {
    }

    public Sale(int bookingId, int roomId, int roomRevenue, int shopRevenue, int? discountId = null)
    {
        BookingId = bookingId;
        RoomId = roomId;
        RoomRevenue = roomRevenue;
        ShopRevenue = shopRevenue;
        TotalRevenue = roomRevenue + shopRevenue;
        DiscountId = discountId;
        CompletedAt = DateTime.Now;
    }
}